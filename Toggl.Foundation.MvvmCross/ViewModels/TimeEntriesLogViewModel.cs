using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using PropertyChanged;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.ViewModels.Hints;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Settings;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class TimeEntriesLogViewModel : MvxViewModel
    {
        private readonly CompositeDisposable disposeBag = new CompositeDisposable();

        private readonly ITimeService timeService;
        private readonly ITogglDataSource dataSource;
        private readonly IInteractorFactory interactorFactory;
        private readonly IOnboardingStorage onboardingStorage;
        private readonly IAnalyticsService analyticsService;
        private readonly IMvxNavigationService navigationService;

        private bool areContineButtonsEnabled = true;

        private DurationFormat durationFormat;

        public NestableObservableCollection<TimeEntryViewModelCollection, TimeEntryViewModel> TimeEntries { get; }
            = new NestableObservableCollection<TimeEntryViewModelCollection, TimeEntryViewModel>(
                newTimeEntry => vm => vm.StartTime < newTimeEntry.StartTime
            );

        [DependsOn(nameof(TimeEntries))]
        public bool IsEmpty => TimeEntries.None();

        public IMvxAsyncCommand<TimeEntryViewModel> EditCommand { get; }

        public IMvxAsyncCommand<TimeEntryViewModel> DeleteCommand { get; }

        public MvxAsyncCommand<TimeEntryViewModel> ContinueTimeEntryCommand { get; }

        public TimeEntriesLogViewModel(ITimeService timeService,
                                       ITogglDataSource dataSource,
                                       IInteractorFactory interactorFactory,
                                       IOnboardingStorage onboardingStorage,
                                       IAnalyticsService analyticsService,
                                       IMvxNavigationService navigationService)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(onboardingStorage, nameof(onboardingStorage));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));

            this.dataSource = dataSource;
            this.timeService = timeService;
            this.onboardingStorage = onboardingStorage;
            this.analyticsService = analyticsService;
            this.interactorFactory = interactorFactory;
            this.navigationService = navigationService;

            EditCommand = new MvxAsyncCommand<TimeEntryViewModel>(edit);
            DeleteCommand = new MvxAsyncCommand<TimeEntryViewModel>(delete);
            ContinueTimeEntryCommand = new MvxAsyncCommand<TimeEntryViewModel>(continueTimeEntry, _ => areContineButtonsEnabled);
        }

        public override async Task Initialize()
        {
            await base.Initialize();

            await fetchSectionedTimeEntries();

            var deleteDisposable =
                dataSource.TimeEntries.Deleted
                    .Subscribe(safeRemoveTimeEntry);

            var updateDisposable =
                dataSource.TimeEntries.Updated
                    .Subscribe(onTimeEntryUpdated);

            var createDisposable =
                dataSource.TimeEntries.Created
                    .Where(isNotRunning)
                    .Subscribe(safeInsertTimeEntry);

            var preferencesDisposable =
                dataSource.Preferences.Current
                    .Subscribe(onPreferencesChanged);

            disposeBag.Add(createDisposable);
            disposeBag.Add(updateDisposable);
            disposeBag.Add(deleteDisposable);
            disposeBag.Add(preferencesDisposable);
        }

        private async Task fetchSectionedTimeEntries()
        {
            var timeEntries = await interactorFactory.GetAllNonDeletedTimeEntries().Execute();
            if (timeEntries == null)
            {
                TimeEntries.Clear();
                return;
            }

            var groupedEntries = timeEntries
                .Where(isNotRunning)
                .OrderByDescending(te => te.Start)
                .Select(te => new TimeEntryViewModel(te, durationFormat))
                .GroupBy(te => te.StartTime.LocalDateTime.Date)
                .Select(grouping => new TimeEntryViewModelCollection(grouping.Key, grouping, durationFormat));

            TimeEntries.ReplaceWith(groupedEntries);
        }

        private void onTimeEntryUpdated(EntityUpdate<IThreadSafeTimeEntry> update)
        {
            var timeEntry = update.Entity;
            var shouldBeAdded = timeEntry != null && !timeEntry.IsRunning() && !timeEntry.IsDeleted;

            var oldCollectionIndex = TimeEntries.IndexOf(c => c.Any(vm => vm.Id == update.Id));
            var collectionIndex = TimeEntries.IndexOf(vm => vm.Date == timeEntry.Start.LocalDateTime.Date);
            var wasMovedIntoDifferentCollection = oldCollectionIndex >= 0 && oldCollectionIndex != collectionIndex;

            var shouldBeRemoved = shouldBeAdded == false || wasMovedIntoDifferentCollection;
            if (shouldBeRemoved)
            {
                safeRemoveTimeEntry(update.Id);
            }

            if (shouldBeAdded)
            {
                var timeEntryIndex = collectionIndex < 0 ? -1 : TimeEntries[collectionIndex].IndexOf(vm => vm.Id == update.Id);
                var timeEntryExistsInTheCollection = timeEntryIndex >= 0;
                if (timeEntryExistsInTheCollection)
                {
                    var timeEntryViewModel = new TimeEntryViewModel(timeEntry, durationFormat);
                    TimeEntries.ReplaceInChildCollection(collectionIndex, timeEntryIndex, timeEntryViewModel);
                    return;
                }

                safeInsertTimeEntry(timeEntry);
            }
        }

        private void safeInsertTimeEntry(IThreadSafeTimeEntry timeEntry)
        {
            var indexDate = timeEntry.Start.LocalDateTime.Date;
            var collectionIndex = TimeEntries.IndexOf(x => x.Date.LocalDateTime == indexDate);
            var groupExists = collectionIndex >= 0;
            if (groupExists)
            {
                insertTimeEntryInGroup(timeEntry, collectionIndex);
            }
            else
            {
                insertNewTimeEntryGroup(timeEntry, indexDate);
            }

            RaisePropertyChanged(nameof(IsEmpty));
        }

        private void insertTimeEntryInGroup(IThreadSafeTimeEntry timeEntry, int collectionIndex)
        {
            var timeEntryViewModel = new TimeEntryViewModel(timeEntry, durationFormat);
            TimeEntries.InsertInChildCollection(collectionIndex, timeEntryViewModel);
        }

        private void insertNewTimeEntryGroup(IThreadSafeTimeEntry timeEntry, DateTime indexDate)
        {
            var timeEntryToAdd = new TimeEntryViewModel(timeEntry, durationFormat);
            var newCollection = new TimeEntryViewModelCollection(indexDate, new[] { timeEntryToAdd }, durationFormat);
            var foundIndex = TimeEntries.IndexOf(TimeEntries.FirstOrDefault(x => x.Date < indexDate));
            var indexToInsert = foundIndex == -1 ? TimeEntries.Count : foundIndex;
            TimeEntries.Insert(indexToInsert, newCollection);
        }

        private void safeRemoveTimeEntry(long id)
        {
            var collectionIndex = TimeEntries.IndexOf(c => c.Any(vm => vm.Id == id));
            if (collectionIndex < 0) return;

            var item = TimeEntries[collectionIndex].First(vm => vm.Id == id);
            TimeEntries.RemoveFromChildCollection(collectionIndex, item);
            RaisePropertyChanged(nameof(IsEmpty));
        }

        private void onPreferencesChanged(IThreadSafePreferences preferences)
        {
            durationFormat = preferences.DurationFormat;

            foreach (var collection in TimeEntries)
            {
                collection.DurationFormat = durationFormat;

                foreach (var timeEntry in collection)
                {
                    timeEntry.DurationFormat = durationFormat;
                }
            }

            fetchSectionedTimeEntries();
        }

        private bool isNotRunning(IThreadSafeTimeEntry timeEntry) => !timeEntry.IsRunning();

        private Task edit(TimeEntryViewModel timeEntryViewModel)
        {
            onboardingStorage.TimeEntryWasTapped();

            return navigationService.Navigate<EditTimeEntryViewModel, long>(timeEntryViewModel.Id);
        }

        private async Task delete(TimeEntryViewModel timeEntryViewModel)
        {
            await interactorFactory.DeleteTimeEntry(timeEntryViewModel.Id).Execute();

            analyticsService.DeleteTimeEntry.Track();
            dataSource.SyncManager.PushSync();
        }

        private async Task continueTimeEntry(TimeEntryViewModel timeEntryViewModel)
        {
            areContineButtonsEnabled = false;
            ContinueTimeEntryCommand.RaiseCanExecuteChanged();

            onboardingStorage.SetTimeEntryContinued();

            await interactorFactory
                .ContinueTimeEntry(timeEntryViewModel)
                .Execute()
                .Do(_ =>
                {
                    areContineButtonsEnabled = true;
                    ContinueTimeEntryCommand.RaiseCanExecuteChanged();
                });
        }
    }
}
