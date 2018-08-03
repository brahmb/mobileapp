using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Sync;
using Xunit;
using FsCheck.Xunit;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Tests.Generators;
using Toggl.PrimeRadiant.Settings;
using static Toggl.Foundation.Sync.SyncState;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Network;

namespace Toggl.Foundation.Tests.Sync
{
    public sealed class SyncManagerTests
    {
        public abstract class SyncManagerTestBase
        {
            protected Subject<SyncResult> OrchestratorSyncComplete { get; } = new Subject<SyncResult>();
            protected Subject<SyncState> OrchestratorStates { get; } = new Subject<SyncState>();
            protected ISyncStateQueue Queue { get; } = Substitute.For<ISyncStateQueue>();
            protected IStateMachineOrchestrator Orchestrator { get; } = Substitute.For<IStateMachineOrchestrator>();
            protected IAnalyticsService AnalyticsService { get; } = Substitute.For<IAnalyticsService>();
            protected ILastTimeUsageStorage LastTimeUsageStorage { get; } = Substitute.For<ILastTimeUsageStorage>();
            protected ITimeService TimeService { get; } = Substitute.For<ITimeService>();
            protected ISyncManager SyncManager { get; }

            protected SyncManagerTestBase()
            {
                Orchestrator.SyncCompleteObservable.Returns(OrchestratorSyncComplete.AsObservable());
                Orchestrator.StateObservable.Returns(OrchestratorStates.AsObservable());
                SyncManager = new SyncManager(Queue, Orchestrator, AnalyticsService, LastTimeUsageStorage, TimeService);
            }
        }

        public sealed class TheConstuctor : SyncManagerTestBase
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyArgumentIsNull(bool useQueue, bool useOrchestrator, bool useAnalyticsService, bool useLastTimeUsageStorage, bool useTimeService)
            {
                var queue = useQueue ? Queue : null;
                var orchestrator = useOrchestrator ? Orchestrator : null;
                var analyticsService = useAnalyticsService ? AnalyticsService : null;
                var lastTimeUsageStorage = useLastTimeUsageStorage ? LastTimeUsageStorage : null;
                var timeService = useTimeService ? TimeService : null;

                // ReSharper disable once ObjectCreationAsStatement
                Action constructor = () => new SyncManager(queue, orchestrator, analyticsService, lastTimeUsageStorage, timeService);

                constructor.Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheStateProperty : SyncManagerTestBase
        {
            [Property]
            public void ShouldReturnStateFromOrchestrator(int stateValue)
            {
                Orchestrator.State.Returns((SyncState)stateValue);

                SyncManager.State.Should().Be((SyncState)stateValue);
            }
        }

        public abstract class ThreadSafeQueingMethodTests : SyncManagerTestBase
        {
            protected abstract void CallMethod();

            [Fact, LogIfTooSlow]
            public void AgainTellsQueueToStartSyncAfterCompletingPreviousFullSync()
            {
                Queue.Dequeue().Returns(Pull);
                SyncManager.ForceFullSync();
                Queue.Dequeue().Returns(Sleep);
                OrchestratorSyncComplete.OnNext(new Success(Pull));
                Queue.ClearReceivedCalls();

                CallMethod();

                Queue.Received().Dequeue();
            }

            [Fact, LogIfTooSlow]
            public void AgainTellsQueueToStartSyncAfterCompletingPreviousPushSync()
            {
                Queue.Dequeue().Returns(Pull);
                SyncManager.PushSync();
                Queue.Dequeue().Returns(Sleep);
                OrchestratorSyncComplete.OnNext(new Success(Push));
                Queue.ClearReceivedCalls();

                CallMethod();

                Queue.Received().Dequeue();
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotQueueUntilOtherCompletedEventReturns()
            {
                await ensureMethodIsThreadSafeWith(() => OrchestratorSyncComplete.OnNext(new Success(0)));
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotQueueUntilOtherCallToPushSyncReturns()
            {
                await ensureMethodIsThreadSafeWith(() => SyncManager.PushSync());
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotQueueUntilOtherCallToForceFullSyncReturns()
            {
                await ensureMethodIsThreadSafeWith(() => SyncManager.ForceFullSync());
            }

            private async Task ensureMethodIsThreadSafeWith(Action otherMethod)
            {
                var endFirstCall = new AutoResetEvent(false);
                var startedFirstCall = new AutoResetEvent(false);
                var startedSecondCall = new AutoResetEvent(false);
                var endedSecondCall = new AutoResetEvent(false);
                var isFirstCall = true;
                Queue.Dequeue().Returns(Sleep).AndDoes(_ =>
                {
                    // the second time we call this we don't want to do anything
                    if (!isFirstCall) return;
                    isFirstCall = false;
                    startedFirstCall.Set();
                    endFirstCall.WaitOne();
                });
                var firstCall = new Thread(() =>
                {
                    otherMethod();
                });
                firstCall.Start();
                // ensure the first call gets inside locked code before starting second one
                startedFirstCall.WaitOne();

                var endedSecondCallBool = false;
                var secondCall = new Thread(() =>
                {
                    startedSecondCall.Set();
                    // here is where we call the method that should be blocked until the first returns
                    CallMethod();
                    endedSecondCallBool = true;
                    endedSecondCall.Set();
                });
                secondCall.Start();
                startedSecondCall.WaitOne();

                // ensure that the second call has time to get to the lock
                // this could probably do with much less time, but I rather make sure
                await Task.Delay(100);

                // the first call is still stuck on `endFirstCall`, so the second has to wait
                endedSecondCallBool.Should().BeFalse();

                // end the first call, which should trigger the second to enter the lock,
                // and complete soon after
                endFirstCall.Set();
                endedSecondCall.WaitOne();

                endedSecondCallBool.Should().BeTrue();
            }
        }

        public sealed class TheOrchestratorCompleteObservable : ThreadSafeQueingMethodTests
        {
            protected override void CallMethod()
                => OrchestratorSyncComplete.OnNext(new Success(0));

            [Fact, LogIfTooSlow]
            public void TellsQueueToStartSync()
            {
                CallMethod();

                Queue.Received().Dequeue();
            }

            [Fact, LogIfTooSlow]
            public void DoesNotQueuePullSync()
            {
                CallMethod();

                Queue.DidNotReceive().QueuePullSync();
            }

            [Fact, LogIfTooSlow]
            public void DoesNotQueuePushSync()
            {
                CallMethod();

                Queue.DidNotReceive().QueuePullSync();
            }

            [Fact, LogIfTooSlow]
            public void TellsQueueToStartOrchestratorWhenAlreadyRunningFullSync()
            {
                Queue.Dequeue().Returns(Pull);
                SyncManager.ForceFullSync();
                Queue.ClearReceivedCalls();

                CallMethod();

                Queue.Received().Dequeue();
            }

            [Fact, LogIfTooSlow]
            public void TellsQueueToStartOrchestratorWhenAlreadyRunningPushSync()
            {
                Queue.Dequeue().Returns(Pull);
                SyncManager.PushSync();
                Queue.ClearReceivedCalls();

                CallMethod();

                Queue.Received().Dequeue();
            }

            [Fact, LogIfTooSlow]
            public void TellsQueueToStartOrchestratorWhenInSecondPartOfMultiPhaseSync()
            {
                Queue.Dequeue().Returns(Pull);
                SyncManager.ForceFullSync();
                OrchestratorSyncComplete.OnNext(new Success(Push));
                Queue.ClearReceivedCalls();

                CallMethod();

                Queue.Received().Dequeue();
            }

            [Fact, LogIfTooSlow]
            public void ThrowsWhenAnUnsupportedSyncResultIsEmittedByTheOrchestrator()
            {
                Action emittingUnsupportedResult = () => OrchestratorSyncComplete.OnNext(new UnsupportedResult());

                emittingUnsupportedResult.Should().Throw<ArgumentException>();
            }

            private class UnsupportedResult : SyncResult { }
        }

        public abstract class SyncMethodTests : ThreadSafeQueingMethodTests
        {
            protected override void CallMethod() => CallSyncMethod();

            protected abstract IObservable<SyncState> CallSyncMethod();

            [Property]
            public void ReturnsObservableThatReplaysSyncStatesUntilSleep(bool[] beforeSleep, bool[] afterSleep)
            {
                var observable = CallSyncMethod();

                var beforeSleepStates = (beforeSleep ?? Enumerable.Empty<bool>())
                    .Select(b => b ? Push : Pull);
                var afterSleepStates = (afterSleep ?? Enumerable.Empty<bool>())
                    .Select(b => b ? Push : Pull);

                var expectedStates = beforeSleepStates.Concat(new[] { Sleep }).ToList();

                foreach (var states in expectedStates.Concat(afterSleepStates))
                    OrchestratorStates.OnNext(states);

                var actual = observable.ToList().Wait();

                actual.Should().BeEquivalentTo(expectedStates);
            }

            [Fact, LogIfTooSlow]
            public void DoesNotTellQueueToStartOrchestratorWhenAlreadyRunningFullSync()
            {
                Queue.Dequeue().Returns(Pull);
                SyncManager.ForceFullSync();
                Queue.ClearReceivedCalls();

                CallMethod();

                Queue.DidNotReceive().Dequeue();
            }

            [Fact, LogIfTooSlow]
            public void DoesNotTellQueueToStartOrchestratorWhenAlreadyRunningPushSync()
            {
                Queue.Dequeue().Returns(Pull);
                SyncManager.PushSync();
                Queue.ClearReceivedCalls();

                CallMethod();

                Queue.DidNotReceive().Dequeue();
            }

            [Fact, LogIfTooSlow]
            public void DoesNotTellQueueToStartOrchestratorWhenInSecondPartOfMultiPhaseSync()
            {
                Queue.Dequeue().Returns(Pull);
                SyncManager.ForceFullSync();
                OrchestratorSyncComplete.OnNext(new Success(Push));
                Queue.ClearReceivedCalls();

                CallMethod();

                Queue.DidNotReceive().Dequeue();
            }
        }

        public sealed class ThePushSyncMethod : SyncMethodTests
        {
            protected override IObservable<SyncState> CallSyncMethod()
                => SyncManager.PushSync();

            [Fact, LogIfTooSlow]
            public void TellsQueueToStartSyncAfterQueingPush()
            {
                CallMethod();

                Received.InOrder(() =>
                {
                    Queue.QueuePushSync();
                    Queue.Dequeue();
                });
            }
        }

        public sealed class TheForceFullSyncMethod : SyncMethodTests
        {
            protected override IObservable<SyncState> CallSyncMethod()
                => SyncManager.ForceFullSync();

            [Fact, LogIfTooSlow]
            public void TellsQueueToStartSyncAfterQueingPull()
            {
                CallMethod();

                Received.InOrder(() =>
                {
                    Queue.QueuePullSync();
                    Queue.Dequeue();
                });
            }

            [Property]
            public void SavesTheTimeOfSyncAttempt(DateTimeOffset now)
            {
                TimeService.CurrentDateTime.Returns(now);

                SyncManager.ForceFullSync();
                OrchestratorStates.OnNext(Pull);

                LastTimeUsageStorage.Received().SetFullSyncAttempt(now);
            }

            [Property]
            public void SavesTheTimeOfSuccessfulFullSync(DateTimeOffset now)
            {
                TimeService.CurrentDateTime.Returns(now);

                var observable = SyncManager.ForceFullSync();
                OrchestratorStates.OnNext(Pull);
                OrchestratorStates.OnNext(Sleep);
                observable.Wait();

                LastTimeUsageStorage.Received().SetSuccessfulFullSync(now);
            }
        }

        public sealed class TheFreezeMethod : SyncManagerTestBase
        {
            [Fact, LogIfTooSlow]
            public void DoesNotThrowWhenFreezingAFrozenSyncManager()
            {
                SyncManager.Freeze();

                Action freezing = () => SyncManager.Freeze();

                freezing.Should().NotThrow();
            }

            [Fact, LogIfTooSlow]
            public void FreezingAFrozenSyncManagerImmediatellyReturnsSleep()
            {
                SyncState? firstState = null;
                SyncManager.Freeze();

                var observable = SyncManager.Freeze();
                var subscription = observable.Subscribe(state => firstState = state);

                firstState.Should().Be(Sleep);
            }

            [Fact, LogIfTooSlow]
            public void FreezingSyncManagerWhenNoSyncIsRunningImmediatellyReturnsSleep()
            {
                SyncState? firstState = null;

                var observable = SyncManager.Freeze();
                var subscription = observable.Subscribe(state => firstState = state);

                firstState.Should().Be(Sleep);
            }

            [Fact, LogIfTooSlow]
            public void RunningPushSyncOnFrozenSyncManagerGoesDirectlyToSleepState()
            {
                SyncManager.Freeze();

                SyncManager.PushSync();

                Orchestrator.Received(1).Start(Arg.Is(Sleep));
                Orchestrator.DidNotReceive().Start(Arg.Is(Push));
                Orchestrator.DidNotReceive().Start(Arg.Is(Pull));
            }

            [Fact, LogIfTooSlow]
            public void RunningFullSyncOnFrozenSyncManagerGoesDirectlyToSleepState()
            {
                SyncManager.Freeze();

                SyncManager.ForceFullSync();

                Orchestrator.Received(1).Start(Arg.Is(Sleep));
                Orchestrator.DidNotReceive().Start(Arg.Is(Push));
                Orchestrator.DidNotReceive().Start(Arg.Is(Pull));
            }

            [Fact, LogIfTooSlow]
            public void KeepsWaitingWhileNoSleepStateOccursAfterFullSync()
            {
                bool finished = false;
                Queue.Dequeue().Returns(Pull);
                SyncManager.ForceFullSync();

                var observable = SyncManager.Freeze().Subscribe(_ => finished = true);
                OrchestratorStates.OnNext(Pull);
                OrchestratorStates.OnNext(Push);

                SyncManager.IsRunningSync.Should().BeTrue();
                finished.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void KeepsWaitingWhileNoSleepStateOccursAfterPushSync()
            {
                bool finished = false;
                Queue.Dequeue().Returns(Push);
                SyncManager.PushSync();

                var observable = SyncManager.Freeze().Subscribe(_ => finished = true);
                OrchestratorStates.OnNext(Push);

                SyncManager.IsRunningSync.Should().BeTrue();
                finished.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void CompletesWhenSleepStateOccursAfterFullSync()
            {
                bool finished = false;
                SyncManager.ForceFullSync();

                var observable = SyncManager.Freeze().Subscribe(_ => finished = true);
                OrchestratorStates.OnNext(Pull);
                OrchestratorStates.OnNext(Push);
                OrchestratorStates.OnNext(Sleep);

                SyncManager.IsRunningSync.Should().BeFalse();
                finished.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void CompletesWhenSleepStateOccursAfterPushSync()
            {
                bool finished = false;
                SyncManager.PushSync();

                var observable = SyncManager.Freeze().Subscribe(_ => finished = true);
                OrchestratorStates.OnNext(Push);
                OrchestratorStates.OnNext(Sleep);

                SyncManager.IsRunningSync.Should().BeFalse();
                finished.Should().BeTrue();
            }
        }

        public sealed class TheProgressObservable : SyncManagerTestBase
        {
            [Fact, LogIfTooSlow]
            public void EmitsTheUnknownSyncProgressBeforeAnyProgressIsEmitted()
            {
                SyncProgress? emitted = null;

                SyncManager.ProgressObservable.Subscribe(
                    progress => emitted = progress);

                emitted.Should().NotBeNull();
                emitted.Should().Be(SyncProgress.Unknown);
            }

            [Fact, LogIfTooSlow]
            public void EmitsSyncingWhenStartingPush()
            {
                SyncProgress? progressAfterPushing = null;
                Queue.Dequeue().Returns(Push);

                SyncManager.PushSync();
                SyncManager.ProgressObservable.Subscribe(
                    progress => progressAfterPushing = progress);

                progressAfterPushing.Should().NotBeNull();
                progressAfterPushing.Should().Be(SyncProgress.Syncing);
            }

            [Fact, LogIfTooSlow]
            public void EmitsSyncingWhenStartingFullSync()
            {
                SyncProgress? progressAfterFullSync = null;
                Queue.Dequeue().Returns(Pull);

                SyncManager.ForceFullSync();
                SyncManager.ProgressObservable.Subscribe(
                    progress => progressAfterFullSync = progress);

                progressAfterFullSync.Should().NotBeNull();
                progressAfterFullSync.Should().Be(SyncProgress.Syncing);
            }

            [Theory, LogIfTooSlow]
            [InlineData(Pull)]
            [InlineData(Push)]
            public void DoesNotEmitSyncedWhenSyncCompletesButAnotherSyncIsQueued(SyncState queuedState)
            {
                SyncProgress? emitted = null;
                Queue.Dequeue().Returns(queuedState);

                OrchestratorSyncComplete.OnNext(new Success(Pull));
                SyncManager.ProgressObservable.Subscribe(progress => emitted = progress);

                Orchestrator.Received().Start(queuedState);
                emitted.Should().NotBe(SyncProgress.Synced);
            }

            [Fact, LogIfTooSlow]
            public void EmitsSyncedWhenSyncCompletesAndQueueIsEmpty()
            {
                SyncProgress? emitted = null;
                Queue.Dequeue().Returns(Sleep);

                OrchestratorSyncComplete.OnNext(new Success(Pull));
                SyncManager.ProgressObservable.Subscribe(progress => emitted = progress);

                emitted.Should().NotBeNull();
                emitted.Should().Be(SyncProgress.Synced);
            }

            [Fact, LogIfTooSlow]
            public void EmitsOfflineModeDetectedWhenSyncFailsWithOfflineException()
            {
                SyncProgress? emitted = null;

                OrchestratorSyncComplete.OnNext(new Error(new OfflineException()));
                SyncManager.ProgressObservable.Subscribe(progress => emitted = progress);

                emitted.Should().NotBeNull();
                emitted.Should().Be(SyncProgress.OfflineModeDetected);
            }

            [Fact, LogIfTooSlow]
            public void EmitsFailedWhenSyncFailsWithUnknownException()
            {
                SyncProgress? emitted = null;

                OrchestratorSyncComplete.OnNext(new Error(new Exception()));
                SyncManager.ProgressObservable.Subscribe(progress => emitted = progress);

                emitted.Should().NotBeNull();
                emitted.Should().Be(SyncProgress.Failed);
            }

            [Theory, LogIfTooSlow]
            [MemberData(nameof(ExceptionsRethrownByProgressObservableOnError))]
            public void EmitsFailedWhenAKnownClientErrorExceptionIsReported(ClientErrorException exception)
            {
                SyncProgress? emitted = null;
                SyncManager.ProgressObservable.Subscribe(progress => emitted = progress, (Exception e) => { });

                OrchestratorSyncComplete.OnNext(new Error(exception));

                emitted.Should().NotBeNull();
                emitted.Should().Be(SyncProgress.Failed);
            }

            [Theory, LogIfTooSlow]
            [MemberData(nameof(ExceptionsRethrownByProgressObservableOnError))]
            public void ReportsTheErrorWhenAKnownClientErrorExceptionIsReported(ClientErrorException exception)
            {
                Exception caughtException = null;
                SyncManager.ProgressObservable.Subscribe(_ => { }, e => caughtException = e);

                OrchestratorSyncComplete.OnNext(new Error(exception));

                caughtException.Should().NotBeNull();
                caughtException.Should().Be(exception);
            }

            [Theory, LogIfTooSlow]
            [MemberData(nameof(ExceptionsRethrownByProgressObservableOnError))]
            public void FreezesTheSyncManagerWhenAKnownClientErrorExceptionIsReported(ClientErrorException exception)
            {
                OrchestratorSyncComplete.OnNext(new Error(exception));

                Orchestrator.Received().Freeze();
            }

            [Fact, LogIfTooSlow]
            public void EmitsTheLastValueAfterSubscribing()
            {
                SyncProgress? emitted = null;
                OrchestratorSyncComplete.OnNext(new Error(new Exception()));

                SyncManager.ProgressObservable.Subscribe(progress => emitted = progress);

                emitted.Should().NotBeNull();
                emitted.Should().Be(SyncProgress.Failed);
            }

            public static IEnumerable<object[]> ExceptionsRethrownByProgressObservableOnError()
                => new[]
                {
                    new object[] { new ClientDeprecatedException(Substitute.For<IRequest>(), Substitute.For<IResponse>()) },
                    new object[] { new ApiDeprecatedException(Substitute.For<IRequest>(), Substitute.For<IResponse>()) },
                    new object[] { new UnauthorizedException(Substitute.For<IRequest>(), Substitute.For<IResponse>()),  }
                };
        }

        public sealed class ErrorHandling : SyncManagerTestBase
        {
            [Fact, LogIfTooSlow]
            public void ClearsTheSyncQueueWhenAnErrorIsReported()
            {
                OrchestratorSyncComplete.OnNext(new Error(new Exception()));

                Queue.Received().Clear();
            }

            [Fact, LogIfTooSlow]
            public void UpdatesInternalStateSoItIsNotLockedForFutureSyncsAfterAnErrorIsReported()
            {
                OrchestratorSyncComplete.OnNext(new Error(new Exception()));

                SyncManager.IsRunningSync.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void PerformsThreadSafeClearingOfTheQueue()
            {
                var startQueueing = new AutoResetEvent(false);
                var startClearing = new AutoResetEvent(false);
                var queueCleared = new AutoResetEvent(false);
                int iterator = 0;
                int queued = -1;
                int cleared = -1;

                Queue.When(q => q.QueuePullSync()).Do(_ =>
                {
                    startClearing.Set();
                    Task.Delay(10).Wait();
                    queued = Interlocked.Increment(ref iterator);
                });

                Queue.When(q => q.Clear()).Do(_ =>
                {
                    cleared = Interlocked.Increment(ref iterator);
                    queueCleared.Set();
                });

                Task.Run(() =>
                {
                    startQueueing.WaitOne();
                    SyncManager.ForceFullSync();
                });

                Task.Run(() =>
                {
                    startClearing.WaitOne();
                    OrchestratorSyncComplete.OnNext(new Error(new Exception()));
                });

                startQueueing.Set();
                queueCleared.WaitOne();

                queued.Should().BeLessThan(cleared);
            }

            [Fact, LogIfTooSlow]
            public void ReportsErrorToTheAnalyticsService()
            {
                var exception = new Exception();

                OrchestratorSyncComplete.OnNext(new Error(exception));

                AnalyticsService.Received().Track(exception);
            }

            [Fact, LogIfTooSlow]
            public void ReportsOfflineExceptionsToTheAnalyticsServiceAsANormalEvent()
            {
                var exception = new OfflineException();

                OrchestratorSyncComplete.OnNext(new Error(exception));

                AnalyticsService.DidNotReceive().Track(Arg.Any<OfflineException>());
                AnalyticsService.OfflineModeDetected.Received().Track();
            }

            [Fact, LogIfTooSlow]
            public void GoesToSleepAfterAnErrorIsReported()
            {
                OrchestratorSyncComplete.OnNext(new Error(new Exception()));

                Orchestrator.Received().Start(Arg.Is(Sleep));
            }

            [Fact, LogIfTooSlow]
            public void DoesNotPreventFurtherSyncingAfterAnErrorWasReported()
            {
                OrchestratorSyncComplete.OnNext(new Error(new Exception()));
                Orchestrator.ClearReceivedCalls();
                Queue.When(q => q.QueuePushSync()).Do(_ => Queue.Dequeue().Returns(Push));

                SyncManager.PushSync();

                Orchestrator.Received().Start(Arg.Is(Push));
            }

        }
    }
}
