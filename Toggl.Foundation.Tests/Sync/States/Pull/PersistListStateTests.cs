using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Sync.States;
using Toggl.Foundation.Sync.States.Pull;
using Toggl.Foundation.Tests.Helpers;
using Toggl.PrimeRadiant;
using Toggl.Ultrawave.Exceptions;
using Xunit;

namespace Toggl.Foundation.Tests.Sync.States
{
    public sealed class PersistListStateTests
    {
        private readonly PersistListState<ITestModel, IDatabaseTestModel, IThreadSafeTestModel> state;

        private readonly IDataSource<IThreadSafeTestModel, IDatabaseTestModel> dataSource =
            Substitute.For<IDataSource<IThreadSafeTestModel, IDatabaseTestModel>>();

        private readonly DateTimeOffset now = new DateTimeOffset(2017, 04, 05, 12, 34, 56, TimeSpan.Zero);

        public PersistListStateTests()
        {
            state = new PersistListState<ITestModel, IDatabaseTestModel, IThreadSafeTestModel>(dataSource, TestModel.From);
        }

        [Fact, LogIfTooSlow]
        public async Task EmitsTransitionToPersistFinished()
        {
            var observables = createObservables();

            var transition = await state.Start(observables).SingleAsync();

            transition.Result.Should().Be(state.FinishedPersisting);
        }

        [Theory]
        [MemberData(nameof(ApiExceptions.ServerExceptions), MemberType = typeof(ApiExceptions))]
        [MemberData(nameof(ApiExceptions.ClientExceptionsWhichAreNotReThrownInSyncStates), MemberType = typeof(ApiExceptions))]
        public async Task ReturnsFailureResultWhenFetchingThrows(ApiException exception)
        {
            var fetchObservables = createFetchObservables(Observable.Throw<List<ITestModel>>(exception));

            var transition = await state.Start(fetchObservables);

            transition.Result.Should().Be(state.ErrorOccured);
        }

        [Fact, LogIfTooSlow]
        public void ThrowsIfFetchObservablePublishesTwice()
        {
            var fetchObservables = createFetchObservables(
                Observable.Create<List<ITestModel>>(observer =>
                {
                    observer.OnNext(new List<ITestModel>());
                    observer.OnNext(new List<ITestModel>());
                    return () => { };
                }));

            Action fetchTwice = () => state.Start(fetchObservables).Wait();

            fetchTwice.Should().Throw<InvalidOperationException>();
        }

        [Fact, LogIfTooSlow]
        public async Task HandlesNullValueReceivedFromTheServerAsAnEmptyList()
        {
            List<ITestModel> entities = null;
            var observables = createObservables(entities);

            var transition = (Transition<IFetchObservables>)(await state.Start(observables).SingleAsync());

            transition.Result.Should().Be(state.FinishedPersisting);
            await dataSource.Received().BatchUpdate(Arg.Is<IEnumerable<IThreadSafeTestModel>>(batch => batch.Count() == 0));
        }

        [Fact, LogIfTooSlow]
        public void ThrowsWhenBatchUpdateThrows()
        {
            var observables = createObservables();
            dataSource.BatchUpdate(Arg.Any<IEnumerable<IThreadSafeTestModel>>()).Returns(
                Observable.Throw<IEnumerable<IConflictResolutionResult<IThreadSafeTestModel>>>(new TestException()));

            Action startingState = () => state.Start(observables).SingleAsync().Wait();

            startingState.Should().Throw<TestException>();
        }

        [Fact, LogIfTooSlow]
        public void ThrowsWhenTheDeviceIsOffline()
        {
            var observables = createFetchObservables(Observable.Throw<List<ITestModel>>(new OfflineException(new Exception())));

            Action startingState = () => state.Start(observables).Wait();

            startingState.Should().Throw<OfflineException>();
        }

        private IFetchObservables createObservables(List<ITestModel> entities = null)
            => createFetchObservables(Observable.Return(entities));

        private IFetchObservables createFetchObservables(IObservable<List<ITestModel>> observable = null)
        {
            var observables = Substitute.For<IFetchObservables>();
            observables.GetList<ITestModel>().Returns(observable);
            return observables;
        }
    }
}
