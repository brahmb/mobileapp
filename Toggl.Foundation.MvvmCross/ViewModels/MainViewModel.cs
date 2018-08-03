using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using PropertyChanged;
using Toggl.Foundation;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Experiments;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels.Hints;
using Toggl.Foundation.Services;
using Toggl.Foundation.Suggestions;
using Toggl.Foundation.Sync;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Settings;

[assembly: MvxNavigation(typeof(MainViewModel), ApplicationUrls.Main.Regex)]
namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class MainViewModel : MvxViewModel
    {
        // Outputs
        public ObservableGroupedOrderedCollection<TimeEntryViewModel> TimeEntries => timeEntriesViewModel.TimeEntries;
        public IObservable<bool> LogEmpty => timeEntriesViewModel.Empty;
        public IObservable<int> TimeEntriesCount => timeEntriesViewModel.Count;
        public IObservable<SyncProgress> SyncProgressState { get; private set; }
        public IObservable<bool> ShouldShowEmptyState { get; private set; }
        public IObservable<bool> ShouldShowWelcomeBack { get; private set; }

        public TimeSpan CurrentTimeEntryElapsedTime { get; private set; } = TimeSpan.Zero;
        public DurationFormat CurrentTimeEntryElapsedTimeFormat { get; } = DurationFormat.Improved;
        public long? CurrentTimeEntryId { get; private set; }
        public string CurrentTimeEntryDescription { get; private set; }
        public string CurrentTimeEntryProject { get; private set; }
        public string CurrentTimeEntryProjectColor { get; private set; }
        public string CurrentTimeEntryTask { get; private set; }
        public string CurrentTimeEntryClient { get; private set; }
        public IObservable<bool> CurrentTimeEntryHasDescription { get; private set; }
        public bool IsAddDescriptionLabelVisible =>
            string.IsNullOrEmpty(CurrentTimeEntryDescription)
            && string.IsNullOrEmpty(CurrentTimeEntryProject);
        public IObservable<bool> IsTimeEntryRunning { get; private set; }

        public int NumberOfSyncFailures { get; private set; }
        public bool IsInManualMode { get; set; } = false;

        public SuggestionsViewModel SuggestionsViewModel { get; }
        public RatingViewModel RatingViewModel { get; }
        public IOnboardingStorage OnboardingStorage => onboardingStorage;
        public IMvxNavigationService NavigationService => navigationService;
        public IMvxAsyncCommand StartTimeEntryCommand { get; }
        public IMvxAsyncCommand AlternativeStartTimeEntryCommand { get; }
        public IMvxAsyncCommand StopTimeEntryCommand { get; }
        public IMvxAsyncCommand OpenSettingsCommand { get; }
        public IMvxAsyncCommand OpenReportsCommand { get; }
        public IMvxAsyncCommand OpenSyncFailuresCommand { get; }
        public IMvxCommand ToggleManualMode { get; }

        // Inputs
        public InputAction<TimeEntryViewModel> ContinueTimeEntry { get; }
        public InputAction<TimeEntryViewModel> DeleteTimeEntry { get; }
        public InputAction<TimeEntryViewModel> SelectTimeEntry { get; }
        public UIAction RefreshAction { get; }

        // Private
        private const int ratingViewTimeout = 5;

        private readonly ITimeService timeService;
        private readonly ITogglDataSource dataSource;
        private readonly IUserPreferences userPreferences;
        private readonly IAnalyticsService analyticsService;
        private readonly IOnboardingStorage onboardingStorage;
        private readonly IInteractorFactory interactorFactory;
        private readonly IMvxNavigationService navigationService;

        private CompositeDisposable disposeBag = new CompositeDisposable();

        private TimeEntriesViewModel timeEntriesViewModel;
        private RatingViewExperiment ratingViewExperiment;

        private bool isStopButtonEnabled = false;
        private string urlNavigationAction;
        private bool hasStopButtonEverBeenUsed;
        private bool isEditViewOpen = false;
        private object isEditViewOpenLock = new object();

        private DateTimeOffset? currentTimeEntryStart;

        // Deprecated properties

        [Obsolete("Use TimeEntriesViewModel and MainViewModel methods instead")]
        public TimeEntriesLogViewModel TimeEntriesLogViewModel { get; }

        [Obsolete("Use SelectTimeEntry RxAction instead")]
        public IMvxAsyncCommand EditTimeEntryCommand { get; }

        [Obsolete("Use RefreshAction RxAction instead")]
        public IMvxCommand RefreshCommand { get; }

        [Obsolete("Use SyncProgressState instead")]
        public SyncProgress SyncingProgress { get; private set; }

        [Obsolete("Use SyncProgressState instead")]
        [DependsOn(nameof(SyncingProgress))]
        public bool ShowSyncIndicator => SyncingProgress == SyncProgress.Syncing;

        public MainViewModel(
            ITogglDataSource dataSource,
            ITimeService timeService,
            IRatingService ratingService,
            IUserPreferences userPreferences,
            IAnalyticsService analyticsService,
            IOnboardingStorage onboardingStorage,
            IInteractorFactory interactorFactory,
            IMvxNavigationService navigationService,
            IRemoteConfigService remoteConfigService,
            ISuggestionProviderContainer suggestionProviders)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(ratingService, nameof(ratingService));
            Ensure.Argument.IsNotNull(userPreferences, nameof(userPreferences));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(onboardingStorage, nameof(onboardingStorage));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(remoteConfigService, nameof(remoteConfigService));
            Ensure.Argument.IsNotNull(suggestionProviders, nameof(suggestionProviders));

            this.dataSource = dataSource;
            this.timeService = timeService;
            this.userPreferences = userPreferences;
            this.analyticsService = analyticsService;
            this.interactorFactory = interactorFactory;
            this.navigationService = navigationService;
            this.onboardingStorage = onboardingStorage;

            SuggestionsViewModel = new SuggestionsViewModel(dataSource, interactorFactory, onboardingStorage, suggestionProviders);
            RatingViewModel = new RatingViewModel(timeService, dataSource, ratingService, analyticsService, onboardingStorage, navigationService);
            TimeEntriesLogViewModel = new TimeEntriesLogViewModel(timeService, dataSource, interactorFactory, onboardingStorage, analyticsService, navigationService);
            timeEntriesViewModel = new TimeEntriesViewModel(dataSource, interactorFactory);

            ratingViewExperiment = new RatingViewExperiment(timeService, dataSource, onboardingStorage, remoteConfigService);

            RefreshCommand = new MvxCommand(Refresh);
            OpenReportsCommand = new MvxAsyncCommand(openReports);
            OpenSettingsCommand = new MvxAsyncCommand(openSettings);
            OpenSyncFailuresCommand = new MvxAsyncCommand(openSyncFailures);
            EditTimeEntryCommand = new MvxAsyncCommand(editTimeEntry, canExecuteEditTimeEntryCommand);
            StopTimeEntryCommand = new MvxAsyncCommand(stopTimeEntry, () => isStopButtonEnabled);
            StartTimeEntryCommand = new MvxAsyncCommand(startTimeEntry, () => CurrentTimeEntryId.HasValue == false);
            AlternativeStartTimeEntryCommand = new MvxAsyncCommand(alternativeStartTimeEntry, () => CurrentTimeEntryId.HasValue == false);

            ContinueTimeEntry = new InputAction<TimeEntryViewModel>(continueTimeEntry);
            DeleteTimeEntry = new InputAction<TimeEntryViewModel>(deleteTimeEntry);
            SelectTimeEntry = new InputAction<TimeEntryViewModel>(timeEntrySelected);
            RefreshAction = new UIAction(refresh);
        }

        public void Init(string action)
        {
            urlNavigationAction = action;
        }

        public override async Task Initialize()
        {
            await base.Initialize();

            await timeEntriesViewModel.Initialize();
            await TimeEntriesLogViewModel.Initialize();
            await SuggestionsViewModel.Initialize();
            await RatingViewModel.Initialize();

            SyncProgressState = dataSource.SyncManager.ProgressObservable;

            var isWelcome = onboardingStorage.IsNewUser;

            var NoTimeEntries = timeEntriesViewModel.Empty
                .Select( e => e && SuggestionsViewModel.IsEmpty )
                .DistinctUntilChanged();

            ShouldShowEmptyState = ObservableAddons.CombineLatestAll(
                    isWelcome,
                    NoTimeEntries
                )
                .DistinctUntilChanged();

            ShouldShowWelcomeBack = ObservableAddons.CombineLatestAll(
                    isWelcome.Select(b => !b),
                    NoTimeEntries
                )
                .StartWith(false)
                .DistinctUntilChanged();

            var connectableTimeEntryIsRunning =
                dataSource
                    .TimeEntries
                    .CurrentlyRunningTimeEntry
                    .Do(setRunningEntry)
                    .Select(timeEntry => timeEntry != null)
                    .DistinctUntilChanged()
                    .Replay(1);

            connectableTimeEntryIsRunning.Connect();

            IsTimeEntryRunning = connectableTimeEntryIsRunning;

            CurrentTimeEntryHasDescription = dataSource
                .TimeEntries
                .CurrentlyRunningTimeEntry
                .Select(te => !string.IsNullOrWhiteSpace(te?.Description))
                .DistinctUntilChanged();

            timeService
                .CurrentDateTimeObservable
                .Where(_ => currentTimeEntryStart != null)
                .Subscribe(currentTime => CurrentTimeEntryElapsedTime = currentTime - currentTimeEntryStart.Value)
                .DisposedBy(disposeBag);

            dataSource
                .SyncManager
                .ProgressObservable
                .Subscribe(progress => SyncingProgress = progress)
                .DisposedBy(disposeBag);

            interactorFactory
                .GetItemsThatFailedToSync()
                .Execute()
                .Select(i => i.Count())
                .Subscribe(n => NumberOfSyncFailures = n)
                .DisposedBy(disposeBag);

            timeService.MidnightObservable
                .Subscribe(onMidnight)
                .DisposedBy(disposeBag);

            switch (urlNavigationAction)
            {
                case ApplicationUrls.Main.Action.Continue:
                    await continueMostRecentEntry();
                    break;

                case ApplicationUrls.Main.Action.Stop:
                    await stopTimeEntry();
                    break;
            }

            ratingViewExperiment
                .RatingViewShouldBeVisible
                .Subscribe(presentRatingViewIfNeeded)
                .DisposedBy(disposeBag);

            onboardingStorage.StopButtonWasTappedBefore
                             .Subscribe(hasBeen => hasStopButtonEverBeenUsed = hasBeen)
                             .DisposedBy(disposeBag);
        }

        private void presentRatingViewIfNeeded(bool shouldBevisible)
        {
            if (!shouldBevisible) return;

            var wasShownMoreThanOnce = onboardingStorage.NumberOfTimesRatingViewWasShown() > 1;
            if (wasShownMoreThanOnce) return;

            var lastOutcome = onboardingStorage.RatingViewOutcome();
            if (lastOutcome != null)
            {
                var thereIsInteractionFormLastTime = lastOutcome != RatingViewOutcome.NoInteraction;
                if (thereIsInteractionFormLastTime) return;
            }

            var lastOutcomeTime = onboardingStorage.RatingViewOutcomeTime();
            if (lastOutcomeTime != null)
            {
                var oneDayHasNotPassedSinceLastTime = lastOutcomeTime + TimeSpan.FromHours(24) > timeService.CurrentDateTime;
                if (oneDayHasNotPassedSinceLastTime && !wasShownMoreThanOnce) return;
            }

            navigationService.ChangePresentation(new ToggleRatingViewVisibilityHint());
            analyticsService.RatingViewWasShown.Track();
            onboardingStorage.SetDidShowRatingView();
            onboardingStorage.SetRatingViewOutcome(RatingViewOutcome.NoInteraction, timeService.CurrentDateTime);
            timeService.RunAfterDelay(TimeSpan.FromMinutes(ratingViewTimeout), () =>
            {
                navigationService.ChangePresentation(new ToggleRatingViewVisibilityHint());
            });
        }

        private async Task continueMostRecentEntry()
        {
            await interactorFactory.ContinueMostRecentTimeEntry().Execute();
        }

        public override void ViewAppearing()
        {
            base.ViewAppearing();

            IsInManualMode = userPreferences.IsManualModeEnabled;
        }

        private void setRunningEntry(IThreadSafeTimeEntry timeEntry)
        {
            CurrentTimeEntryId = timeEntry?.Id;
            currentTimeEntryStart = timeEntry?.Start;
            CurrentTimeEntryDescription = timeEntry?.Description ?? "";
            CurrentTimeEntryElapsedTime = timeService.CurrentDateTime - currentTimeEntryStart ?? TimeSpan.Zero;

            CurrentTimeEntryTask = timeEntry?.Task?.Name ?? "";
            CurrentTimeEntryProject = timeEntry?.Project?.DisplayName() ?? "";
            CurrentTimeEntryProjectColor = timeEntry?.Project?.DisplayColor() ?? "";
            CurrentTimeEntryClient = timeEntry?.Project?.Client?.Name ?? "";

            isStopButtonEnabled = timeEntry != null;

            StopTimeEntryCommand.RaiseCanExecuteChanged();
            StartTimeEntryCommand.RaiseCanExecuteChanged();
            EditTimeEntryCommand.RaiseCanExecuteChanged();

            RaisePropertyChanged(nameof(IsTimeEntryRunning));
        }

        public void Refresh()
        {
            dataSource.SyncManager.ForceFullSync();
        }

        private void onMidnight(DateTimeOffset midnight)
        {
            navigationService.ChangePresentation(new ReloadLogHint());
        }

        private Task openSettings()
            => navigate<SettingsViewModel>();

        private async Task openReports()
        {
            var workspace = await interactorFactory.GetDefaultWorkspace().Execute();
            await navigate<ReportsViewModel, long>(workspace.Id);
        }

        private Task openSyncFailures()
            => navigate<SyncFailuresViewModel>();

        private Task startTimeEntry()
            => startTimeEntry(IsInManualMode);

        private Task alternativeStartTimeEntry()
            => startTimeEntry(!IsInManualMode);

        private Task startTimeEntry(bool initializeInManualMode)
        {
            OnboardingStorage.StartButtonWasTapped();

            if (hasStopButtonEverBeenUsed)
                onboardingStorage.SetNavigatedAwayFromMainViewAfterStopButton();

            var parameter = initializeInManualMode
                ? StartTimeEntryParameters.ForManualMode(timeService.CurrentDateTime)
                : StartTimeEntryParameters.ForTimerMode(timeService.CurrentDateTime);

            return navigate<StartTimeEntryViewModel, StartTimeEntryParameters>(parameter);
        }

        private IObservable<Unit> continueTimeEntry(TimeEntryViewModel timeEntry)
        {
            return interactorFactory
                .ContinueTimeEntry(timeEntry)
                .Execute()
                .Do( _ => {
                    onboardingStorage.SetTimeEntryContinued();
                })
                .SelectUnit();
        }

        private IObservable<Unit> timeEntrySelected(TimeEntryViewModel timeEntry)
        {
            if (isEditViewOpen)
                return Observable.Empty<Unit>();

            onboardingStorage.TimeEntryWasTapped();
            return navigate<EditTimeEntryViewModel, long>(timeEntry.Id).ToObservable();
        }

        private IObservable<Unit> deleteTimeEntry(TimeEntryViewModel timeEntry)
        {
            return interactorFactory
                .DeleteTimeEntry(timeEntry.Id)
                .Execute()
                .Do( _ => {
                    analyticsService.DeleteTimeEntry.Track();
                    dataSource.SyncManager.PushSync();
                });
        }

        private IObservable<Unit> refresh()
        {
            return dataSource.SyncManager.ForceFullSync()
                .SelectUnit();
        }

        private async Task stopTimeEntry()
        {
            OnboardingStorage.StopButtonWasTapped();

            isStopButtonEnabled = false;
            StopTimeEntryCommand.RaiseCanExecuteChanged();

            await dataSource.TimeEntries.Stop(timeService.CurrentDateTime)
                .Do(_ => dataSource.SyncManager.PushSync());

            CurrentTimeEntryElapsedTime = TimeSpan.Zero;
        }

        private async Task editTimeEntry()
        {
            lock (isEditViewOpenLock)
            {
                isEditViewOpen = true;
            }

            await navigate<EditTimeEntryViewModel, long>(CurrentTimeEntryId.Value);

            lock (isEditViewOpenLock)
            {
                isEditViewOpen = false;
            }
        }

        private bool canExecuteEditTimeEntryCommand()
        {
            lock (isEditViewOpenLock)
            {
                if (isEditViewOpen)
                    return false;
            }

            return CurrentTimeEntryId.HasValue;
        }

        private Task navigate<TModel, TParameters>(TParameters value)
            where TModel : IMvxViewModel<TParameters>
        {
            if (hasStopButtonEverBeenUsed)
                onboardingStorage.SetNavigatedAwayFromMainViewAfterStopButton();

            return navigationService.Navigate<TModel, TParameters>(value);
        }

        private Task navigate<TModel>()
            where TModel : IMvxViewModel
        {
            if (hasStopButtonEverBeenUsed)
                onboardingStorage.SetNavigatedAwayFromMainViewAfterStopButton();

            return navigationService.Navigate<TModel>();
        }
    }
}
