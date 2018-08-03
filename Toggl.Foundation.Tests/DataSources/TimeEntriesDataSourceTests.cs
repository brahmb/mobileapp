using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using NSubstitute.Routing.Handlers;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.DTOs;
using Toggl.Foundation.Exceptions;
using Toggl.Foundation.Models;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Tests.Generators;
using Toggl.Foundation.Tests.Mocks;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Exceptions;
using Toggl.PrimeRadiant.Models;
using Xunit;
using ThreadingTask = System.Threading.Tasks.Task;

namespace Toggl.Foundation.Tests.DataSources
{
    public sealed class TimeEntriesDataSourceTests
    {
        public abstract class TimeEntryDataSourceTest : BaseInteractorTests
        {
            protected const long ProjectId = 10;

            protected const long WorkspaceId = 11;

            protected const long UserId = 12;

            protected const long CurrentRunningId = 13;

            protected const long TaskId = 14;

            protected ITimeEntriesSource TimeEntriesSource { get; }

            protected TestScheduler TestScheduler { get; } = new TestScheduler();

            protected static DateTimeOffset Now { get; } = new DateTimeOffset(2018, 05, 14, 18, 00, 00, TimeSpan.Zero);

            protected IThreadSafeTimeEntry TimeEntry { get; } =
                Models.TimeEntry.Builder
                    .Create(CurrentRunningId)
                    .SetUserId(UserId)
                    .SetDescription("")
                    .SetWorkspaceId(WorkspaceId)
                    .SetSyncStatus(SyncStatus.InSync)
                    .SetAt(Now.AddDays(-1))
                    .SetStart(Now.AddHours(-2))
                    .Build();

            protected IRepository<IDatabaseTimeEntry> Repository { get; } = Substitute.For<IRepository<IDatabaseTimeEntry>>();

            protected TimeEntryDataSourceTest()
            {
                TimeEntriesSource = new TimeEntriesDataSource(Repository, TimeService, AnalyticsService);

                IdProvider.GetNextIdentifier().Returns(-1);
                Repository.GetById(Arg.Is(TimeEntry.Id)).Returns(Observable.Return(TimeEntry));

                Repository.Create(Arg.Any<IDatabaseTimeEntry>())
                          .Returns(info => Observable.Return(info.Arg<IDatabaseTimeEntry>()));

                Repository.Update(Arg.Any<long>(), Arg.Any<IDatabaseTimeEntry>())
                          .Returns(info => Observable.Return(info.Arg<IDatabaseTimeEntry>()));
            }
        }

        public sealed class TheConstructor : TimeEntryDataSourceTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useRepository,
                bool useTimeService,
                bool useAnalyticsService)
            {
                var repository = useRepository ? Repository : null;
                var timeService = useTimeService ? TimeService : null;
                var analyticsService = useAnalyticsService ? AnalyticsService : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new TimeEntriesDataSource(repository, timeService, analyticsService);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }

            [Fact]
            public void FixesMultipleRunningTimeEntriesDatabaseInconsistency()
            {
                Repository.GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>())
                    .Returns(
                        Observable.Return(new[]
                        {
                            new MockTimeEntry { Id = 1, Duration = null, IsDeleted = false },
                            new MockTimeEntry { Id = 2, Duration = null, IsDeleted = false },
                        }),
                        Observable.Return(new[]
                        {
                            new MockTimeEntry { Id = 1, Duration = null, IsDeleted = false }
                        }));

                // ReSharper disable once ObjectCreationAsStatement
                new TimeEntriesDataSource(Repository, TimeService, AnalyticsService);

                Repository.Received().BatchUpdate(
                    Arg.Is<IEnumerable<(long Id, IDatabaseTimeEntry Entity)>>(
                        timeEntries => timeEntries.Count() == 2
                                       && timeEntries.Any(tuple => tuple.Id == 1 && tuple.Entity.Duration == null)
                                       && timeEntries.Any(tuple => tuple.Id == 2 && tuple.Entity.Duration == null)),
                    Arg.Any<Func<IDatabaseTimeEntry, IDatabaseTimeEntry, ConflictResolutionMode>>(),
                    Arg.Is<IRivalsResolver<IDatabaseTimeEntry>>(rivalsResolver => rivalsResolver != null));
                AnalyticsService.TwoRunningTimeEntriesInconsistencyFixed.Received().Track();
            }

            [Fact]
            public void DoesNotTrackTheEventIfThereAreNotTwoRunningTimeEntries()
            {
                Repository.GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>())
                    .Returns(
                        Observable.Return(new[]
                        {
                            new MockTimeEntry { Id = 1, Duration = null, IsDeleted = false }
                        }));

                // ReSharper disable once ObjectCreationAsStatement
                new TimeEntriesDataSource(Repository, TimeService, AnalyticsService);

                AnalyticsService.TwoRunningTimeEntriesInconsistencyFixed.DidNotReceive().Track();
            }
        }

        public sealed class TheCreateMethod : TimeEntryDataSourceTest
        {
            [Fact]
            public async ThreadingTask CallsRepositoryWithConflictResolvers()
            {
                var timeEntry = new MockTimeEntry();
                Repository.BatchUpdate(null, null, null)
                    .ReturnsForAnyArgs(Observable.Return(new[] { new CreateResult<IDatabaseTimeEntry>(timeEntry) }));

                await TimeEntriesSource.Create(timeEntry);

                await Repository.Received().BatchUpdate(
                    Arg.Any<IEnumerable<(long, IDatabaseTimeEntry)>>(),
                    Arg.Is<Func<IDatabaseTimeEntry, IDatabaseTimeEntry, ConflictResolutionMode>>(conflictResolution => conflictResolution != null),
                    Arg.Is<IRivalsResolver<IDatabaseTimeEntry>>(resolver => resolver != null));
            }

            [Fact]
            public async ThreadingTask EmitsObservableEventsForTheNewlyCreatedRunningTimeEntry()
            {
                var createdObserver = TestScheduler.CreateObserver<IThreadSafeTimeEntry>();
                var newTimeEntry = new MockTimeEntry { Id = -1, Duration = null };
                Repository.BatchUpdate(
                    Arg.Any<IEnumerable<(long, IDatabaseTimeEntry)>>(),
                    Arg.Any<Func<IDatabaseTimeEntry, IDatabaseTimeEntry, ConflictResolutionMode>>(),
                    Arg.Any<IRivalsResolver<IDatabaseTimeEntry>>())
                    .Returns(Observable.Return(new IConflictResolutionResult<IDatabaseTimeEntry>[]
                    {
                        new CreateResult<IDatabaseTimeEntry>(newTimeEntry)
                    }));

                var timeEntriesSource = new TimeEntriesDataSource(Repository, TimeService, AnalyticsService);
                timeEntriesSource.Created.Subscribe(createdObserver);
                await timeEntriesSource.Create(newTimeEntry);

                createdObserver.Messages.Single().Value.Value.Id.Should().Be(newTimeEntry.Id);
                createdObserver.Messages.Single().Value.Value.Duration.Should().BeNull();
            }

            [Fact]
            public async ThreadingTask EmitsObservableEventsForTheNewRunningTimeEntryAndTheStoppedTimeEntry()
            {
                var durationAfterStopping = 100;
                var updatedObserver = TestScheduler.CreateObserver<EntityUpdate<IThreadSafeTimeEntry>>();
                var createdObserver = TestScheduler.CreateObserver<IThreadSafeTimeEntry>();
                var runningTimeEntry = new MockTimeEntry { Id = 1, Duration = null };
                var newTimeEntry = new MockTimeEntry { Id = -2, Duration = null };
                Repository.GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>())
                    .Returns(Observable.Return(new IDatabaseTimeEntry[] { runningTimeEntry }));
                Repository.BatchUpdate(
                    Arg.Any<IEnumerable<(long, IDatabaseTimeEntry)>>(),
                    Arg.Any<Func<IDatabaseTimeEntry, IDatabaseTimeEntry, ConflictResolutionMode>>(),
                    Arg.Any<IRivalsResolver<IDatabaseTimeEntry>>())
                    .Returns(Observable.Return(new IConflictResolutionResult<IDatabaseTimeEntry>[]
                    {
                        new UpdateResult<IDatabaseTimeEntry>(runningTimeEntry.Id, runningTimeEntry.With(durationAfterStopping)),
                        new CreateResult<IDatabaseTimeEntry>(newTimeEntry)
                    }));

                var timeEntriesSource = new TimeEntriesDataSource(Repository, TimeService, AnalyticsService);
                timeEntriesSource.Updated.Subscribe(updatedObserver);
                timeEntriesSource.Created.Subscribe(createdObserver);
                await timeEntriesSource.Create(newTimeEntry);

                updatedObserver.Messages.Single().Value.Value.Entity.Duration.Should().Be(durationAfterStopping);
                createdObserver.Messages.Single().Value.Value.Id.Should().Be(newTimeEntry.Id);
                createdObserver.Messages.Single().Value.Value.Duration.Should().BeNull();
            }
        }

        public sealed class TheGetAllMethod : TimeEntryDataSourceTest
        {
            [Fact, LogIfTooSlow]
            public async ThreadingTask NeverReturnsDeletedTimeEntries()
            {
                var result = Enumerable
                    .Range(0, 20)
                    .Select(i =>
                    {
                        var isDeleted = i % 2 == 0;
                        var timeEntry = Substitute.For<IThreadSafeTimeEntry>();
                        timeEntry.Id.Returns(i);
                        timeEntry.IsDeleted.Returns(isDeleted);
                        return timeEntry;
                    });
                DataSource
                    .TimeEntries
                    .GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>())
                    .Returns(callInfo =>
                        Observable
                             .Return(result)
                             .Select(x => x.Where(callInfo.Arg<Func<IThreadSafeTimeEntry, bool>>())));

                var timeEntries = await InteractorFactory.GetAllNonDeletedTimeEntries().Execute()
                    .Select(tes => tes.Where(x => x.Id > 10));

                timeEntries.Should().HaveCount(5);
            }
        }

        public sealed class TheStopMethod : TimeEntryDataSourceTest
        {
            public TheStopMethod()
            {
                long duration = (long)(Now - TimeEntry.Start).TotalSeconds;
                var timeEntries = new List<IDatabaseTimeEntry>
                {
                    TimeEntry,
                    TimeEntry.With(duration),
                    TimeEntry.With(duration),
                    TimeEntry.With(duration)
                };

                Repository
                    .GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>())
                    .Returns(callInfo =>
                        Observable
                             .Return(timeEntries)
                             .Select(x => x.Where(callInfo.Arg<Func<IDatabaseTimeEntry, bool>>())));
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask UpdatesTheTimeEntrySettingItsDuration()
            {
                await TimeEntriesSource.Stop(Now); 
                await Repository.Received().Update(Arg.Any<long>(), Arg.Is<IDatabaseTimeEntry>(te => te.Duration == (long)(Now - te.Start).TotalSeconds));
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask UpdatesTheTimeEntryMakingItSyncNeeded()
            {
                await TimeEntriesSource.Stop(Now);

                await Repository.Received().Update(Arg.Any<long>(), Arg.Is<IDatabaseTimeEntry>(te => te.SyncStatus == SyncStatus.SyncNeeded));
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask SetsTheCurrentlyRunningTimeEntryToNull()
            {
                var observer = TestScheduler.CreateObserver<ITimeEntry>();
                TimeEntriesSource.CurrentlyRunningTimeEntry.Subscribe(observer);
                Repository.Update(Arg.Any<long>(), Arg.Any<IDatabaseTimeEntry>()).Returns(callInfo => Observable.Return(callInfo.Arg<IDatabaseTimeEntry>()));

                await TimeEntriesSource.Stop(Now);

                observer.Messages.Single().Value.Value.Should().BeNull();
            }

            [Fact, LogIfTooSlow]
            public void ThrowsIfThereAreNoRunningTimeEntries()
            {
                long duration = (long)(Now - TimeEntry.Start).TotalSeconds;
                var timeEntries = new List<IDatabaseTimeEntry>
                {
                    TimeEntry.With(duration),
                    TimeEntry.With(duration),
                    TimeEntry.With(duration)
                };

                Repository
                    .GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>())
                    .Returns(callInfo =>
                        Observable
                             .Return(timeEntries)
                             .Select(x => x.Where(callInfo.Arg<Func<IDatabaseTimeEntry, bool>>())));

                var observer = TestScheduler.CreateObserver<ITimeEntry>();
                var observable = TimeEntriesSource.Stop(Now);
                observable.Subscribe(observer);

                observer.Messages.Single().Value.Exception.Should().BeOfType<NoRunningTimeEntryException>();
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask EmitsANewEventOnTheTimeEntryUpdatedObservable()
            {
                var observer = TestScheduler.CreateObserver<EntityUpdate<IThreadSafeTimeEntry>>();
                TimeEntriesSource.Updated.Subscribe(observer);

                await TimeEntriesSource.Stop(Now);

                var tuple = observer.Messages.Single().Value.Value;
                tuple.Id.Should().Be(CurrentRunningId);
                tuple.Entity.Duration.Should().Be((long)(Now - tuple.Entity.Start).TotalSeconds);
            }
        }

        public sealed class TheSoftDeleteMethod : TimeEntryDataSourceTest
        {
            [Fact, LogIfTooSlow]
            public async ThreadingTask SetsTheDeletedFlag()
            {
                await TimeEntriesSource.SoftDelete(TimeEntry).LastOrDefaultAsync();

                await Repository.Received().Update(Arg.Is(TimeEntry.Id), Arg.Is<IDatabaseTimeEntry>(te => te.IsDeleted == true));
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask SetsTheSyncNeededStatus()
            {
                await TimeEntriesSource.SoftDelete(TimeEntry).LastOrDefaultAsync();

                await Repository.Received().Update(Arg.Is(TimeEntry.Id), Arg.Is<IDatabaseTimeEntry>(te => te.SyncStatus == SyncStatus.SyncNeeded));
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask UpdatesTheCorrectTimeEntry()
            {
                await TimeEntriesSource.SoftDelete(TimeEntry).LastOrDefaultAsync();

                await Repository.Received().Update(Arg.Is(TimeEntry.Id), Arg.Is<IDatabaseTimeEntry>(te => te.Id == TimeEntry.Id));
            }

            [Fact, LogIfTooSlow]
            public void EmitsSingleElementBeforeCompleting()
            {
                var observer = Substitute.For<IObserver<Unit>>();

                TimeEntriesSource.SoftDelete(TimeEntry).Subscribe(observer);

                observer.Received(1).OnNext(Arg.Any<Unit>());
                observer.Received(1).OnCompleted();
            }

            [Fact, LogIfTooSlow]
            public void PropagatesErrorIfUpdateFails()
            {
                var timeEntry = Models.TimeEntry.Builder.Create(12)
                      .SetStart(Now)
                      .SetSyncStatus(SyncStatus.InSync)
                      .SetDescription("")
                      .SetUserId(11)
                      .SetWorkspaceId(10)
                      .SetAt(Now)
                      .Build();

                var timeEntryObservable = Observable.Return(timeEntry);
                var errorObservable = Observable.Throw<IDatabaseTimeEntry>(new DatabaseOperationException<IDatabaseTimeEntry>(new Exception()));
                Repository.GetById(Arg.Is(timeEntry.Id)).Returns(timeEntryObservable);
                Repository.Update(Arg.Any<long>(), Arg.Any<IDatabaseTimeEntry>()).Returns(errorObservable);
                var observer = Substitute.For<IObserver<Unit>>();

                TimeEntriesSource.SoftDelete(timeEntry).Subscribe(observer);

                observer.Received().OnError(Arg.Any<DatabaseOperationException<IDatabaseTimeEntry>>());
            }

            [Fact, LogIfTooSlow]
            public void PropagatesErrorIfTimeEntryIsNotInRepository()
            {
                var otherTimeEntry = Substitute.For<IThreadSafeTimeEntry>();
                otherTimeEntry.Id.Returns(12);
                var observer = Substitute.For<IObserver<Unit>>();
                Repository.Update(Arg.Any<long>(), Arg.Any<IThreadSafeTimeEntry>())
                          .Returns(Observable.Throw<IDatabaseTimeEntry>(new DatabaseOperationException<IDatabaseTimeEntry>(new Exception())));

                TimeEntriesSource.SoftDelete(otherTimeEntry).Subscribe(observer);

                observer.Received().OnError(Arg.Any<DatabaseOperationException<IDatabaseTimeEntry>>());
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask RemovesCurrentlyRunningTimeEntryWhenItIsDeleted()
            {
                DataSource.TimeEntries.Returns(TimeEntriesSource);
                var runningTimeEntriesHistory = new List<IThreadSafeTimeEntry>();
                var user = Substitute.For<IThreadSafeUser>();
                user.Id.Returns(10);
                DataSource.User.Current.Returns(Observable.Return(user));
                TimeEntriesSource.CurrentlyRunningTimeEntry
                    .Subscribe(te => runningTimeEntriesHistory.Add(te));
                var prototype = new MockTimeEntryPrototype
                {
                    WorkspaceId = WorkspaceId,
                    StartTime = Now,
                    Description = "Some description",
                    IsBillable = true,
                    ProjectId = ProjectId
                };
                prepareBatchUpdate();
                var timeEntry = Models.TimeEntry.From(await InteractorFactory.CreateTimeEntry(prototype).Execute());
                Repository.Update(Arg.Is(timeEntry.Id), Arg.Is(timeEntry)).Returns(Observable.Return(timeEntry));

                TimeEntriesSource.SoftDelete(timeEntry).Wait();

                runningTimeEntriesHistory.Should().HaveCount(3);
                runningTimeEntriesHistory[0].Should().Be(null); // originally there is no running time entry (in the repository)
                runningTimeEntriesHistory[1].Id.Should().Be(timeEntry.Id);
                runningTimeEntriesHistory[2].Should().Be(null);
            }

            private void prepareBatchUpdate()
            {
                Repository.BatchUpdate(
                        Arg.Any<IEnumerable<(long, IDatabaseTimeEntry)>>(),
                        Arg.Any<Func<IDatabaseTimeEntry, IDatabaseTimeEntry, ConflictResolutionMode>>(),
                        Arg.Any<IRivalsResolver<IDatabaseTimeEntry>>())
                    .Returns(info => Observable.Return(new[] { new CreateResult<IDatabaseTimeEntry>(info.Arg<IEnumerable<(long, IDatabaseTimeEntry Entity)>>().First().Entity) }));
            }
        }

        public sealed class TheUpdateMethod : TimeEntryDataSourceTest
        {
            private EditTimeEntryDto prepareTest()
            {
                var observable = Observable.Return(TimeEntry);
                Repository.GetById(Arg.Is(TimeEntry.Id)).Returns(observable);
                return new EditTimeEntryDto
                {
                    Id = TimeEntry.Id,
                    Description = "New description",
                    StartTime = DateTimeOffset.UtcNow,
                    ProjectId = 13,
                    Billable = true,
                    WorkspaceId = 71,
                    TagIds = new long[] { 1, 10, 34, 42 }
                };
            }

            private bool ensurePropertiesDidNotChange(IDatabaseTimeEntry timeEntry)
                => timeEntry.Id == TimeEntry.Id
                && timeEntry.UserId == TimeEntry.UserId
                && timeEntry.IsDeleted == TimeEntry.IsDeleted
                && timeEntry.ServerDeletedAt == TimeEntry.ServerDeletedAt;

            [Fact, LogIfTooSlow]
            public async ThreadingTask UpdatesTheDescriptionProperty()
            {
                var dto = prepareTest();

                await TimeEntriesSource.Update(dto);

                await Repository.Received().Update(Arg.Is(dto.Id), Arg.Is<IDatabaseTimeEntry>(te => te.Description == dto.Description));
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask UpdatesTheSyncStatusProperty()
            {
                var dto = prepareTest();

                await TimeEntriesSource.Update(dto);

                await Repository.Received().Update(Arg.Is(dto.Id), Arg.Is<IDatabaseTimeEntry>(te => te.SyncStatus == SyncStatus.SyncNeeded));
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask UpdatesTheAtProperty()
            {
                var dto = prepareTest();
                TimeService.CurrentDateTime.Returns(DateTimeOffset.UtcNow);

                await TimeEntriesSource.Update(dto);

                await Repository.Received().Update(Arg.Is(dto.Id), Arg.Is<IDatabaseTimeEntry>(te => te.At > TimeEntry.At));
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask UpdatesTheProjectId()
            {
                var dto = prepareTest();

                await TimeEntriesSource.Update(dto);

                await Repository.Received().Update(Arg.Is(dto.Id), Arg.Is<IDatabaseTimeEntry>(te => te.ProjectId == dto.ProjectId));
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask UpdatesTheBillbaleFlag()
            {
                var dto = prepareTest();

                await TimeEntriesSource.Update(dto);

                await Repository.Received().Update(Arg.Is(dto.Id), Arg.Is<IDatabaseTimeEntry>(te => te.Billable == dto.Billable));
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask UpdatesTheTagIds()
            {
                var dto = prepareTest();

                await TimeEntriesSource.Update(dto);

                await Repository.Received().Update(Arg.Is(dto.Id), Arg.Is<IDatabaseTimeEntry>(te => te.TagIds.SequenceEqual(dto.TagIds)));
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask UpdatesTheWorkspaceId()
            {
                var dto = prepareTest();

                await TimeEntriesSource.Update(dto);

                await Repository.Received().Update(
                    Arg.Is(dto.Id),
                    Arg.Is<IDatabaseTimeEntry>(te => te.WorkspaceId == dto.WorkspaceId));
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask LeavesAllOtherPropertiesUnchanged()
            {
                var dto = prepareTest();

                await TimeEntriesSource.Update(dto);

                await Repository.Received().Update(Arg.Is(dto.Id), Arg.Is<IDatabaseTimeEntry>(te => ensurePropertiesDidNotChange(te)));
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask NotifiesAboutTheUpdate()
            {
                var observable = Observable.Return(TimeEntry);
                Repository.GetById(Arg.Is(TimeEntry.Id)).Returns(observable);
                var dto = new EditTimeEntryDto { Id = TimeEntry.Id, Description = "New description", StartTime = DateTimeOffset.UtcNow, WorkspaceId = 71 };
                var observer = Substitute.For<IObserver<EntityUpdate<IThreadSafeTimeEntry>>>();
                TimeEntriesSource.Updated.Subscribe(observer);

                await TimeEntriesSource.Update(dto);

                observer.Received().OnNext(Arg.Is<EntityUpdate<IThreadSafeTimeEntry>>(
                    update => update.Id == dto.Id && update.Entity.Id == dto.Id));
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask UsesTheUpdatedEntityFromTheRepositoryAndNotTheArgumentToUpdateTheCurrentlyRunningTimeEntry()
            {
                var observable = Observable.Return(TimeEntry);
                Repository.GetById(Arg.Is(TimeEntry.Id)).Returns(observable);
                var updatedTimeEntry = Substitute.For<IDatabaseTimeEntry>();
                updatedTimeEntry.Id.Returns(123);
                Repository.Update(TimeEntry.Id, Arg.Any<IDatabaseTimeEntry>()).Returns(Observable.Return(updatedTimeEntry));
                var dto = new EditTimeEntryDto { Id = TimeEntry.Id, Description = "New description", StartTime = DateTimeOffset.UtcNow, WorkspaceId = 71 };
                var observer = Substitute.For<IObserver<EntityUpdate<IThreadSafeTimeEntry>>>();
                TimeEntriesSource.Updated.Subscribe(observer);

                await TimeEntriesSource.Update(dto);
                var calls = observer.ReceivedCalls();

                observer.Received().OnNext(Arg.Is<EntityUpdate<IThreadSafeTimeEntry>>(
                    update => update.Id == updatedTimeEntry.Id && update.Entity.Id == updatedTimeEntry.Id));
            }
        }
    }
}
