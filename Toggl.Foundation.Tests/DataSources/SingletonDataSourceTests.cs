using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Tests.Sync.States;
using Toggl.PrimeRadiant;
using Xunit;

namespace Toggl.Foundation.Tests.DataSources
{
    public sealed class SingletonDataSourceTests
    {
        public sealed class TheConstructor
        {
            [Fact]
            public void ThrowsWhenRequiredParameterIsNull()
            {
                Action createDataSource = () => new TestSingletonSource(null, null);

                createDataSource.Should().Throw<ArgumentNullException>();
            }

            [Fact]
            public async Task InitializesTheCurrentObservableWithTheDefaultValueIfTheStorageDoesNotContainTheObject()
            {
                var storage = Substitute.For<ISingleObjectStorage<IDatabaseTestModel>>();
                var defaultValue = new TestModel(123, SyncStatus.InSync);
                storage.Single().Returns(Observable.Throw<IDatabaseTestModel>(new Exception()));

                var dataSource = new TestSingletonSource(storage, defaultValue);
                var value = await dataSource.Current.FirstAsync();

                value.Id.Should().Be(defaultValue.Id);
            }

            [Fact]
            public async Task InitializesTheCurrentObservableWithTheStoredValueFromTheStorage()
            {
                var storage = Substitute.For<ISingleObjectStorage<IDatabaseTestModel>>();
                var storedValue = new TestModel(456, SyncStatus.InSync);
                storage.Single().Returns(Observable.Return(storedValue));

                var dataSource = new TestSingletonSource(storage, null);
                var value = await dataSource.Current.FirstAsync();

                value.Id.Should().Be(storedValue.Id);
            }

            [Fact]
            public async Task IgnoresTheDefaultValueWhenTheStorageContainsSomeValue()
            {
                var storage = Substitute.For<ISingleObjectStorage<IDatabaseTestModel>>();
                var defaultValue = new TestModel(123, SyncStatus.InSync);
                var storedValue = new TestModel(456, SyncStatus.InSync);
                storage.Single().Returns(Observable.Return(storedValue));

                var dataSource = new TestSingletonSource(storage, defaultValue);
                var value = await dataSource.Current.FirstAsync();

                value.Id.Should().Be(storedValue.Id);
            }
        }

        public sealed class TheCreateMethod
        {
            [Fact]
            public async Task UpdatesTheCurrentObservableValue()
            {
                var storage = Substitute.For<ISingleObjectStorage<IDatabaseTestModel>>();
                var createdValue = new TestModel(123, SyncStatus.InSync);
                var storedValue = new TestModel(456, SyncStatus.InSync);
                storage.Single().Returns(Observable.Return(storedValue));
                storage.Create(createdValue).Returns(Observable.Return(createdValue));

                var dataSource = new TestSingletonSource(storage, null);
                await dataSource.Create(createdValue);
                var value = await dataSource.Current.FirstAsync();

                value.Id.Should().Be(createdValue.Id);
            }
        }

        public sealed class TheUpdateMethod
        {
            [Fact]
            public async Task UpdatesTheCurrentObservableValue()
            {
                var storage = Substitute.For<ISingleObjectStorage<IDatabaseTestModel>>();
                var updatedValue = new TestModel(123, SyncStatus.InSync);
                var storedValue = new TestModel(456, SyncStatus.InSync);
                storage.Single().Returns(Observable.Return(storedValue));
                storage.Update(updatedValue.Id, updatedValue).Returns(Observable.Return(updatedValue));

                var dataSource = new TestSingletonSource(storage, null);
                await dataSource.Update(updatedValue);
                var value = await dataSource.Current.FirstAsync();

                value.Id.Should().Be(updatedValue.Id);
            }
        }

        public sealed class TheOverwriteIfOriginalDidNotChangeMethod
        {
            [Fact]
            public async Task UpdatesTheCurrentObservableValue()
            {
                var storage = Substitute.For<ISingleObjectStorage<IDatabaseTestModel>>();
                var updatedValue = new TestModel(123, SyncStatus.InSync);
                var storedValue = new TestModel(456, SyncStatus.InSync);
                storage.Single().Returns(Observable.Return(storedValue));
                storage.BatchUpdate(null, null, null)
                    .ReturnsForAnyArgs(Observable.Return(new[] { new UpdateResult<IDatabaseTestModel>(storedValue.Id, updatedValue) }));

                var dataSource = new TestSingletonSource(storage, null);
                await dataSource.OverwriteIfOriginalDidNotChange(storedValue, updatedValue);
                var value = await dataSource.Current.FirstAsync();

                value.Id.Should().Be(updatedValue.Id);
            }
        }

        public sealed class TheUpdateWithConflictResolutionMethod
        {
            [Fact]
            public async Task UpdatesTheCurrentObservableValueWhenTheStoredValueIsCreated()
            {
                var storage = Substitute.For<ISingleObjectStorage<IDatabaseTestModel>>();
                var createdValue = new TestModel(123, SyncStatus.InSync);
                storage.Single().Returns(Observable.Throw<IDatabaseTestModel>(new Exception()));
                storage.BatchUpdate(null, null, null)
                    .ReturnsForAnyArgs(Observable.Return(new[] { new CreateResult<IDatabaseTestModel>(createdValue) }));

                var dataSource = new TestSingletonSource(storage, null);
                await dataSource.UpdateWithConflictResolution(createdValue);
                var value = await dataSource.Current.FirstAsync();

                value.Id.Should().Be(createdValue.Id);
            }

            [Fact]
            public async Task UpdatesTheCurrentObservableValueWhenTheStoredValueIsUpdated()
            {
                var storage = Substitute.For<ISingleObjectStorage<IDatabaseTestModel>>();
                var updatedValue = new TestModel(123, SyncStatus.InSync);
                var storedValue = new TestModel(456, SyncStatus.InSync);
                storage.Single().Returns(Observable.Return(storedValue));
                storage.BatchUpdate(null, null, null)
                    .ReturnsForAnyArgs(Observable.Return(new[] { new UpdateResult<IDatabaseTestModel>(storedValue.Id, updatedValue) }));

                var dataSource = new TestSingletonSource(storage, null);
                await dataSource.UpdateWithConflictResolution(updatedValue);
                var value = await dataSource.Current.FirstAsync();

                value.Id.Should().Be(updatedValue.Id);
            }
        }

        private class TestSingletonSource : SingletonDataSource<IThreadSafeTestModel, IDatabaseTestModel>
        {
            public TestSingletonSource(ISingleObjectStorage<IDatabaseTestModel> storage, IThreadSafeTestModel defaultCurrentValue) : base(storage, defaultCurrentValue)
            {
            }

            protected override IThreadSafeTestModel Convert(IDatabaseTestModel entity)
                => TestModel.From(entity);

            protected override ConflictResolutionMode ResolveConflicts(IDatabaseTestModel first,
                IDatabaseTestModel second)
                => ConflictResolutionMode.Ignore;
        }
    }
}

