using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using NSubstitute.Core;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Tests.Mocks;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Xunit;

namespace Toggl.Foundation.Tests.DataSources
{
    public abstract class BaseDataSourceTests<TDataSource>
    {
        protected IIdProvider IdProvider { get; } = Substitute.For<IIdProvider>();
        protected ITimeService TimeService { get; } = Substitute.For<ITimeService>();
        protected ITogglDatabase DataBase { get; } = Substitute.For<ITogglDatabase>();

        protected TDataSource DataSource { get; private set; }

        protected BaseDataSourceTests()
        {
            Setup();
        }

        private void Setup()
        {
            DataSource = CreateDataSource();
        }

        protected abstract TDataSource CreateDataSource();
    }

    public sealed class DataSourceTests
    {
        private readonly ITimeService timeService = Substitute.For<ITimeService>();
        private readonly IRepository<IDatabaseTimeEntry> repository
            = Substitute.For<IRepository<IDatabaseTimeEntry>>();
        private readonly IAnalyticsService analyticsService = Substitute.For<IAnalyticsService>();

        private readonly DataSource<IThreadSafeTimeEntry, IDatabaseTimeEntry> dataSource;

        public DataSourceTests()
        {
            dataSource = new TimeEntriesDataSource(repository, timeService, analyticsService);
        }

        [Fact]
        public async Task TheDeleteAllMethodIgnoresTheConflictIfTheOldEntityIsNull()
        {
            var entities = Enumerable.Range(0, 10).Select(i => new MockTimeEntry { Id = i });

            repository.BatchUpdate(
                Arg.Any<IEnumerable<(long id, IDatabaseTimeEntry)>>(),
                Arg.Any<Func<IDatabaseTimeEntry, IDatabaseTimeEntry, ConflictResolutionMode>>())
                .Returns(batchUpdateResult);

            var results = await dataSource.DeleteAll(entities);
            results.OfType<IgnoreResult<IThreadSafeTimeEntry>>().Count().Should().Be(5);
        }

        private IObservable<IEnumerable<IConflictResolutionResult<IDatabaseTimeEntry>>> batchUpdateResult(CallInfo info)
        {
            var conflictFn =
                info.Arg<Func<IDatabaseTimeEntry, IDatabaseTimeEntry, ConflictResolutionMode>>();

            var entitiesToDelete =
                info.Arg<IEnumerable<(long Id, IDatabaseTimeEntry Entity)>>();

            var result = entitiesToDelete.Select(ignoreResultFromTuple);

            return Observable.Return(result);

            IConflictResolutionResult<IDatabaseTimeEntry> ignoreResultFromTuple((long Id, IDatabaseTimeEntry Entity) tuple)
            {
                var entity = tuple.Id % 2 == 0 ? null : tuple.Entity;
                var confictMode = conflictFn(entity, null);
                switch (confictMode)
                {
                    case ConflictResolutionMode.Ignore:
                        return new IgnoreResult<IDatabaseTimeEntry>(tuple.Id);

                    case ConflictResolutionMode.Delete:
                        return new DeleteResult<IDatabaseTimeEntry>(tuple.Id);

                    default:
                        throw new InvalidOperationException("Unexpected conflict resolution mode in DeleteAll");
                }
            }
        }
    }
}
