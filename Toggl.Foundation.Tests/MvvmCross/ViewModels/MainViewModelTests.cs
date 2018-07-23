using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using FluentAssertions;
using FsCheck.Xunit;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Suggestions;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Tests.Generators;
using Toggl.Foundation.Tests.Mocks;
using Toggl.Multivac;
using Toggl.PrimeRadiant;
using Xunit;
using static Toggl.Foundation.Helper.Constants;
using ThreadingTask = System.Threading.Tasks.Task;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class MainViewModelTests
    {
        public abstract class MainViewModelTest : BaseViewModelTests<MainViewModel>
        {
            private IDisposable disposable;

            protected ISubject<SyncProgress> ProgressSubject { get; } = new Subject<SyncProgress>();

            protected TestScheduler Scheduler { get; } = new TestScheduler();

            protected override MainViewModel CreateViewModel()
            {
                var vm = new MainViewModel(
                    DataSource,
                    TimeService,
                    RatingService,
                    UserPreferences,
                    FeedbackService,
                    AnalyticsService,
                    OnboardingStorage,
                    InteractorFactory,
                    NavigationService,
                    RemoteConfigService,
                    SuggestionProviderContainer);

                vm.Prepare();

                return vm;
            }

            protected override void AdditionalSetup()
            {
                base.AdditionalSetup();

                var syncManager = Substitute.For<ISyncManager>();
                syncManager.ProgressObservable.Returns(ProgressSubject.AsObservable());
                DataSource.SyncManager.Returns(syncManager);

                RemoteConfigService
                    .RatingViewConfiguration
                    .Returns(Observable.Never<RatingViewConfiguration>());
            }
        }

        public sealed class TheConstructor : MainViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ClassData(typeof(ElevenParameterConstructorTestData))]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useDataSource,
                bool useTimeService,
                bool useRatingService,
                bool useUserPreferences,
                bool useFeedbackService,
                bool useAnalyticsService,
                bool useOnboardingStorage,
                bool useInteractorFactory,
                bool useNavigationService,
                bool useRemoteConfigService,
                bool useSuggestionProviderContainer)
            {
                var dataSource = useDataSource ? DataSource : null;
                var timeService = useTimeService ? TimeService : null;
                var ratingService = useRatingService ? RatingService : null;
                var userPreferences = useUserPreferences ? UserPreferences : null;
                var feedbackService = useFeedbackService ? FeedbackService : null;
                var analyticsService = useAnalyticsService ? AnalyticsService : null;
                var navigationService = useNavigationService ? NavigationService : null;
                var interactorFactory = useInteractorFactory ? InteractorFactory : null;
                var onboardingStorage = useOnboardingStorage ? OnboardingStorage : null;
                var remoteConfigService = useRemoteConfigService ? RemoteConfigService : null;
                var suggestionProviderContainer = useSuggestionProviderContainer ? SuggestionProviderContainer : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new MainViewModel(
                        dataSource,
                        timeService,
                        ratingService,
                        userPreferences,
                        feedbackService,
                        analyticsService,
                        onboardingStorage,
                        interactorFactory,
                        navigationService,
                        remoteConfigService,
                        suggestionProviderContainer);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }

            [Fact, LogIfTooSlow]
            public void IsNotInManualModeByDefault()
            {
                ViewModel.IsInManualMode.Should().BeFalse();
            }
        }

        public sealed class TheViewAppearingMethod : MainViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(true)]
            [InlineData(false)]
            public void InitializesTheIsInManualModePropertyAccordingToUsersPreferences(bool isEnabled)
            {
                UserPreferences.IsManualModeEnabled.Returns(isEnabled);

                ViewModel.ViewAppearing();

                ViewModel.IsInManualMode.Should().Be(isEnabled);
            }
        }

        public abstract class BaseStartTimeEntryTest : MainViewModelTest
        {
            private readonly bool flipManualModeChecks;

            protected abstract ThreadingTask CallCommand();

            protected BaseStartTimeEntryTest(bool flipManualModeChecks)
            {
                this.flipManualModeChecks = flipManualModeChecks;
                TimeService.CurrentDateTime.Returns(DateTimeOffset.Now);
            }

            [Theory, LogIfTooSlow]
            [InlineData(true)]
            [InlineData(false)]
            public async ThreadingTask NavigatesToTheStartTimeEntryViewModel(bool isInManualMode)
            {
                ViewModel.IsInManualMode = isInManualMode;

                await CallCommand();

                await NavigationService.Received()
                   .Navigate<StartTimeEntryViewModel, StartTimeEntryParameters>(Arg.Any<StartTimeEntryParameters>());
            }

            [Theory, LogIfTooSlow]
            [InlineData(true)]
            [InlineData(false)]
            public async ThreadingTask PassesTheAppropriatePlaceholderToTheStartTimeEntryViewModel(bool isInManualMode)
            {
                ViewModel.IsInManualMode = isInManualMode;

                await CallCommand();

                var expected = isInManualMode ^ flipManualModeChecks
                    ? Resources.ManualTimeEntryPlaceholder
                    : Resources.StartTimeEntryPlaceholder;
                NavigationService.Received().Navigate<StartTimeEntryViewModel, StartTimeEntryParameters>(
                    Arg.Is<StartTimeEntryParameters>(parameter => parameter.PlaceholderText == expected)
                ).Wait();
            }

            [Theory, LogIfTooSlow]
            [InlineData(true)]
            [InlineData(false)]
            public async ThreadingTask PassesTheAppropriateDurationToTheStartTimeEntryViewModel(bool isInManualMode)
            {
                ViewModel.IsInManualMode = isInManualMode;

                await CallCommand();

                var expected = isInManualMode ^ flipManualModeChecks
                    ? TimeSpan.FromMinutes(DefaultTimeEntryDurationForManualModeInMinutes)
                    : (TimeSpan?)null;
                await NavigationService.Received().Navigate<StartTimeEntryViewModel, StartTimeEntryParameters>(
                    Arg.Is<StartTimeEntryParameters>(parameter => parameter.Duration == expected)
                );
            }

            [Theory, LogIfTooSlow]
            [InlineData(true)]
            [InlineData(false)]
            public async ThreadingTask PassesTheAppropriateStartTimeToTheStartTimeEntryViewModel(bool isInManualMode)
            {
                var date = DateTimeOffset.Now;
                TimeService.CurrentDateTime.Returns(date);
                ViewModel.IsInManualMode = isInManualMode;

                await CallCommand();

                var expected = isInManualMode ^ flipManualModeChecks
                    ? date.Subtract(TimeSpan.FromMinutes(DefaultTimeEntryDurationForManualModeInMinutes))
                    : date;
                await NavigationService.Received().Navigate<StartTimeEntryViewModel, StartTimeEntryParameters>(
                    Arg.Is<StartTimeEntryParameters>(parameter => parameter.StartTime == expected)
                );
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask CannotBeExecutedWhenThereIsARunningTimeEntry()
            {
                var timeEntry = Substitute.For<IThreadSafeTimeEntry>();
                var observable = Observable.Return(timeEntry);
                DataSource.TimeEntries.CurrentlyRunningTimeEntry.Returns(observable);
                ViewModel.Initialize().Wait();

                await CallCommand();

                await NavigationService.DidNotReceive()
                    .Navigate<StartTimeEntryViewModel, StartTimeEntryParameters>(Arg.Any<StartTimeEntryParameters>());
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask MarksTheActionButtonTappedForOnboardingPurposes()
            {
                await CallCommand();

                OnboardingStorage.Received().StartButtonWasTapped();
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask MarksTheActionNavigatedAwayBeforeStopButtonForOnboardingPurposes()
            {
                OnboardingStorage.StopButtonWasTappedBefore.Returns(Observable.Return(false));
                ViewModel.Initialize().Wait();

                await CallCommand();

                OnboardingStorage.DidNotReceive().SetNavigatedAwayFromMainViewAfterStopButton();
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask MarksTheActionNavigatedAwayAfterStopButtonForOnboardingPurposes()
            {
                var timeEntry = Substitute.For<IThreadSafeTimeEntry>();
                var observable = Observable.Return<IThreadSafeTimeEntry>(null);
                DataSource.TimeEntries.CurrentlyRunningTimeEntry.Returns(observable);
                OnboardingStorage.StopButtonWasTappedBefore.Returns(Observable.Return(true));
                ViewModel.Initialize().Wait();

                await CallCommand();

                OnboardingStorage.Received().SetNavigatedAwayFromMainViewAfterStopButton();
            }
        }

        public sealed class TheStartTimeEntryCommand : BaseStartTimeEntryTest
        {
            public TheStartTimeEntryCommand()
                : base(false) { }

            protected override ThreadingTask CallCommand()
                => ViewModel.StartTimeEntryCommand.ExecuteAsync();
        }

        public sealed class TheAlternativeStartTimeEntryCommand : BaseStartTimeEntryTest
        {
            public TheAlternativeStartTimeEntryCommand()
                : base(true) { }

            protected override ThreadingTask CallCommand()
                => ViewModel.AlternativeStartTimeEntryCommand.ExecuteAsync();
        }

        public sealed class TheOpenSettingsCommand : MainViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async ThreadingTask NavigatesToTheSettingsViewModel()
            {
                await ViewModel.OpenSettingsCommand.ExecuteAsync();

                await NavigationService.Received().Navigate<SettingsViewModel>();
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask MarksTheActionBeforeStopButtonForOnboardingPurposes()
            {
                OnboardingStorage.StopButtonWasTappedBefore.Returns(Observable.Return(false));
                ViewModel.Initialize().Wait();

                await ViewModel.OpenSettingsCommand.ExecuteAsync();

                OnboardingStorage.DidNotReceive().SetNavigatedAwayFromMainViewAfterStopButton();
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask MarksTheActionAfterStopButtonForOnboardingPurposes()
            {
                OnboardingStorage.StopButtonWasTappedBefore.Returns(Observable.Return(true));
                ViewModel.Initialize().Wait();

                await ViewModel.OpenSettingsCommand.ExecuteAsync();

                OnboardingStorage.Received().SetNavigatedAwayFromMainViewAfterStopButton();
            }
        }

        public sealed class TheOpenReportsCommand : MainViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async ThreadingTask NavigatesToTheReportsViewModel()
            {
                const long workspaceId = 10;
                var workspace = Substitute.For<IThreadSafeWorkspace>();
                workspace.Id.Returns(workspaceId);
                InteractorFactory.GetDefaultWorkspace().Execute().Returns(Observable.Return(workspace));
                OnboardingStorage.StopButtonWasTappedBefore.Returns(Observable.Return(false));

                await ViewModel.OpenReportsCommand.ExecuteAsync();

                await NavigationService.Received().Navigate<ReportsViewModel, long>(workspaceId);
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask MarksTheActionBeforeStopButtonForOnboardingPurposes()
            {
                const long workspaceId = 10;
                var workspace = Substitute.For<IThreadSafeWorkspace>();
                workspace.Id.Returns(workspaceId);
                InteractorFactory.GetDefaultWorkspace().Execute().Returns(Observable.Return(workspace));
                OnboardingStorage.StopButtonWasTappedBefore.Returns(Observable.Return(false));
                ViewModel.Initialize().Wait();

                await ViewModel.OpenReportsCommand.ExecuteAsync();

                OnboardingStorage.DidNotReceive().SetNavigatedAwayFromMainViewAfterStopButton();
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask MarksTheActionAfterStopButtonForOnboardingPurposes()
            {
                const long workspaceId = 10;
                var workspace = Substitute.For<IThreadSafeWorkspace>();
                workspace.Id.Returns(workspaceId);
                InteractorFactory.GetDefaultWorkspace().Execute().Returns(Observable.Return(workspace));
                OnboardingStorage.StopButtonWasTappedBefore.Returns(Observable.Return(true));
                ViewModel.Initialize().Wait();

                await ViewModel.OpenReportsCommand.ExecuteAsync();

                OnboardingStorage.Received().SetNavigatedAwayFromMainViewAfterStopButton();
            }
        }

        public sealed class TheOpenSyncFailuresCommand : MainViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async ThreadingTask NavigatesToTheSettingsViewModel()
            {
                await ViewModel.OpenSyncFailuresCommand.ExecuteAsync();

                await NavigationService.Received().Navigate<SyncFailuresViewModel>();
            }
        }

        public class TheStopTimeEntryCommand : MainViewModelTest
        {
            private ISubject<IThreadSafeTimeEntry> subject;

            public TheStopTimeEntryCommand()
            {
                var timeEntry = Substitute.For<IThreadSafeTimeEntry>();
                subject = new BehaviorSubject<IThreadSafeTimeEntry>(timeEntry);
                var observable = subject.AsObservable();
                DataSource.TimeEntries.CurrentlyRunningTimeEntry.Returns(observable);

                ViewModel.Initialize().Wait();
                Scheduler.AdvanceBy(TimeSpan.FromMilliseconds(50).Ticks);
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask CallsTheStopMethodOnTheDataSource()
            {
                var date = DateTimeOffset.UtcNow;
                TimeService.CurrentDateTime.Returns(date);

                await ViewModel.StopTimeEntryCommand.ExecuteAsync();

                await DataSource.TimeEntries.Received().Stop(date);
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask SetsTheElapsedTimeToZero()
            {
                await ViewModel.StopTimeEntryCommand.ExecuteAsync();

                ViewModel.CurrentTimeEntryElapsedTime.Should().Be(TimeSpan.Zero);
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask InitiatesPushSync()
            {
                await ViewModel.StopTimeEntryCommand.ExecuteAsync();

                await DataSource.SyncManager.Received().PushSync();
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask MarksTheActionForOnboardingPurposes()
            {
                await ViewModel.StopTimeEntryCommand.ExecuteAsync();

                OnboardingStorage.Received().StopButtonWasTapped();
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask DoesNotInitiatePushSyncWhenSavingFails()
            {
                DataSource.TimeEntries.Stop(Arg.Any<DateTimeOffset>())
                    .Returns(Observable.Throw<IThreadSafeTimeEntry>(new Exception()));

                Action stopTimeEntry = () => ViewModel.StopTimeEntryCommand.ExecuteAsync().Wait();

                stopTimeEntry.Should().Throw<Exception>();
                await DataSource.SyncManager.DidNotReceive().PushSync();
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask CannotBeExecutedTwiceInARowInFastSuccession()
            {
                var taskA = ViewModel.StopTimeEntryCommand.ExecuteAsync();
                var taskB = ViewModel.StopTimeEntryCommand.ExecuteAsync();

                ThreadingTask.WaitAll(taskA, taskB);

                await DataSource.TimeEntries.Received(1).Stop(Arg.Any<DateTimeOffset>());
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask CannotBeExecutedTwiceInARow()
            {
                await ViewModel.StopTimeEntryCommand.ExecuteAsync();
                subject.OnNext(null);
                await ViewModel.StopTimeEntryCommand.ExecuteAsync();

                await DataSource.TimeEntries.Received(1).Stop(Arg.Any<DateTimeOffset>());
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask CannotBeExecutedWhenNoTimeEntryIsRunning()
            {
                subject.OnNext(null);
                Scheduler.AdvanceBy(TimeSpan.FromMilliseconds(50).Ticks);

                await ViewModel.StopTimeEntryCommand.ExecuteAsync();

                await DataSource.TimeEntries.DidNotReceive().Stop(Arg.Any<DateTimeOffset>());
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask CanBeExecutedForTheSecondTimeIfAnotherTimeEntryIsStartedInTheMeantime()
            {
                var secondTimeEntry = Substitute.For<IThreadSafeTimeEntry>();

                await ViewModel.StopTimeEntryCommand.ExecuteAsync();
                subject.OnNext(secondTimeEntry);
                Scheduler.AdvanceBy(TimeSpan.FromMilliseconds(50).Ticks);
                await ViewModel.StopTimeEntryCommand.ExecuteAsync();

                await DataSource.TimeEntries.Received(2).Stop(Arg.Any<DateTimeOffset>());
            }
        }

        public sealed class TheEditTimeEntryCommand : MainViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async ThreadingTask NavigatesToTheEditTimeEntryViewModel()
            {
                var timeEntry = Substitute.For<IThreadSafeTimeEntry>();
                var observable = Observable.Return(timeEntry);
                DataSource.TimeEntries.CurrentlyRunningTimeEntry.Returns(observable);
                ViewModel.Initialize().Wait();

                await ViewModel.EditTimeEntryCommand.ExecuteAsync();

                await NavigationService.Received()
                    .Navigate<EditTimeEntryViewModel, long>(Arg.Any<long>());
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask NavigatesToTheEditViewOnlyOnceOnMultipleTaps()
            {
                var timeEntry = Substitute.For<IThreadSafeTimeEntry>();
                var observable = Observable.Return(timeEntry);
                DataSource.TimeEntries.CurrentlyRunningTimeEntry.Returns(observable);
                ViewModel.Initialize().Wait();
                NavigationService
                    .Navigate<EditTimeEntryViewModel, long>(Arg.Any<long>())
                    .Returns(ThreadingTask.Delay(10));

                for (int i = 0; i < 10; i++)
                {
                    ThreadingTask runSynchronously = ViewModel.EditTimeEntryCommand.ExecuteAsync();
                }

                await NavigationService.Received(1)
                    .Navigate<EditTimeEntryViewModel, long>(Arg.Any<long>());
            }

            [Property]
            public void PassesTheCurrentDateToTheStartTimeEntryViewModel(long id)
            {
                var timeEntry = Substitute.For<IThreadSafeTimeEntry>();
                timeEntry.Id.Returns(id);
                var observable = Observable.Return(timeEntry);
                DataSource.TimeEntries.CurrentlyRunningTimeEntry.Returns(observable);
                ViewModel.Initialize().Wait();

                ViewModel.EditTimeEntryCommand.ExecuteAsync().Wait();

                NavigationService.Received()
                    .Navigate<EditTimeEntryViewModel, long>(Arg.Is(id)).Wait();
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask CannotBeExecutedWhenThereIsNoRunningTimeEntry()
            {
                var observable = Observable.Return<IThreadSafeTimeEntry>(null);
                DataSource.TimeEntries.CurrentlyRunningTimeEntry.Returns(observable);
                ViewModel.Initialize().Wait();

                ViewModel.EditTimeEntryCommand.ExecuteAsync().Wait();

                await NavigationService.DidNotReceive()
                    .Navigate<EditTimeEntryViewModel, long>(Arg.Any<long>());
            }

            [Fact, LogIfTooSlow]
            public void MarksTheActionBeforeStopButtonForOnboardingPurposes()
            {
                const long timeEntryId = 100;
                var timeEntry = Substitute.For<IThreadSafeTimeEntry>();
                timeEntry.Id.Returns(timeEntryId);
                var observable = Observable.Return(timeEntry);
                DataSource.TimeEntries.CurrentlyRunningTimeEntry.Returns(observable);
                OnboardingStorage.StopButtonWasTappedBefore.Returns(Observable.Return(false));
                ViewModel.Initialize().Wait();

                ViewModel.EditTimeEntryCommand.ExecuteAsync().Wait();

                OnboardingStorage.DidNotReceive().SetNavigatedAwayFromMainViewAfterStopButton();
            }

            [Fact, LogIfTooSlow]
            public void MarksTheActionAfterStopButtonForOnboardingPurposes()
            {
                const long timeEntryId = 100;
                var timeEntry = Substitute.For<IThreadSafeTimeEntry>();
                timeEntry.Id.Returns(timeEntryId);
                var observable = Observable.Return(timeEntry);
                DataSource.TimeEntries.CurrentlyRunningTimeEntry.Returns(observable);
                OnboardingStorage.StopButtonWasTappedBefore.Returns(Observable.Return(true));
                ViewModel.Initialize().Wait();

                ViewModel.EditTimeEntryCommand.ExecuteAsync().Wait();

                OnboardingStorage.Received().SetNavigatedAwayFromMainViewAfterStopButton();
            }
        }

        public abstract class CurrentTimeEntrypropertyTest<T> : MainViewModelTest
        {
            private readonly BehaviorSubject<IThreadSafeTimeEntry> currentTimeEntrySubject
                = new BehaviorSubject<IThreadSafeTimeEntry>(null);

            protected abstract T ActualValue { get; }
            protected abstract T ExpectedValue { get; }
            protected abstract T ExpectedEmptyValue { get; }

            protected long TimeEntryId = 13;
            protected string Description = "Something";
            protected string Project = "Some project";
            protected string Task = "Some task";
            protected string Client = "Some client";
            protected string ProjectColor = "0000AF";
            protected bool Active = true;
            protected DateTimeOffset StartTime = new DateTimeOffset(2018, 01, 02, 03, 04, 05, TimeSpan.Zero);
            protected DateTimeOffset Now = new DateTimeOffset(2018, 01, 02, 06, 04, 05, TimeSpan.Zero);

            private async ThreadingTask prepare()
            {
                var timeEntry = Substitute.For<IThreadSafeTimeEntry>();
                timeEntry.Id.Returns(TimeEntryId);
                timeEntry.Description.Returns(Description);
                timeEntry.Project.Name.Returns(Project);
                timeEntry.Project.Color.Returns(ProjectColor);
                timeEntry.Project.Active.Returns(Active);
                timeEntry.Task.Name.Returns(Task);
                timeEntry.Project.Client.Name.Returns(Client);
                timeEntry.Start.Returns(StartTime);

                TimeService.CurrentDateTime.Returns(Now);

                DataSource.TimeEntries.CurrentlyRunningTimeEntry.Returns(currentTimeEntrySubject.AsObservable());

                await ViewModel.Initialize();
                currentTimeEntrySubject.OnNext(timeEntry);
                Scheduler.AdvanceBy(TimeSpan.FromMilliseconds(50).Ticks);
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask IsSet()
            {
                await prepare();

                ActualValue.Should().Be(ExpectedValue);
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask IsUnset()
            {
                await prepare();
                currentTimeEntrySubject.OnNext(null);
                Scheduler.AdvanceBy(TimeSpan.FromMilliseconds(50).Ticks);

                ActualValue.Should().Be(ExpectedEmptyValue);
            }
        }

        public sealed class TheCurrentTimeEntryIdProperty : CurrentTimeEntrypropertyTest<long?>
        {
            protected override long? ActualValue => ViewModel.CurrentTimeEntryId;

            protected override long? ExpectedValue => TimeEntryId;

            protected override long? ExpectedEmptyValue => null;
        }

        public sealed class TheCurrentTimeEntryDescriptionProperty : CurrentTimeEntrypropertyTest<string>
        {
            protected override string ActualValue => ViewModel.CurrentTimeEntryDescription;

            protected override string ExpectedValue => Description;

            protected override string ExpectedEmptyValue => "";
        }

        public sealed class TheCurrentTimeEntryProjectProperty : CurrentTimeEntrypropertyTest<string>
        {
            protected override string ActualValue => ViewModel.CurrentTimeEntryProject;

            protected override string ExpectedValue => Project;

            protected override string ExpectedEmptyValue => "";
        }

        public sealed class TheCurrentTimeEntryProjectColorProperty : CurrentTimeEntrypropertyTest<string>
        {
            protected override string ActualValue => ViewModel.CurrentTimeEntryProjectColor;

            protected override string ExpectedValue => ProjectColor;

            protected override string ExpectedEmptyValue => "";
        }

        public sealed class TheCurrentTimeEntryTaskProperty : CurrentTimeEntrypropertyTest<string>
        {
            protected override string ActualValue => ViewModel.CurrentTimeEntryTask;

            protected override string ExpectedValue => Task;

            protected override string ExpectedEmptyValue => "";
        }

        public sealed class TheCurrentTimeEntryClientProperty : CurrentTimeEntrypropertyTest<string>
        {
            protected override string ActualValue => ViewModel.CurrentTimeEntryClient;

            protected override string ExpectedValue => Client;

            protected override string ExpectedEmptyValue => "";
        }

        public sealed class TheNumberOfSyncFailuresProperty : MainViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async ThreadingTask ReturnsTheCountOfInteractorResult()
            {
                var syncables = new IDatabaseSyncable[]
                {
                    new MockTag { Name = "Tag", SyncStatus = SyncStatus.SyncFailed, LastSyncErrorMessage = "Error1" },
                    new MockTag { Name = "Tag2", SyncStatus = SyncStatus.SyncFailed, LastSyncErrorMessage = "Error1" },
                    new MockProject { Name = "Project", SyncStatus = SyncStatus.SyncFailed, LastSyncErrorMessage = "Error2" }
                };

                var items = syncables.Select(i => new SyncFailureItem(i));

                var interactor = Substitute.For<IInteractor<IObservable<IEnumerable<SyncFailureItem>>>>();
                interactor.Execute().Returns(Observable.Return(items));
                InteractorFactory.GetItemsThatFailedToSync().Returns(interactor);

                await ViewModel.Initialize();

                ViewModel.NumberOfSyncFailures.Should().Be(3);
            }
        }

        public sealed class TheCurrentTimeEntryElapsedTimeProperty : CurrentTimeEntrypropertyTest<TimeSpan>
        {
            protected override TimeSpan ActualValue => ViewModel.CurrentTimeEntryElapsedTime;

            protected override TimeSpan ExpectedValue => Now - StartTime;

            protected override TimeSpan ExpectedEmptyValue => TimeSpan.Zero;
        }

        public sealed class TheIsWelcomeProperty : MainViewModelTest
        {
            private IThreadSafeTimeEntry createTimeEntry(int id)
            {
                var timeEntry = Substitute.For<IThreadSafeTimeEntry>();
                timeEntry.Id.Returns(id);
                timeEntry.Start.Returns(DateTimeOffset.Now);
                timeEntry.Duration.Returns(100);
                return timeEntry;
            }
        }

        public abstract class InitialStateTest : MainViewModelTest
        {
            protected void PrepareSuggestion()
            {
                DataSource.TimeEntries.IsEmpty.Returns(Observable.Return(false));
                var suggestionProvider = Substitute.For<ISuggestionProvider>();
                var timeEntry = Substitute.For<IThreadSafeTimeEntry>();
                timeEntry.Id.Returns(123);
                timeEntry.Start.Returns(DateTimeOffset.Now);
                timeEntry.Duration.Returns((long?)null);
                timeEntry.Description.Returns("something");
                var suggestion = new Suggestion(timeEntry);
                suggestionProvider.GetSuggestions().Returns(Observable.Return(suggestion));
                var providers = new ReadOnlyCollection<ISuggestionProvider>(
                    new List<ISuggestionProvider> { suggestionProvider }
                );
                SuggestionProviderContainer.Providers.Returns(providers);
            }

            protected void PrepareTimeEntry()
            {
                var timeEntry = Substitute.For<IThreadSafeTimeEntry>();
                timeEntry.Id.Returns(123);
                timeEntry.Start.Returns(DateTimeOffset.Now);
                timeEntry.Duration.Returns(100);
                InteractorFactory.GetAllNonDeletedTimeEntries().Execute()
                    .Returns(Observable.Return(new[] { timeEntry }));
                DataSource
                    .TimeEntries
                    .Updated
                    .Returns(Observable.Never<EntityUpdate<IThreadSafeTimeEntry>>());
            }

            protected void PrepareIsWelcome(bool isWelcome)
            {
                var observable = Observable.Return(isWelcome);
                OnboardingStorage.IsNewUser.Returns(observable);
            }
        }

        public sealed class TheShouldShowEmptyStateProperty : InitialStateTest
        {
            [Fact, LogIfTooSlow]
            public async ThreadingTask ReturnsTrueWhenThereAreNoSuggestionsAndNoTimeEntriesAndIsWelcome()
            {
                PrepareIsWelcome(true);
                await ViewModel.Initialize();

                ViewModel.ShouldShowEmptyState.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask ReturnsFalseWhenThereAreSomeSuggestions()
            {
                PrepareSuggestion();

                await ViewModel.Initialize();

                ViewModel.ShouldShowEmptyState.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask ReturnsFalseWhenThereAreSomeTimeEntries()
            {
                PrepareTimeEntry();

                await ViewModel.Initialize();

                ViewModel.ShouldShowEmptyState.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask ReturnsFalseWhenIsNotWelcome()
            {
                PrepareIsWelcome(false);
                await ViewModel.Initialize();

                ViewModel.ShouldShowEmptyState.Should().BeFalse();
            }
        }

        public sealed class TheShouldShowWelcomeBackProperty : InitialStateTest
        {
            [Fact, LogIfTooSlow]
            public async ThreadingTask ReturnsTrueWhenThereAreNoSuggestionsAndNoTimeEntriesAndIsNotWelcome()
            {
                PrepareIsWelcome(false);
                await ViewModel.Initialize();

                ViewModel.ShouldShowWelcomeBack.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask ReturnsFalseWhenThereAreSomeSuggestions()
            {
                PrepareSuggestion();
                await ViewModel.Initialize();

                ViewModel.ShouldShowWelcomeBack.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask ReturnsFalseWhenThereAreSomeTimeEntries()
            {
                PrepareTimeEntry();
                await ViewModel.Initialize();

                ViewModel.ShouldShowWelcomeBack.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask ReturnsFalseWhenIsWelcome()
            {
                PrepareIsWelcome(true);
                await ViewModel.Initialize();

                ViewModel.ShouldShowWelcomeBack.Should().BeFalse();
            }
        }

        public sealed class TheInitializeMethod
        {
            public sealed class WhenNavigationActionIsStop : MainViewModelTest
            {
                [Fact, LogIfTooSlow]
                public async ThreadingTask StopsTheCurrentEntry()
                {
                    ViewModel.Init(ApplicationUrls.Main.Action.Stop);
                    await ViewModel.Initialize();

                    await DataSource.TimeEntries.Received().Stop(TimeService.CurrentDateTime);
                }

                [Fact, LogIfTooSlow]
                public async ThreadingTask StartsPushSync()
                {
                    ViewModel.Init(ApplicationUrls.Main.Action.Stop);
                    await ViewModel.Initialize();

                    await DataSource.SyncManager.Received().PushSync();
                }
            }

            public sealed class WhenNavigationActionIsContinue : MainViewModelTest
            {
                [Fact, LogIfTooSlow]
                public async ThreadingTask GetsTheContinueMostRecentTimeEntryInteractor()
                {
                    ViewModel.Init(ApplicationUrls.Main.Action.Continue);

                    await ViewModel.Initialize();

                    InteractorFactory.Received().ContinueMostRecentTimeEntry();
                }

                [Fact, LogIfTooSlow]
                public async ThreadingTask ExecutesTheContinueMostRecentTimeEntryInteractor()
                {
                    var interactor = Substitute.For<IInteractor<IObservable<IThreadSafeTimeEntry>>>();
                    InteractorFactory.ContinueMostRecentTimeEntry().Returns(interactor);
                    ViewModel.Init(ApplicationUrls.Main.Action.Continue);

                    await ViewModel.Initialize();

                    await interactor.Received().Execute();
                }
            }
        }
    }
}
