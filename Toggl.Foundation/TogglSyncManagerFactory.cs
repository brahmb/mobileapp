using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Models;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Sync.States;
using Toggl.Foundation.Sync.States.Pull;
using Toggl.Foundation.Sync.States.Push;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave;
using Toggl.Ultrawave.ApiClients;
using Toggl.Ultrawave.ApiClients.Interfaces;

namespace Toggl.Foundation
{
    public static class TogglSyncManager
    {
        public static ISyncManager CreateSyncManager(
            ITogglDatabase database,
            ITogglApi api,
            ITogglDataSource dataSource,
            ITimeService timeService,
            IAnalyticsService analyticsService,
            ILastTimeUsageStorage lastTimeUsageStorage,
            TimeSpan? retryLimit,
            IScheduler scheduler)
        {
            var random = new Random();
            var queue = new SyncStateQueue();
            var entryPoints = new StateMachineEntryPoints();
            var transitions = new TransitionHandlerProvider();
            var apiDelay = new RetryDelayService(random, retryLimit);
            var delayCancellation = new Subject<Unit>();
            var delayCancellationObservable = delayCancellation.AsObservable().Replay();
            ConfigureTransitions(transitions, database, api, dataSource, apiDelay, scheduler, timeService, analyticsService, entryPoints, delayCancellationObservable);
            var stateMachine = new StateMachine(transitions, scheduler, delayCancellation);
            var orchestrator = new StateMachineOrchestrator(stateMachine, entryPoints);

            return new SyncManager(queue, orchestrator, analyticsService, lastTimeUsageStorage, timeService);
        }

        public static void ConfigureTransitions(
            ITransitionConfigurator transitions,
            ITogglDatabase database,
            ITogglApi api,
            ITogglDataSource dataSource,
            IRetryDelayService apiDelay,
            IScheduler scheduler,
            ITimeService timeService,
            IAnalyticsService analyticsService,
            StateMachineEntryPoints entryPoints,
            IObservable<Unit> delayCancellation)
        {
            configurePullTransitions(transitions, database, api, dataSource, timeService, analyticsService, scheduler, entryPoints.StartPullSync, delayCancellation);
            configurePushTransitions(transitions, api, dataSource, analyticsService, apiDelay, scheduler, entryPoints.StartPushSync, delayCancellation);
        }

        private static void configurePullTransitions(
            ITransitionConfigurator transitions,
            ITogglDatabase database,
            ITogglApi api,
            ITogglDataSource dataSource,
            ITimeService timeService,
            IAnalyticsService analyticsService,
            IScheduler scheduler,
            StateResult entryPoint,
            IObservable<Unit> delayCancellation)
        {
            var rnd = new Random();
            var apiDelay = new RetryDelayService(rnd);
            var statusDelay = new RetryDelayService(rnd);

            var fetchAllSince = new FetchAllSinceState(database, api, timeService);

            var persistWorkspaces =
                new PersistListState<IWorkspace, IDatabaseWorkspace, IThreadSafeWorkspace>(dataSource.Workspaces, Workspace.Clean);

            var updateWorkspacesSinceDate =
                new SinceDateUpdatingState<IWorkspace, IDatabaseWorkspace>(database.SinceParameters);

            var detectNoWorkspaceState = new NoWorkspaceDetectingState(dataSource);

            var persistWorkspaceFeatures =
                new PersistListState<IWorkspaceFeatureCollection, IDatabaseWorkspaceFeatureCollection, IThreadSafeWorkspaceFeatureCollection>(
                    dataSource.WorkspaceFeatures, WorkspaceFeatureCollection.From);

            var persistUser =
                new PersistSingletonState<IUser, IDatabaseUser, IThreadSafeUser>(dataSource.User, User.Clean);

            var noDefaultWorkspaceTrackingState = new NoDefaultWorkspaceTrackingState(analyticsService);

            var persistTags =
                new PersistListState<ITag, IDatabaseTag, IThreadSafeTag>(dataSource.Tags, Tag.Clean);

            var updateTagsSinceDate = new SinceDateUpdatingState<ITag, IDatabaseTag>(database.SinceParameters);

            var persistClients =
                new PersistListState<IClient, IDatabaseClient, IThreadSafeClient>(dataSource.Clients, Client.Clean);

            var updateClientsSinceDate = new SinceDateUpdatingState<IClient, IDatabaseClient>(database.SinceParameters);

            var persistPreferences =
                new PersistSingletonState<IPreferences, IDatabasePreferences, IThreadSafePreferences>(dataSource.Preferences, Preferences.Clean);

            var persistProjects =
                new PersistListState<IProject, IDatabaseProject, IThreadSafeProject>(dataSource.Projects, Project.Clean);

            var updateProjectsSinceDate = new SinceDateUpdatingState<IProject, IDatabaseProject>(database.SinceParameters);

            var createGhostProjects = new CreateGhostProjectsState(dataSource.Projects, analyticsService);

            var persistTimeEntries =
                new PersistListState<ITimeEntry, IDatabaseTimeEntry, IThreadSafeTimeEntry>(dataSource.TimeEntries, TimeEntry.Clean);

            var updateTimeEntriesSinceDate = new SinceDateUpdatingState<ITimeEntry, IDatabaseTimeEntry>(database.SinceParameters);

            var persistTasks =
                new PersistListState<ITask, IDatabaseTask, IThreadSafeTask>(dataSource.Tasks, Task.Clean);

            var updateTasksSinceDate = new SinceDateUpdatingState<ITask, IDatabaseTask>(database.SinceParameters);

            var refetchInaccessibleProjects =
                new TryFetchInaccessibleProjectsState(dataSource.Projects, timeService, api.Projects);

            var retryOrThrow = new SevereApiExceptionsRethrowingState();
            var checkServerStatus = new CheckServerStatusState(api, scheduler, apiDelay, statusDelay, delayCancellation);

            var finished = new ResetAPIDelayState(apiDelay);
            var deleteOlderEntries = new DeleteOldEntriesState(timeService, dataSource.TimeEntries);
            var deleteNonReferencedGhostProjects = new DeleteNonReferencedProjectGhostsState(dataSource.Projects, dataSource.TimeEntries);

            transitions.ConfigureTransition(entryPoint, fetchAllSince);
            transitions.ConfigureTransition(fetchAllSince.FetchStarted, persistWorkspaces);

            transitions.ConfigureTransition(persistWorkspaces.FinishedPersisting, updateWorkspacesSinceDate);
            transitions.ConfigureTransition(updateWorkspacesSinceDate.Finished, detectNoWorkspaceState);
            transitions.ConfigureTransition(detectNoWorkspaceState.Continue, persistUser);

            transitions.ConfigureTransition(persistUser.FinishedPersisting, noDefaultWorkspaceTrackingState);
            transitions.ConfigureTransition(noDefaultWorkspaceTrackingState.Continue, persistWorkspaceFeatures);

            transitions.ConfigureTransition(persistWorkspaceFeatures.FinishedPersisting, persistPreferences);

            transitions.ConfigureTransition(persistPreferences.FinishedPersisting, persistTags);

            transitions.ConfigureTransition(persistTags.FinishedPersisting, updateTagsSinceDate);
            transitions.ConfigureTransition(updateTagsSinceDate.Finished, persistClients);

            transitions.ConfigureTransition(persistClients.FinishedPersisting, updateClientsSinceDate);
            transitions.ConfigureTransition(updateClientsSinceDate.Finished, persistProjects);

            transitions.ConfigureTransition(persistProjects.FinishedPersisting, updateProjectsSinceDate);
            transitions.ConfigureTransition(updateProjectsSinceDate.Finished, persistTasks);

            transitions.ConfigureTransition(persistTasks.FinishedPersisting, updateTasksSinceDate);
            transitions.ConfigureTransition(updateTasksSinceDate.Finished, createGhostProjects);

            transitions.ConfigureTransition(createGhostProjects.FinishedPersisting, persistTimeEntries);
            transitions.ConfigureTransition(persistTimeEntries.FinishedPersisting, updateTimeEntriesSinceDate);
            transitions.ConfigureTransition(updateTimeEntriesSinceDate.Finished, refetchInaccessibleProjects);
            transitions.ConfigureTransition(refetchInaccessibleProjects.FetchNext, refetchInaccessibleProjects);

            transitions.ConfigureTransition(refetchInaccessibleProjects.FinishedPersisting, deleteOlderEntries);
            transitions.ConfigureTransition(deleteOlderEntries.FinishedDeleting, deleteNonReferencedGhostProjects);

            transitions.ConfigureTransition(persistWorkspaces.ErrorOccured, retryOrThrow);
            transitions.ConfigureTransition(persistUser.ErrorOccured, retryOrThrow);
            transitions.ConfigureTransition(persistWorkspaceFeatures.ErrorOccured, retryOrThrow);
            transitions.ConfigureTransition(persistPreferences.ErrorOccured, retryOrThrow);
            transitions.ConfigureTransition(persistTags.ErrorOccured, retryOrThrow);
            transitions.ConfigureTransition(persistClients.ErrorOccured, retryOrThrow);
            transitions.ConfigureTransition(persistProjects.ErrorOccured, retryOrThrow);
            transitions.ConfigureTransition(persistTasks.ErrorOccured, retryOrThrow);
            transitions.ConfigureTransition(createGhostProjects.ErrorOccured, retryOrThrow);
            transitions.ConfigureTransition(persistTimeEntries.ErrorOccured, retryOrThrow);
            transitions.ConfigureTransition(refetchInaccessibleProjects.ErrorOccured, retryOrThrow);

            transitions.ConfigureTransition(retryOrThrow.Retry, checkServerStatus);
            transitions.ConfigureTransition(checkServerStatus.Retry, checkServerStatus);
            transitions.ConfigureTransition(checkServerStatus.ServerIsAvailable, finished);
            transitions.ConfigureTransition(finished.Continue, fetchAllSince);
        }

        private static void configurePushTransitions(
            ITransitionConfigurator transitions,
            ITogglApi api,
            ITogglDataSource dataSource,
            IAnalyticsService analyticsService,
            IRetryDelayService apiDelay,
            IScheduler scheduler,
            StateResult entryPoint,
            IObservable<Unit> delayCancellation)
        {
            var pushingWorkspacesFinished = configureCreateOnlyPush(transitions, entryPoint, dataSource.Workspaces, analyticsService, api.Workspaces, Workspace.Clean, Workspace.Unsyncable, api, scheduler, delayCancellation);
            var pushingUsersFinished = configurePushSingleton(transitions, pushingWorkspacesFinished, dataSource.User, analyticsService, api.User, User.Clean, User.Unsyncable, api, scheduler, delayCancellation);
            var pushingPreferencesFinished = configurePushSingleton(transitions, pushingUsersFinished, dataSource.Preferences, analyticsService, api.Preferences, Preferences.Clean, Preferences.Unsyncable, api, scheduler, delayCancellation);
            var pushingTagsFinished = configureCreateOnlyPush(transitions, pushingPreferencesFinished, dataSource.Tags, analyticsService, api.Tags, Tag.Clean, Tag.Unsyncable, api, scheduler, delayCancellation);
            var pushingClientsFinished = configureCreateOnlyPush(transitions, pushingTagsFinished, dataSource.Clients, analyticsService, api.Clients, Client.Clean, Client.Unsyncable, api, scheduler, delayCancellation);
            var pushingProjectsFinished = configureCreateOnlyPush(transitions, pushingClientsFinished, dataSource.Projects, analyticsService, api.Projects, Project.Clean, Project.Unsyncable, api, scheduler, delayCancellation);
            configurePush(transitions, pushingProjectsFinished, dataSource.TimeEntries, analyticsService, api.TimeEntries, api.TimeEntries, api.TimeEntries, TimeEntry.Clean, TimeEntry.Unsyncable, api, apiDelay, scheduler, delayCancellation);
        }

        private static IStateResult configurePush<TModel, TDatabase, TThreadsafe>(
            ITransitionConfigurator transitions,
            IStateResult entryPoint,
            IDataSource<TThreadsafe, TDatabase> dataSource,
            IAnalyticsService analyticsService,
            ICreatingApiClient<TModel> creatingApi,
            IUpdatingApiClient<TModel> updatingApi,
            IDeletingApiClient<TModel> deletingApi,
            Func<TModel, TThreadsafe> toClean,
            Func<TThreadsafe, string, TThreadsafe> toUnsyncable,
            ITogglApi api,
            IRetryDelayService apiDelay,
            IScheduler scheduler,
            IObservable<Unit> delayCancellation)
            where TModel : class, IIdentifiable, ILastChangedDatable
            where TDatabase : class, TModel, IDatabaseSyncable
            where TThreadsafe : class, TDatabase, IThreadSafeModel
        {
            var rnd = new Random();
            var statusDelay = new RetryDelayService(rnd);

            var push = new PushState<TDatabase, TThreadsafe>(dataSource);
            var pushOne = new PushOneEntityState<TThreadsafe>();
            var create = new CreateEntityState<TModel, TThreadsafe>(creatingApi, dataSource, analyticsService, toClean);
            var update = new UpdateEntityState<TModel, TThreadsafe>(updatingApi, dataSource, analyticsService, toClean);
            var delete = new DeleteEntityState<TModel, TDatabase, TThreadsafe>(deletingApi, dataSource, analyticsService);
            var deleteLocal = new DeleteLocalEntityState<TDatabase, TThreadsafe>(dataSource);
            var tryResolveClientError = new TryResolveClientErrorState<TThreadsafe>();
            var unsyncable = new UnsyncableEntityState<TThreadsafe>(dataSource, toUnsyncable);
            var checkServerStatus = new CheckServerStatusState(api, scheduler, apiDelay, statusDelay, delayCancellation);
            var finished = new ResetAPIDelayState(apiDelay);

            transitions.ConfigureTransition(entryPoint, push);
            transitions.ConfigureTransition(push.PushEntity, pushOne);
            transitions.ConfigureTransition(pushOne.CreateEntity, create);
            transitions.ConfigureTransition(pushOne.UpdateEntity, update);
            transitions.ConfigureTransition(pushOne.DeleteEntity, delete);
            transitions.ConfigureTransition(pushOne.DeleteEntityLocally, deleteLocal);

            transitions.ConfigureTransition(create.ClientError, tryResolveClientError);
            transitions.ConfigureTransition(update.ClientError, tryResolveClientError);
            transitions.ConfigureTransition(delete.ClientError, tryResolveClientError);

            transitions.ConfigureTransition(create.ServerError, checkServerStatus);
            transitions.ConfigureTransition(update.ServerError, checkServerStatus);
            transitions.ConfigureTransition(delete.ServerError, checkServerStatus);

            transitions.ConfigureTransition(create.UnknownError, checkServerStatus);
            transitions.ConfigureTransition(update.UnknownError, checkServerStatus);
            transitions.ConfigureTransition(delete.UnknownError, checkServerStatus);

            transitions.ConfigureTransition(tryResolveClientError.UnresolvedTooManyRequests, checkServerStatus);
            transitions.ConfigureTransition(tryResolveClientError.Unresolved, unsyncable);

            transitions.ConfigureTransition(checkServerStatus.Retry, checkServerStatus);
            transitions.ConfigureTransition(checkServerStatus.ServerIsAvailable, push);

            transitions.ConfigureTransition(create.CreatingFinished, finished);
            transitions.ConfigureTransition(update.UpdatingSucceeded, finished);
            transitions.ConfigureTransition(delete.DeletingFinished, finished);
            transitions.ConfigureTransition(deleteLocal.Deleted, finished);
            transitions.ConfigureTransition(deleteLocal.DeletingFailed, finished);

            transitions.ConfigureTransition(finished.Continue, push);

            return push.NothingToPush;
        }

        private static IStateResult configureCreateOnlyPush<TModel, TDatabase, TThreadsafe>(
            ITransitionConfigurator transitions,
            IStateResult entryPoint,
            IDataSource<TThreadsafe, TDatabase> dataSource,
            IAnalyticsService analyticsService,
            ICreatingApiClient<TModel> creatingApi,
            Func<TModel, TThreadsafe> toClean,
            Func<TThreadsafe, string, TThreadsafe> toUnsyncable,
            ITogglApi api,
            IScheduler scheduler,
            IObservable<Unit> delayCancellation)
            where TModel : IIdentifiable, ILastChangedDatable
            where TDatabase : class, TModel, IDatabaseSyncable
            where TThreadsafe : class, TDatabase, IThreadSafeModel
        {
            var rnd = new Random();
            var apiDelay = new RetryDelayService(rnd);
            var statusDelay = new RetryDelayService(rnd);

            var push = new PushState<TDatabase, TThreadsafe>(dataSource);
            var pushOne = new PushOneEntityState<TThreadsafe>();
            var create = new CreateEntityState<TModel, TThreadsafe>(creatingApi, dataSource, analyticsService, toClean);
            var tryResolveClientError = new TryResolveClientErrorState<TThreadsafe>();
            var unsyncable = new UnsyncableEntityState<TThreadsafe>(dataSource, toUnsyncable);
            var checkServerStatus = new CheckServerStatusState(api, scheduler, apiDelay, statusDelay, delayCancellation);
            var finished = new ResetAPIDelayState(apiDelay);

            transitions.ConfigureTransition(entryPoint, push);
            transitions.ConfigureTransition(push.PushEntity, pushOne);
            transitions.ConfigureTransition(pushOne.CreateEntity, create);

            transitions.ConfigureTransition(pushOne.UpdateEntity, new InvalidTransitionState($"Updating is not supported for {typeof(TModel).Name} during Push sync."));
            transitions.ConfigureTransition(pushOne.DeleteEntity, new InvalidTransitionState($"Deleting is not supported for {typeof(TModel).Name} during Push sync."));
            transitions.ConfigureTransition(pushOne.DeleteEntityLocally, new InvalidTransitionState($"Deleting locally is not supported for {typeof(TModel).Name} during Push sync."));

            transitions.ConfigureTransition(create.ClientError, tryResolveClientError);
            transitions.ConfigureTransition(create.ServerError, checkServerStatus);
            transitions.ConfigureTransition(create.UnknownError, checkServerStatus);

            transitions.ConfigureTransition(tryResolveClientError.UnresolvedTooManyRequests, checkServerStatus);
            transitions.ConfigureTransition(tryResolveClientError.Unresolved, unsyncable);

            transitions.ConfigureTransition(checkServerStatus.Retry, checkServerStatus);
            transitions.ConfigureTransition(checkServerStatus.ServerIsAvailable, push);

            transitions.ConfigureTransition(create.CreatingFinished, finished);

            transitions.ConfigureTransition(finished.Continue, push);

            return push.NothingToPush;
        }

        private static IStateResult configurePushSingleton<TModel, TThreadsafe>(
            ITransitionConfigurator transitions,
            IStateResult entryPoint,
            ISingletonDataSource<TThreadsafe> dataSource,
            IAnalyticsService analyticsService,
            IUpdatingApiClient<TModel> updatingApi,
            Func<TModel, TThreadsafe> toClean,
            Func<TThreadsafe, string, TThreadsafe> toUnsyncable,
            ITogglApi api,
            IScheduler scheduler,
            IObservable<Unit> delayCancellation)
            where TModel : class
            where TThreadsafe : class, TModel, IThreadSafeModel, IDatabaseSyncable
        {
            var rnd = new Random();
            var apiDelay = new RetryDelayService(rnd);
            var statusDelay = new RetryDelayService(rnd);

            var push = new PushSingleState<TThreadsafe>(dataSource);
            var pushOne = new PushOneEntityState<TThreadsafe>();
            var update = new UpdateEntityState<TModel, TThreadsafe>(updatingApi, dataSource, analyticsService, toClean);
            var tryResolveClientError = new TryResolveClientErrorState<TThreadsafe>();
            var unsyncable = new UnsyncableEntityState<TThreadsafe>(dataSource, toUnsyncable);
            var checkServerStatus = new CheckServerStatusState(api, scheduler, apiDelay, statusDelay, delayCancellation);
            var finished = new ResetAPIDelayState(apiDelay);

            transitions.ConfigureTransition(entryPoint, push);
            transitions.ConfigureTransition(push.PushEntity, pushOne);
            transitions.ConfigureTransition(pushOne.UpdateEntity, update);

            transitions.ConfigureTransition(pushOne.CreateEntity, new InvalidTransitionState($"Creating is not supported for {typeof(TModel).Name} during Push sync."));
            transitions.ConfigureTransition(pushOne.DeleteEntity, new InvalidTransitionState($"Deleting is not supported for {typeof(TModel).Name} during Push sync."));
            transitions.ConfigureTransition(pushOne.DeleteEntityLocally, new InvalidTransitionState($"Deleting locally is not supported for {typeof(TModel).Name} during Push sync."));

            transitions.ConfigureTransition(update.ClientError, tryResolveClientError);
            transitions.ConfigureTransition(update.ServerError, checkServerStatus);
            transitions.ConfigureTransition(update.UnknownError, checkServerStatus);

            transitions.ConfigureTransition(tryResolveClientError.UnresolvedTooManyRequests, checkServerStatus);
            transitions.ConfigureTransition(tryResolveClientError.Unresolved, unsyncable);

            transitions.ConfigureTransition(checkServerStatus.Retry, checkServerStatus);
            transitions.ConfigureTransition(checkServerStatus.ServerIsAvailable, push);

            transitions.ConfigureTransition(update.UpdatingSucceeded, finished);

            transitions.ConfigureTransition(finished.Continue, push);

            return push.NothingToPush;
        }
    }
}
