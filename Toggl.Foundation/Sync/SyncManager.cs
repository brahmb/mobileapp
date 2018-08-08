using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Exceptions;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave.Exceptions;
using static Toggl.Foundation.Sync.SyncState;

namespace Toggl.Foundation.Sync
{
    public sealed class SyncManager : ISyncManager
    {
        private readonly object stateLock = new object();
        private readonly ISyncStateQueue queue;
        private readonly IStateMachineOrchestrator orchestrator;
        private readonly IAnalyticsService analyticsService;
        private readonly ILastTimeUsageStorage lastTimeUsageStorage;
        private readonly ITimeService timeService;

        private bool isFrozen;

        private readonly ISubject<SyncProgress> progress;

        public bool IsRunningSync { get; private set; }

        public SyncState State => orchestrator.State;
        public IObservable<SyncProgress> ProgressObservable { get; }

        public SyncManager(
            ISyncStateQueue queue,
            IStateMachineOrchestrator orchestrator,
            IAnalyticsService analyticsService,
            ILastTimeUsageStorage lastTimeUsageStorage,
            ITimeService timeService)
        {
            Ensure.Argument.IsNotNull(queue, nameof(queue));
            Ensure.Argument.IsNotNull(orchestrator, nameof(orchestrator));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(lastTimeUsageStorage, nameof(lastTimeUsageStorage));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));

            this.queue = queue;
            this.orchestrator = orchestrator;
            this.analyticsService = analyticsService;
            this.lastTimeUsageStorage = lastTimeUsageStorage;
            this.timeService = timeService;

            progress = new BehaviorSubject<SyncProgress>(SyncProgress.Unknown);
            ProgressObservable = progress.AsObservable();

            orchestrator.SyncCompleteObservable.Subscribe(syncOperationCompleted);
            isFrozen = false;
        }

        public IObservable<SyncState> PushSync()
        {
            lock (stateLock)
            {
                queue.QueuePushSync();
                return startSyncIfNeededAndObserve();
            }
        }

        public IObservable<SyncState> ForceFullSync()
        {
            lastTimeUsageStorage.SetFullSyncAttempt(timeService.CurrentDateTime);

            lock (stateLock)
            {
                queue.QueuePullSync();
                return startSyncIfNeededAndObserve().Do(saveTimeWhenReachedSleep);
            }
        }

        public IObservable<SyncState> CleanUp()
        {
            lock (stateLock)
            {
                queue.QueueCleanUp();
                return startSyncIfNeededAndObserve();
            }
        }

        public IObservable<SyncState> Freeze()
        {
            lock (stateLock)
            {
                if (isFrozen == false)
                {
                    isFrozen = true;
                    orchestrator.Freeze();
                }

                return IsRunningSync
                    ? syncStatesUntilAndIncludingSleep().LastAsync()
                    : Observable.Return(Sleep);
            }
        }

        private void saveTimeWhenReachedSleep(SyncState state)
        {
            if (state == Sleep)
            {
                lastTimeUsageStorage.SetSuccessfulFullSync(timeService.CurrentDateTime);
            }
        }

        private void syncOperationCompleted(SyncResult result)
        {
            lock (stateLock)
            {
                IsRunningSync = false;

                if (result is Success)
                {
                    startSyncIfNeeded();
                    if (IsRunningSync == false)
                    {
                        progress.OnNext(SyncProgress.Synced);
                    }
                    return;
                }

                if (result is Error error)
                {
                    processError(error.Exception);
                    return;
                }

                throw new ArgumentException(nameof(result));
            }
        }

        private void processError(Exception error)
        {
            queue.Clear();
            orchestrator.Start(Sleep);

            if (error is NoWorkspaceException)
            {
                progress.OnError(error);
                return;
            }

            if (error is OfflineException)
            {
                progress.OnNext(SyncProgress.OfflineModeDetected);
                analyticsService.OfflineModeDetected.Track();
            }
            else
            {
                progress.OnNext(SyncProgress.Failed);
                analyticsService.Track(error);
            }

            if (error is ClientDeprecatedException
                || error is ApiDeprecatedException
                || error is UnauthorizedException)
            {
                Freeze();
                progress.OnError(error);
            }
        }

        private IObservable<SyncState> startSyncIfNeededAndObserve()
        {
            startSyncIfNeeded();

            return syncStatesUntilAndIncludingSleep();
        }

        private void startSyncIfNeeded()
        {
            if (IsRunningSync) return;

            var state = isFrozen ? Sleep : queue.Dequeue();
            IsRunningSync = state != Sleep;

            if (IsRunningSync && progress.FirstAsync().Wait() != SyncProgress.Syncing)
            {
                progress.OnNext(SyncProgress.Syncing);
            }

            orchestrator.Start(state);
        }

        private IObservable<SyncState> syncStatesUntilAndIncludingSleep()
            => orchestrator.StateObservable
                .TakeWhile(s => s != Sleep)
                .Concat(Observable.Return(Sleep))
                .ConnectedReplay();
    }
}
