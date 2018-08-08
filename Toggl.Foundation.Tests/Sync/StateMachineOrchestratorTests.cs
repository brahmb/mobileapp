using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using FluentAssertions;
using FsCheck.Xunit;
using NSubstitute;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Tests.Generators;
using Toggl.Foundation.Tests.Extensions;
using Xunit;
using static Toggl.Foundation.Sync.SyncState;

namespace Toggl.Foundation.Tests.Sync
{
    public sealed class StateMachineOrchestratorTests
    {
        public abstract class StateMachineOrchestratorBaseTests
        {
            private readonly Subject<StateMachineEvent> stateMachineEventSubject
                = new Subject<StateMachineEvent>();

            protected IStateMachine StateMachine = Substitute.For<IStateMachine>();
            protected StateMachineEntryPoints EntryPoints { get; } = new StateMachineEntryPoints();
            protected IStateMachineOrchestrator Orchestrator { get; }
            protected List<SyncState> StateEvents { get; } = new List<SyncState>();
            protected List<SyncState> CompletedEvents { get; } = new List<SyncState>();
            protected List<Exception> ReportedErrors { get; } = new List<Exception>();

            protected StateMachineOrchestratorBaseTests()
            {
                StateMachine.StateTransitions.Returns(stateMachineEventSubject.AsObservable());

                Orchestrator = new StateMachineOrchestrator(StateMachine, EntryPoints);

                Orchestrator.StateObservable.Subscribe(StateEvents.Add);
                Orchestrator.SyncCompleteObservable.Subscribe(handleSyncCompleteEvent);
            }

            protected void SendStateMachineEvent(StateMachineEvent @event)
            {
                stateMachineEventSubject.OnNext(@event);
            }

            private void handleSyncCompleteEvent(SyncResult result)
            {
                if (result is Error error)
                    ReportedErrors.Add(error.Exception);

                if (result is Success success)
                    CompletedEvents.Add(success.Operation);
            }
        }

        public sealed class TheConstructor
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyArgumentIsNull(bool useStateMachine, bool useEntryPoints)
            {
                var stateMachine = useStateMachine ? Substitute.For<IStateMachine>() : null;
                var entryPoints = useEntryPoints ? new StateMachineEntryPoints() : null;

                // ReSharper disable once ObjectCreationAsStatement
                Action constructing = () => new StateMachineOrchestrator(stateMachine, entryPoints);

                constructing.Should().Throw<ArgumentNullException>();
            }

            [Fact, LogIfTooSlow]
            public void SubscribesToStateMachineEvents()
            {
                var stateMachine = Substitute.For<IStateMachine>();
                var transitions = Substitute.For<IObservable<StateMachineEvent>>();
                stateMachine.StateTransitions.Returns(transitions);

                // ReSharper disable once ObjectCreationAsStatement
                new StateMachineOrchestrator(stateMachine, new StateMachineEntryPoints());

                transitions.Received().Subscribe(Arg.Any<IObserver<StateMachineEvent>>());
            }
        }

        public sealed class TheStateProperty : StateMachineOrchestratorBaseTests
        {
            [Fact, LogIfTooSlow]
            public void StartsWithSleep()
            {
                Orchestrator.State.Should().Be(Sleep);
            }
        }

        public abstract class StateChangeMethodTests : StateMachineOrchestratorBaseTests
        {
            protected abstract SyncState ExpectedState { get; }
            protected abstract void CallMethod();

            private Action callingMethod => CallMethod;

            [Fact, LogIfTooSlow]
            public void DoesNotThrowIfNotSyncing()
            {
                callingMethod.Should().NotThrow();
            }

            [Fact, LogIfTooSlow]
            public void DoesNotThrowIfSleepWasCalledLast()
            {
                Orchestrator.Start(Sleep);

                callingMethod.Should().NotThrow();
            }

            [Fact, LogIfTooSlow]
            public void ShouldThrowIfPullSyncing()
            {
                Orchestrator.Start(Pull);

                callingMethod.Should().Throw<InvalidOperationException>();
            }

            [Fact, LogIfTooSlow]
            public void ShouldThrowIfPushSyncing()
            {
                Orchestrator.Start(Push);

                callingMethod.Should().Throw<InvalidOperationException>();
            }

            [Fact, LogIfTooSlow]
            public void ShouldNotThrowIfPullSyncingCompleted()
            {
                Orchestrator.Start(Pull);
                SendStateMachineEvent(new StateMachineDeadEnd(null));

                callingMethod.Should().NotThrow();
            }

            [Fact, LogIfTooSlow]
            public void ShouldNotThrowIfPushSyncingCompleted()
            {
                Orchestrator.Start(Push);
                SendStateMachineEvent(new StateMachineDeadEnd(null));

                callingMethod.Should().NotThrow();
            }

            [Fact, LogIfTooSlow]
            public void ShouldNotThrowIfPullSyncingFailed()
            {
                Orchestrator.Start(Pull);
                SendStateMachineEvent(new StateMachineError(new Exception()));

                callingMethod.Should().NotThrow();
            }

            [Fact, LogIfTooSlow]
            public void ShouldNotThrowIfPushSyncingFailed()
            {
                Orchestrator.Start(Push);
                SendStateMachineEvent(new StateMachineError(new Exception()));

                callingMethod.Should().NotThrow();
            }

            [Fact, LogIfTooSlow]
            public void ShouldResultInExpectedState()
            {
                CallMethod();

                Orchestrator.State.Should().Be(ExpectedState);
            }

            [Fact, LogIfTooSlow]
            public void ShouldCauseExpectedStateEvent()
            {
                StateEvents.Clear();

                CallMethod();

                StateEvents.ShouldBeSameEventsAs(
                    ExpectedState
                );
            }

            [Fact, LogIfTooSlow]
            public void ShouldNotCauseCompletedEvent()
            {
                CallMethod();

                CompletedEvents.Should().BeEmpty();
            }
        }

        public abstract class PullPushSyncMethodTests : StateChangeMethodTests
        {
            protected abstract StateResult EntryPoint { get; }

            [Fact, LogIfTooSlow]
            public void ShouldStartStateMachineWithCorrectEntryPoint()
            {
                CallMethod();

                StateMachine.Received().Start(
                    Arg.Is<ITransition>(t => t.Result == EntryPoint)
                );
            }

            [Fact, LogIfTooSlow]
            public void ShouldCauseExpectedCompletedEventWhenSyncingCompletes()
            {
                CallMethod();
                SendStateMachineEvent(new StateMachineDeadEnd(null));

                CompletedEvents.ShouldBeSameEventsAs(
                    ExpectedState
                );
            }

            [Fact, LogIfTooSlow]
            public void ShouldReportErrorWhenSyncingFails()
            {
                var exception = new Exception();
                CallMethod();
                SendStateMachineEvent(new StateMachineError(exception));

                ReportedErrors.Should().HaveCount(1);
                ReportedErrors[0].Should().Be(exception);
            }
        }

        public sealed class TheStartMethod
        {
            public sealed class TheStartPull : PullPushSyncMethodTests
            {
                protected override SyncState ExpectedState => Pull;
                protected override StateResult EntryPoint => EntryPoints.StartPullSync;
                protected override void CallMethod() => Orchestrator.Start(Pull);
            }

            public sealed class TheStartPush : PullPushSyncMethodTests
            {
                protected override SyncState ExpectedState => Push;
                protected override StateResult EntryPoint => EntryPoints.StartPushSync;
                protected override void CallMethod() => Orchestrator.Start(Push);
            }

            public sealed class TheStartCleanUp : PullPushSyncMethodTests
            {
                protected override SyncState ExpectedState => CleanUp;
                protected override StateResult EntryPoint => EntryPoints.StartCleanUp;
                protected override void CallMethod() => Orchestrator.Start(CleanUp);
            }

            public sealed class TheStartSleep : StateChangeMethodTests
            {
                protected override SyncState ExpectedState => Sleep;
                protected override void CallMethod() => Orchestrator.Start(Sleep);

                [Fact, LogIfTooSlow]
                public void ShouldNotStartStateMachine()
                {
                    CallMethod();

                    StateMachine.DidNotReceive().Start(Arg.Any<ITransition>());
                }
            }

            public sealed class TheStartUnknown : StateMachineOrchestratorBaseTests
            {
                [Property]
                public void ThrowsWhenUnknownSyncStateIsPassedToTheStartMethod(int state)
                {
                    if (Enum.IsDefined(typeof(SyncState), state)) return;

                    Action callingStartWithUnknownState = () => Orchestrator.Start((SyncState)state);

                    callingStartWithUnknownState.Should().Throw<ArgumentOutOfRangeException>();
                }
            }
        }

        public sealed class TheFreezeMethod : StateMachineOrchestratorBaseTests
        {
            [Fact, LogIfTooSlow]
            public void ShouldCallFreezeOnTheStateMachine()
            {
                Orchestrator.Freeze();

                StateMachine.Received().Freeze();
            }
        }
    }
}
