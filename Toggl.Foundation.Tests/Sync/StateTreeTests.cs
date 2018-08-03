using System;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Tests.Generators;
using Xunit;

namespace Toggl.Foundation.Tests.Sync
{
    public sealed class TransitionHandlerProviderTests
    {
        public abstract class ConfigureTransitionMethodTests<TStateResult, TStateFactory>
            where TStateResult : class, new()
            where TStateFactory : class
        {
            protected TransitionHandlerProvider Provider { get; } = new TransitionHandlerProvider();

            protected abstract void CallMethod(TStateResult result, TStateFactory factory);
            protected abstract TStateFactory ToStateFactory(Action action);

            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyArgumentIsNull(bool useStateResult, bool useStateFactory)
            {
                var stateResult = useStateResult ? new TStateResult() : null;
                var stateFactory = useStateFactory ? Substitute.For<TStateFactory>() : null;

                Action callingMethod = () => CallMethod(stateResult, stateFactory);

                callingMethod.Should().Throw<ArgumentNullException>();
            }

            [Fact, LogIfTooSlow]
            public void ThrowsIfCalledTwiceWithSameStateResult()
            {
                var stateResult = new TStateResult();
                CallMethod(stateResult, Substitute.For<TStateFactory>());

                Action callingMethodSecondTime =
                    () => CallMethod(stateResult, Substitute.For<TStateFactory>());

                callingMethodSecondTime.Should().Throw<Exception>();
            }

            [Fact, LogIfTooSlow]
            public void DoesNotCallProvidedStatesStartMethod()
            {
                var factoryWasCalled = false;

                CallMethod(new TStateResult(), ToStateFactory(() => factoryWasCalled = true));

                factoryWasCalled.Should().BeFalse();
            }
        }

        public sealed class TheConfigureTransitionMethod
            : ConfigureTransitionMethodTests<StateResult, ISyncState>
        {
            protected override void CallMethod(StateResult result, ISyncState factory)
                => Provider.ConfigureTransition(result, factory);

            protected override ISyncState ToStateFactory(Action action)
                => new TestSyncState(() => { action(); return null; });
        }

        public sealed class TheGenericConfigureTransitionMethod
            : ConfigureTransitionMethodTests<StateResult<object>, ISyncState<object>>
        {
            protected override void CallMethod(StateResult<object> result, ISyncState<object> factory)
                => Provider.ConfigureTransition(result, factory);

            protected override ISyncState<object> ToStateFactory(Action action)
                => new TestSyncState<object>(_ => { action(); return null; });
        }

        public sealed class TheGetTransitionHandlerMethod
        {
            private TransitionHandlerProvider provider { get; } = new TransitionHandlerProvider();

            [Fact, LogIfTooSlow]
            public void ThrowsIfArgumentIsNull()
            {
                Action callingMethodWithNull = () => provider.GetTransitionHandler(null);

                callingMethodWithNull.Should().Throw<ArgumentNullException>();
            }

            [Fact, LogIfTooSlow]
            public void ReturnsNullIfThereAreNoHandlers()
            {
                var handler = provider.GetTransitionHandler(Substitute.For<IStateResult>());

                handler.Should().BeNull();
            }

            [Fact, LogIfTooSlow]
            public void ReturnsNullIfTheStateResultIsUnknown()
            {
                provider.ConfigureTransition(new StateResult(), state(() => null));
                provider.ConfigureTransition(new StateResult<object>(), state(_ => null));

                var handler = provider.GetTransitionHandler(Substitute.For<IStateResult>());

                handler.Should().BeNull();
            }

            [Fact, LogIfTooSlow]
            public void ReturnsHandlerThatCallsProvidedStateFactory()
            {
                var stateResult = new StateResult();
                var expectedResult = Substitute.For<IObservable<ITransition>>();
                provider.ConfigureTransition(stateResult, state(() => expectedResult));

                var handler = provider.GetTransitionHandler(stateResult);
                var actualResult = handler(null);

                actualResult.Should().Be(expectedResult);
            }

            [Fact, LogIfTooSlow]
            public void ReturnsHandlerThatCallsProvidedGenericStateFactory()
            {
                var stateResult = new StateResult<object>();
                var expectedResult = Substitute.For<IObservable<ITransition>>();
                provider.ConfigureTransition(stateResult, state(_ => expectedResult));

                var handler = provider.GetTransitionHandler(stateResult);
                var actualResult = handler(stateResult.Transition(null));

                actualResult.Should().Be(expectedResult);
            }

            [Fact, LogIfTooSlow]
            public void ReturnsHandlerThatCallsProvidedGenericStateFactoryWithCorrectArgument()
            {
                var stateResult = new StateResult<object>();
                var expectedArgument = new object();
                object actualArgument = null;
                provider.ConfigureTransition(stateResult, state(obj => { actualArgument = obj; return null; }));

                var handler = provider.GetTransitionHandler(stateResult);
                handler(stateResult.Transition(expectedArgument));

                actualArgument.Should().Be(expectedArgument);
            }

            private ISyncState state(Func<IObservable<ITransition>> start)
                => new TestSyncState(start);
            private ISyncState<object> state(Func<object, IObservable<ITransition>> start)
                => new TestSyncState<object>(start);
        }
    }
}
