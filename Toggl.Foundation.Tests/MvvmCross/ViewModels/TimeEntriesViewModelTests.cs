using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection.Metadata;
using System.Threading;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Helper;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Toggl.Foundation.Tests.Mocks;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Models;
using Xunit;
using ThreadingTask = System.Threading.Tasks.Task;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class TimeEntriesViewModelTests
    {
        public abstract class TimeEntriesViewModelTest
        {
            protected ITogglDataSource DataSource { get; } = Substitute.For<ITogglDataSource>();
            protected IInteractorFactory InteractorFactory { get; } = Substitute.For<IInteractorFactory>();
            protected IAnalyticsService AnalyticsService { get; } = Substitute.For<IAnalyticsService>();
            protected IScheduler Scheduler { get; } = DefaultScheduler.Instance;

            protected TimeEntriesViewModel ViewModel { get; private set; }

            protected TimeEntriesViewModel CreateViewModel()
                => new TimeEntriesViewModel(DataSource, InteractorFactory, AnalyticsService, Scheduler);

            protected TimeEntriesViewModelTest()
            {
                ViewModel = CreateViewModel();
            }
        }

        public sealed class TheConstructor : TimeEntriesViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useDataSource,
                bool useInteractorFactory,
                bool useAnalyticsService,
                bool useScheduler)
            {
                var dataSource = useDataSource ? DataSource : null;
                var interactorFactory = useInteractorFactory ? InteractorFactory : null;
                var analyticsService = useAnalyticsService ? AnalyticsService : null;
                var scheduler = useScheduler ? Scheduler : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new TimeEntriesViewModel(dataSource, interactorFactory, analyticsService, scheduler);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheTimeEntriesProperty : TimeEntriesViewModelTest
        {
            [Property]
            public Property ShouldBeOrderedAfterInitialization()
            {
                var arb = generatorForTimeEntriesViewModel(_ => true).ToArbitrary();
                return Prop.ForAll(arb, viewModel =>
                {
                    viewModel.Initialize().Wait();

                    for (int i = 1; i < viewModel.TimeEntries.Count(); i++)
                    {
                        var dateTime1 = viewModel.TimeEntries.ElementAt(i - 1).First().StartTime.Date;
                        var dateTime2 = viewModel.TimeEntries.ElementAt(i).First().StartTime.Date;
                        dateTime1.Should().BeAfter(dateTime2);
                    }
                });
            }

            [Property]
            public Property ShouldNotHaveEmptyGroups()
            {
                var arb = generatorForTimeEntriesViewModel(_ => true).ToArbitrary();
                return Prop.ForAll(arb, viewModel =>
                {

                    viewModel.Initialize().Wait();

                    foreach (var grouping in viewModel.TimeEntries)
                    {
                        grouping.Count().Should().BeGreaterOrEqualTo(1);
                    }
                });
            }

            [Property]
            public Property ShouldHaveOrderedGroupsAfterInitialization()
            {
                var arb =
                    generatorForTimeEntriesViewModel(m => m == DateTime.UtcNow.Month).ToArbitrary();

                return Prop.ForAll(arb, (Action<TimeEntriesViewModel>)(viewModel =>
                {
                    viewModel.Initialize().Wait();
                    foreach (var grouping in viewModel.TimeEntries)
                    {
                        for (int i = 1; i < grouping.Count(); i++)
                        {
                            var dateTime1 = grouping.ElementAt(i - 1).StartTime;
                            var dateTime2 = grouping.ElementAt(i).StartTime;
                            AssertionExtensions.Should(dateTime1).BeOnOrAfter((DateTimeOffset)dateTime2);
                        }
                    }
                }));
            }

            private Gen<TimeEntriesViewModel> generatorForTimeEntriesViewModel(Func<int, bool> filter)
            {
                var now = new DateTimeOffset(2017, 08, 13, 08, 01, 23, TimeSpan.Zero);
                var monthsGenerator = Gen.Choose(1, 12).Where(filter);
                var yearGenerator = Gen.Choose(2007, now.Year);

                return Arb.Default
                    .Array<DateTimeOffset>()
                    .Generator
                    .Select(dateTimes =>
                    {
                        var viewModel = CreateViewModel();
                        var year = yearGenerator.Sample(0, 1).First();

                        var observable = dateTimes
                            .Select(newDateWithGenerator(monthsGenerator, year))
                            .Select(d => TimeEntry.Builder
                                    .Create(-1)
                                    .SetUserId(-2)
                                    .SetWorkspaceId(-3)
                                    .SetStart(d)
                                    .SetDescription("")
                                    .SetAt(now).Build())
                            .Apply(Observable.Return);

                        InteractorFactory.GetAllNonDeletedTimeEntries().Execute().Returns(observable);

                        return viewModel;
                    });
            }

            private static Func<DateTimeOffset, DateTimeOffset> newDateWithGenerator(Gen<int> monthGenerator, int year)
            {
                var month = monthGenerator.Sample(0, 1).First();
                var day = Gen.Choose(1, DateTime.DaysInMonth(year, month)).Sample(0, 1).First();

                return dateTime =>
                    new DateTime(year, month, day, dateTime.Hour, dateTime.Minute, dateTime.Second);
            }
        }

        public abstract class TimeEntryDataSourceObservableTest : TimeEntriesViewModelTest
        {
            private static readonly DateTimeOffset now = new DateTimeOffset(2017, 01, 19, 07, 10, 00, TimeZone.CurrentTimeZone.GetUtcOffset(new DateTime(2017, 01, 19)));

            protected const int InitialAmountOfTimeEntries = 20;

            protected Subject<IThreadSafeTimeEntry> TimeEntryCreatedSubject = new Subject<IThreadSafeTimeEntry>();
            protected Subject<EntityUpdate<IThreadSafeTimeEntry>> TimeEntryUpdatedSubject = new Subject<EntityUpdate<IThreadSafeTimeEntry>>();
            protected Subject<long> TimeEntryDeletedSubject = new Subject<long>();
            protected IThreadSafeTimeEntry NewTimeEntry =
                TimeEntry.Builder.Create(21)
                         .SetUserId(10)
                         .SetWorkspaceId(12)
                         .SetDescription("")
                         .SetAt(now)
                         .SetStart(now)
                         .Build();

            protected TimeEntryDataSourceObservableTest()
            {
                var startTime = now.AddHours(-2);

                var observable = Enumerable.Range(1, InitialAmountOfTimeEntries)
                    .Select(i => TimeEntry.Builder.Create(i))
                    .Select(builder => builder
                        .SetStart(startTime.AddHours(builder.Id * 2))
                        .SetUserId(11)
                        .SetWorkspaceId(12)
                        .SetDescription("")
                        .SetAt(now)
                        .Build())
                  .Select(te => te.With((long)TimeSpan.FromHours(te.Id * 2 + 2).TotalSeconds))
                  .Apply(Observable.Return);

                InteractorFactory.GetAllNonDeletedTimeEntries().Execute().Returns(observable);
                DataSource.TimeEntries.Created.Returns(TimeEntryCreatedSubject.AsObservable());
                DataSource.TimeEntries.Updated.Returns(TimeEntryUpdatedSubject.AsObservable());
                DataSource.TimeEntries.Deleted.Returns(TimeEntryDeletedSubject.AsObservable());
            }
        }

        public sealed class WhenReceivingAnEventFromTheTimeEntryCreatedObservable : TimeEntryDataSourceObservableTest
        {
            [Fact, LogIfTooSlow]
            public async ThreadingTask AddsTheCreatedTimeEntryToTheList()
            {
                await ViewModel.Initialize();
                var newTimeEntry = NewTimeEntry.With((long)TimeSpan.FromHours(1).TotalSeconds);

                TimeEntryCreatedSubject.OnNext(newTimeEntry);

                ViewModel.TimeEntries.Any(c => c.Any(te => te.Id == 21)).Should().BeTrue();
                ViewModel.TimeEntries.Aggregate(0, (acc, te) => acc + te.Count).Should().Be(InitialAmountOfTimeEntries + 1);
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask IgnoresTheTimeEntryIfItsStillRunning()
            {
                await ViewModel.Initialize();

                TimeEntryCreatedSubject.OnNext(NewTimeEntry);

                ViewModel.TimeEntries.Any(c => c.Any(te => te.Id == 21)).Should().BeFalse();
                ViewModel.TimeEntries.Aggregate(0, (acc, te) => acc + te.Count).Should().Be(InitialAmountOfTimeEntries);
            }
        }

        public sealed class WhenReceivingAnEventFromTheTimeEntryUpdatedObservable : TimeEntryDataSourceObservableTest
        {
            [Fact, LogIfTooSlow]
            //This can happen, for example, if the time entry was just stopped
            public async ThreadingTask AddsTheTimeEntryIfItWasNotAddedPreviously()
            {
                await ViewModel.Initialize();
                var newTimeEntry = NewTimeEntry.With((long)TimeSpan.FromHours(1).TotalSeconds);

                TimeEntryUpdatedSubject.OnNext(new EntityUpdate<IThreadSafeTimeEntry>(newTimeEntry.Id, newTimeEntry));

                ViewModel.TimeEntries.Any(c => c.Any(te => te.Id == 21)).Should().BeTrue();
                ViewModel.TimeEntries.Aggregate(0, (acc, te) => acc + te.Count).Should().Be(InitialAmountOfTimeEntries + 1);
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask IgnoresTheTimeEntryIfItWasDeleted()
            {
                await ViewModel.Initialize();

                TimeEntryCreatedSubject.OnNext(NewTimeEntry);

                ViewModel.TimeEntries.Any(c => c.Any(te => te.Id == 21)).Should().BeFalse();
                ViewModel.TimeEntries.Aggregate(0, (acc, te) => acc + te.Count).Should().Be(InitialAmountOfTimeEntries);
            }
        }

        public sealed class WhenReceivingAnEventFromTheTimeEntryDeletedObservable : TimeEntryDataSourceObservableTest
        {
            [Fact, LogIfTooSlow]
            public async ThreadingTask RemovesTheTimeEntryIfItWasNotRemovedPreviously()
            {
                await ViewModel.Initialize();
                var timeEntryCollection = await InteractorFactory.GetAllNonDeletedTimeEntries().Execute().FirstAsync();
                var timeEntryToDelete = timeEntryCollection.First();

                TimeEntryDeletedSubject.OnNext(timeEntryToDelete.Id);

                ViewModel.TimeEntries.All(c => c.All(te => te.Id != timeEntryToDelete.Id)).Should().BeTrue();
                ViewModel.TimeEntries.Aggregate(0, (acc, te) => acc + te.Count).Should().Be(InitialAmountOfTimeEntries - 1);
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask RemovesTheWholeCollectionWhenThereAreNoOtherTimeEntriesLeftForThatDay()
            {
                await ViewModel.Initialize();
                var timeEntryCollection = ViewModel.TimeEntries.First();
                var timeEntriesToDelete = new List<TimeEntryViewModel>(timeEntryCollection);
                var InitialSectionCount = ViewModel.TimeEntries.Count;

                foreach (var te in timeEntriesToDelete)
                    TimeEntryDeletedSubject.OnNext(te.Id);

                ViewModel.TimeEntries.Count.Should().Be(InitialSectionCount - 1);
                ViewModel.TimeEntries.Aggregate(0, (acc, te) => acc + te.Count).Should().Be(InitialAmountOfTimeEntries - timeEntriesToDelete.Count);
            }
        }

        public sealed class TheDelayDeleteTimeEntryAction : TimeEntriesViewModelTest
        {
            private readonly TestScheduler testScheduler = new TestScheduler();
            private readonly TimeEntriesViewModel viewModel;
            private readonly TimeEntryViewModel timeEntry = new TimeEntryViewModel(new MockTimeEntry { Id = 1, Duration = 123, TagIds = Array.Empty<long>() }, DurationFormat.Classic);
            private readonly IObserver<bool> observer = Substitute.For<IObserver<bool>>();

            public TheDelayDeleteTimeEntryAction()
            {
                viewModel = new TimeEntriesViewModel(DataSource, InteractorFactory, AnalyticsService, testScheduler);
                viewModel.ShouldShowUndo.Subscribe(observer);
            }

            [Fact]
            public async ThreadingTask ShowsTheUndoUI()
            {
                await viewModel.DelayDeleteTimeEntry.Execute(timeEntry);

                observer.Received().OnNext(true);
            }

            [Fact]
            public async ThreadingTask DoesNotHideTheUndoUITooEarly()
            {
                var observable = viewModel.DelayDeleteTimeEntry.Execute(timeEntry);
                testScheduler.AdvanceBy(Constants.UndoTime.Ticks - 1);
                await observable;

                observer.Received().OnNext(true);
                observer.DidNotReceive().OnNext(false);
            }

            [Fact]
            public async ThreadingTask HidesTheUndoUIAfterSeveralSeconds()
            {
                var observable = viewModel.DelayDeleteTimeEntry.Execute(timeEntry);
                testScheduler.AdvanceBy(Constants.UndoTime.Ticks);
                await observable;

                observer.Received().OnNext(true);
                observer.Received().OnNext(false);
            }

            [Fact]
            public async ThreadingTask DoesNotHideTheUndoUIIfAnotherItemWasDeletedWhileWaiting()
            {
                var timeEntryA = new TimeEntryViewModel(new MockTimeEntry { Id = 1, Duration = 123, TagIds = Array.Empty<long>() }, DurationFormat.Classic);
                var timeEntryB = new TimeEntryViewModel(new MockTimeEntry { Id = 1, Duration = 123, TagIds = Array.Empty<long>() }, DurationFormat.Classic);

                var observableA = viewModel.DelayDeleteTimeEntry.Execute(timeEntryA);
                testScheduler.AdvanceBy((long)(Constants.UndoTime.Ticks * 0.5));
                var observableB = viewModel.DelayDeleteTimeEntry.Execute(timeEntryB);
                testScheduler.AdvanceBy((long)(Constants.UndoTime.Ticks * 0.6));
                await observableA;

                observer.Received().OnNext(true);
            }

            [Fact]
            public async ThreadingTask ImmediatelyHardDeletesTheTimeEntryWhenAnotherTimeEntryIsDeletedBeforeTheUndoTimeRunsOut()
            {
                var timeEntryA = new TimeEntryViewModel(new MockTimeEntry { Id = 1, Duration = 123, TagIds = Array.Empty<long>() }, DurationFormat.Classic);
                var timeEntryB = new TimeEntryViewModel(new MockTimeEntry { Id = 1, Duration = 123, TagIds = Array.Empty<long>() }, DurationFormat.Classic);

                var observableA = viewModel.DelayDeleteTimeEntry.Execute(timeEntryA);
                testScheduler.AdvanceBy(Constants.UndoTime.Ticks / 2);
                var observableB = viewModel.DelayDeleteTimeEntry.Execute(timeEntryB);
                await observableA;

                InteractorFactory.Received().DeleteTimeEntry(Arg.Is(timeEntryA.Id)).Execute();
            }
        }

        public sealed class TheCancelDeleteTimeEntryAction : TimeEntriesViewModelTest
        {
            private readonly TestScheduler testScheduler = new TestScheduler();
            private readonly TimeEntriesViewModel viewModel;
            private readonly TimeEntryViewModel timeEntry = new TimeEntryViewModel(new MockTimeEntry { Id = 1, Duration = 123, TagIds = Array.Empty<long>() }, DurationFormat.Classic);
            private readonly IObserver<bool> observer = Substitute.For<IObserver<bool>>();

            public TheCancelDeleteTimeEntryAction()
            {
                viewModel = new TimeEntriesViewModel(DataSource, InteractorFactory, AnalyticsService, testScheduler);
                viewModel.ShouldShowUndo.Subscribe(observer);
            }

            [Fact]
            public async ThreadingTask DoesNotDeleteTheTimeEntryIfTheUndoIsInitiatedBeforeTheUndoPeriodIsOver()
            {
                var observable = viewModel.DelayDeleteTimeEntry.Execute(timeEntry);
                testScheduler.AdvanceBy(Constants.UndoTime.Ticks / 2);
                await viewModel.CancelDeleteTimeEntry.Execute();
                testScheduler.AdvanceBy((Constants.UndoTime.Ticks / 2) + 1);
                await observable;

                InteractorFactory.DidNotReceive().DeleteTimeEntry(Arg.Is(timeEntry.Id));
            }

            [Fact]
            public async ThreadingTask DeletesTheTimeEntryIfTheUndoIsInitiatedAfterTheUndoPeriodIsOver()
            {
                var observable = viewModel.DelayDeleteTimeEntry.Execute(timeEntry);
                testScheduler.AdvanceBy(Constants.UndoTime.Ticks);
                await viewModel.CancelDeleteTimeEntry.Execute();
                await observable;

                InteractorFactory.Received().DeleteTimeEntry(Arg.Is(timeEntry.Id));
            }

            [Fact]
            public async ThreadingTask HidesTheUndoUI()
            {
                var observable = viewModel.DelayDeleteTimeEntry.Execute(timeEntry);
                testScheduler.AdvanceBy(Constants.UndoTime.Ticks / 2);
                await viewModel.CancelDeleteTimeEntry.Execute();
                await observable;

                observer.Received().OnNext(true);
                observer.Received().OnNext(false);
            }
        }
    }
}
