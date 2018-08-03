using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave.ApiClients;
using Toggl.Ultrawave.Network;

namespace Toggl.Foundation.Interactors.Settings
{
    public class SendFeedbackInteractor : IInteractor<IObservable<Unit>>
    {
        private readonly IFeedbackApi feedbackApi;

        private readonly ISingletonDataSource<IThreadSafeUser> userDataSource;

        private readonly IDataSource<IThreadSafeWorkspace, IDatabaseWorkspace> workspacesDataSource;

        private readonly IDataSource<IThreadSafeTimeEntry, IDatabaseTimeEntry> timeEntriesDataSource;

        private readonly IPlatformConstants platformConstants;

        private readonly IUserPreferences userPreferences;

        private readonly ILastTimeUsageStorage lastTimeUsageStorage;

        private readonly ITimeService timeService;

        private readonly UserAgent userAgent;

        private readonly string message;

        public SendFeedbackInteractor(
            IFeedbackApi feedbackApi,
            ISingletonDataSource<IThreadSafeUser> userDataSource,
            IDataSource<IThreadSafeWorkspace, IDatabaseWorkspace> workspacesDataSource,
            IDataSource<IThreadSafeTimeEntry, IDatabaseTimeEntry> timeEntriesDataSource,
            IPlatformConstants platformConstants,
            IUserPreferences userPreferences,
            ILastTimeUsageStorage lastTimeUsageStorage,
            ITimeService timeService,
            UserAgent userAgent,
            string message)
        {
            Ensure.Argument.IsNotNull(feedbackApi, nameof(feedbackApi));
            Ensure.Argument.IsNotNull(userDataSource, nameof(userDataSource));
            Ensure.Argument.IsNotNull(workspacesDataSource, nameof(workspacesDataSource));
            Ensure.Argument.IsNotNull(timeEntriesDataSource, nameof(timeEntriesDataSource));
            Ensure.Argument.IsNotNull(platformConstants, nameof(platformConstants));
            Ensure.Argument.IsNotNull(userPreferences, nameof(userPreferences));
            Ensure.Argument.IsNotNull(lastTimeUsageStorage, nameof(lastTimeUsageStorage));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(userAgent, nameof(userAgent));
            Ensure.Argument.IsNotNull(message, nameof(message));

            this.feedbackApi = feedbackApi;
            this.userDataSource = userDataSource;
            this.workspacesDataSource = workspacesDataSource;
            this.timeEntriesDataSource = timeEntriesDataSource;
            this.platformConstants = platformConstants;
            this.userPreferences = userPreferences;
            this.lastTimeUsageStorage = lastTimeUsageStorage;
            this.timeService = timeService;
            this.userAgent = userAgent;
            this.message = message;
        }

        public static string PhoneModel { get; } = "Phone model";

        public static string OperatingSystem { get; } = "Platform and OS version";

        public static string AppNameAndVersion { get; } = "App name and version";

        public static string NumberOfWorkspaces { get; } = "Number of workspaces available to the user";

        public static string NumberOfTimeEntries { get; } = "Number of time entries in our database in total";

        public static string NumberOfUnsyncedTimeEntries { get; } = "Number of unsynced time entries";

        public static string NumberOfUnsyncableTimeEntries { get; } = "Number of unsyncable time entries";

        public static string LastSyncAttempt { get; } = "Time of last attempted sync";

        public static string LastSuccessfulSync { get; } = "Time of last successful full sync";

        public static string LastLogin { get; } = "Last login";

        public static string DeviceTime { get; } = "Device's time when sending this feedback";

        public static string ManualModeIsOn { get; } = "Manual mode is on";

        private IObservable<int> workspacesCount
            => workspacesDataSource
                .GetAll()
                .Select(list => list.Count());

        private IObservable<int> timeEntriesCount
            => timeEntriesDataSource
                .GetAll()
                .Select(list => list.Count());

        private IObservable<int> unsyncedTimeEntriesCount
            => timeEntriesDataSource
                .GetAll(te => te.SyncStatus == SyncStatus.SyncNeeded)
                .Select(list => list.Count());

        private IObservable<int> unsyncabeTimeEntriesCount
            => timeEntriesDataSource
                .GetAll(te => te.SyncStatus == SyncStatus.SyncFailed)
                .Select(list => list.Count());

        public IObservable<Unit> Execute()
            => workspacesCount.Zip(
                    timeEntriesCount,
                    unsyncedTimeEntriesCount,
                    unsyncabeTimeEntriesCount,
                    combineData)
                .SelectMany(data =>
                    userDataSource.Get().SelectMany(user =>
                        feedbackApi.Send(user.Email, message, data)));

        private Dictionary<string, string> combineData(
            int workspaces,
            int timeEntries,
            int unsyncedTimeEntries,
            int unsyncableTimeEntriesCount)
            => new Dictionary<string, string>
            {
                [PhoneModel] = platformConstants.PhoneModel,
                [OperatingSystem] = platformConstants.OperatingSystem,
                [AppNameAndVersion] = $"{userAgent.Name}/{userAgent.Version}",
                [NumberOfWorkspaces] = workspaces.ToString(),
                [NumberOfTimeEntries] = timeEntries.ToString(),
                [NumberOfUnsyncedTimeEntries] = unsyncedTimeEntries.ToString(),
                [NumberOfUnsyncableTimeEntries] = unsyncableTimeEntriesCount.ToString(),
                [LastSyncAttempt] = lastTimeUsageStorage.LastSyncAttempt?.ToString() ?? "never",
                [LastSuccessfulSync] = lastTimeUsageStorage.LastSuccessfulSync?.ToString() ?? "never",
                [DeviceTime] = timeService.CurrentDateTime.ToString(),
                [ManualModeIsOn] = userPreferences.IsManualModeEnabled ? "yes" : "no",
                [LastLogin] = lastTimeUsageStorage.LastLogin?.ToString() ?? "never"
            };
    }
}
