using System;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Reports;
using Toggl.Foundation.Services;
using Toggl.Foundation.Shortcuts;
using Toggl.Foundation.Sync;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Toggl.Ultrawave;
using Toggl.Ultrawave.ApiClients;

namespace Toggl.Foundation.DataSources
{
    public sealed class TogglDataSource : ITogglDataSource
    {
        private readonly ITogglDatabase database;
        private readonly IErrorHandlingService errorHandlingService;
        private readonly IBackgroundService backgroundService;
        private readonly IApplicationShortcutCreator shortcutCreator;

        private readonly TimeSpan minimumTimeInBackgroundForFullSync;

        private IDisposable errorHandlingDisposable;
        private IDisposable signalDisposable;

        private bool isLoggedIn;

        private Func<ITogglDataSource, ISyncManager> createSyncManager;

        public TogglDataSource(
            ITogglApi api,
            ITogglDatabase database,
            ITimeService timeService,
            IErrorHandlingService errorHandlingService,
            IBackgroundService backgroundService,
            Func<ITogglDataSource, ISyncManager> createSyncManager,
            TimeSpan minimumTimeInBackgroundForFullSync,
            IApplicationShortcutCreator shortcutCreator,
            IAnalyticsService analyticsService)
        {
            Ensure.Argument.IsNotNull(api, nameof(api));
            Ensure.Argument.IsNotNull(database, nameof(database));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(errorHandlingService, nameof(errorHandlingService));
            Ensure.Argument.IsNotNull(backgroundService, nameof(backgroundService));
            Ensure.Argument.IsNotNull(createSyncManager, nameof(createSyncManager));
            Ensure.Argument.IsNotNull(shortcutCreator, nameof(shortcutCreator));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));

            this.database = database;
            this.errorHandlingService = errorHandlingService;
            this.backgroundService = backgroundService;
            this.shortcutCreator = shortcutCreator;

            this.minimumTimeInBackgroundForFullSync = minimumTimeInBackgroundForFullSync;

            User = new UserDataSource(database.User, timeService);
            Tags = new TagsDataSource(database.IdProvider, database.Tags, timeService);
            Tasks = new TasksDataSource(database.Tasks);
            Clients = new ClientsDataSource(database.IdProvider, database.Clients, timeService);
            Preferences = new PreferencesDataSource(database.Preferences);
            Projects = new ProjectsDataSource(database.IdProvider, database.Projects, timeService);
            TimeEntries = new TimeEntriesDataSource(database.TimeEntries, timeService, analyticsService);
            Workspaces = new WorkspacesDataSource(database.IdProvider, database.Workspaces, timeService);
            WorkspaceFeatures = new WorkspaceFeaturesDataSource(database.WorkspaceFeatures);


            this.createSyncManager = createSyncManager;
            CreateNewSyncManager();

            ReportsProvider = new ReportsProvider(api, database);

            FeedbackApi = api.Feedback;

            isLoggedIn = true;
        }

        public IUserSource User { get; }
        public ITagsSource Tags { get; }
        public ITasksSource Tasks { get; }
        public IClientsSource Clients { get; }
        public IPreferencesSource Preferences { get; }
        public IProjectsSource Projects { get; }
        public ITimeEntriesSource TimeEntries { get; }
        public IWorkspacesSource Workspaces { get; }
        public IDataSource<IThreadSafeWorkspaceFeatureCollection, IDatabaseWorkspaceFeatureCollection> WorkspaceFeatures { get; }

        public ISyncManager SyncManager { get; private set; }

        public IReportsProvider ReportsProvider { get; }

        public IFeedbackApi FeedbackApi { get; }

        public void CreateNewSyncManager()
        {
            SyncManager = createSyncManager(this);
            errorHandlingDisposable?.Dispose();
            errorHandlingDisposable = SyncManager.ProgressObservable.Subscribe(onSyncError);
        }

        public IObservable<Unit> StartSyncing()
        {
            if (isLoggedIn == false)
                throw new InvalidOperationException("Cannot start syncing after the user logged out of the app.");

            if (signalDisposable != null)
                throw new InvalidOperationException("The StartSyncing method has already been called.");

            signalDisposable = backgroundService.AppResumedFromBackground
                .Where(timeInBackground => timeInBackground >= minimumTimeInBackgroundForFullSync)
                .Subscribe((TimeSpan _) => SyncManager.ForceFullSync());

            return SyncManager.ForceFullSync()
                .Select(_ => Unit.Default);
        }

        public IObservable<bool> HasUnsyncedData()
            => Observable.Merge(
                hasUnsyncedData(database.TimeEntries),
                hasUnsyncedData(database.Projects),
                hasUnsyncedData(database.User),
                hasUnsyncedData(database.Tasks),
                hasUnsyncedData(database.Clients),
                hasUnsyncedData(database.Tags),
                hasUnsyncedData(database.Workspaces))
                .Any(hasUnsynced => hasUnsynced);

        public IObservable<Unit> Logout()
            => SyncManager.Freeze()
                .FirstAsync()
                .Do(_ => isLoggedIn = false)
                .Do(_ => stopSyncingOnSignal())
                .SelectMany(_ => database.Clear())
                .Do(_ => shortcutCreator.OnLogout())
                .FirstAsync();

        private IObservable<bool> hasUnsyncedData<TModel>(IBaseStorage<TModel> repository)
            where TModel : IDatabaseSyncable
            => repository
                .GetAll(entity => entity.SyncStatus != SyncStatus.InSync)
                .Select(unsynced => unsynced.Any())
                .SingleAsync();

        private void onSyncError(Exception exception)
        {
            if (errorHandlingService.TryHandleDeprecationError(exception)
                || errorHandlingService.TryHandleUnauthorizedError(exception)
                || errorHandlingService.TryHandleNoWorkspaceError(exception))
            {
                stopSyncingOnSignal();
                return;
            }

            throw new ArgumentException($"{nameof(TogglDataSource)} could not handle unknown sync error {exception.GetType().FullName}.", exception);
        }

        private void stopSyncingOnSignal()
            => signalDisposable?.Dispose();
    }
}
