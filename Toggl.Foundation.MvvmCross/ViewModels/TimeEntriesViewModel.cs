using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Helper;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Settings;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class TimeEntriesViewModel
    {
        private readonly ITogglDataSource dataSource;
        private readonly IInteractorFactory interactorFactory;
        private readonly IAnalyticsService analyticsService;

        private CompositeDisposable disposeBag = new CompositeDisposable();
        private bool areContineButtonsEnabled = true;
        private DurationFormat durationFormat;

        public ObservableGroupedOrderedCollection<TimeEntryViewModel> TimeEntries { get; }
        public IObservable<bool> Empty => TimeEntries.Empty;
        public IObservable<int> Count => TimeEntries.TotalCount;

        private readonly Subject<bool> showUndoSubject = new Subject<bool>();
        private IDisposable delayedDeletionDisposable;
        private TimeEntryViewModel timeEntryToDelete;

        public IObservable<bool> ShouldShowUndo => showUndoSubject.AsObservable();

        public InputAction<TimeEntryViewModel> DelayDeleteTimeEntry { get; }
        public UIAction CancelDeleteTimeEntry { get; }

        public TimeEntriesViewModel (ITogglDataSource dataSource,
                                     IInteractorFactory interactorFactory,
                                     IAnalyticsService analyticsService)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));

            this.dataSource = dataSource;
            this.interactorFactory = interactorFactory;
            this.analyticsService = analyticsService;

            TimeEntries = new ObservableGroupedOrderedCollection<TimeEntryViewModel>(
                indexKey: t => t.Id,
                orderingKey: t => t.StartTime,
                groupingKey: t => t.StartTime.LocalDateTime.Date,
                descending: true
            );

            DelayDeleteTimeEntry = new InputAction<TimeEntryViewModel>(delayDeleteTimeEntry);
            CancelDeleteTimeEntry = new UIAction(cancelDeleteTimeEntry);
        }

        public async Task Initialize()
        {
            await fetchSectionedTimeEntries();

            disposeBag = new CompositeDisposable();

            dataSource.TimeEntries.Created
                .Where(isNotRunning)
                .Subscribe(onTimeEntryAdded)
                .DisposedBy(disposeBag);

            dataSource.TimeEntries.Deleted
                .Subscribe(onTimeEntryRemoved)
                .DisposedBy(disposeBag);

            dataSource.TimeEntries.Updated
                .Subscribe(onTimeEntryUpdated)
                .DisposedBy(disposeBag);

            dataSource.Preferences.Current
                .Subscribe(onPreferencesChanged)
                .DisposedBy(disposeBag);
        }


        private IObservable<Unit> delayDeleteTimeEntry(TimeEntryViewModel timeEntry)
        {
            timeEntryToDelete = timeEntry;

            remove(timeEntry.Id);
            showUndoSubject.OnNext(true);

            delayedDeletionDisposable = Observable.Merge( // If 5 seconds pass or we try to delete another TE
                    Observable.Return(timeEntry).Delay(Constants.UndoTime),
                    showUndoSubject.Where(t => t).Select(timeEntry)
                )
                .Take(1)
                .SelectMany(deleteTimeEntry)
                .Do(te =>
                {
                    if (te == timeEntryToDelete) // Hide bar if there isn't other TE trying to be deleted
                        showUndoSubject.OnNext(false);
                })
                .Subscribe();

            return Observable.Return(Unit.Default);
        }

        private IObservable<Unit> cancelDeleteTimeEntry()
        {
            add(timeEntryToDelete);
            timeEntryToDelete = null;
            delayedDeletionDisposable.Dispose();
            showUndoSubject.OnNext(false);
            return Observable.Return(Unit.Default);
        }

        private IObservable<TimeEntryViewModel> deleteTimeEntry(TimeEntryViewModel timeEntry)
        {
            return interactorFactory
                .DeleteTimeEntry(timeEntry.Id)
                .Execute()
                .Do(_ =>
                {
                    analyticsService.DeleteTimeEntry.Track();
                    dataSource.SyncManager.PushSync();
                })
                .Select(timeEntry);
        }

        private async Task fetchSectionedTimeEntries()
        {
            var groupedEntries = await interactorFactory.GetAllNonDeletedTimeEntries().Execute()
                .Select(entries => entries
                    .Where(isNotRunning)
                    .Select(te => new TimeEntryViewModel(te, durationFormat))
                );

            TimeEntries.ReplaceWith(groupedEntries);
        }

        private void onTimeEntryUpdated(EntityUpdate<IThreadSafeTimeEntry> update)
        {
            var timeEntry = update.Entity;
            if (timeEntry == null) return;

            if (timeEntry.IsDeleted || timeEntry.IsRunning())
            {
                remove(timeEntry.Id);
            }
            else
            {
                var timeEntryViewModel = new TimeEntryViewModel(timeEntry, durationFormat);
                TimeEntries.UpdateItem(timeEntryViewModel);
            }
        }

        private void onTimeEntryAdded(IThreadSafeTimeEntry timeEntry)
        {
            var timeEntryViewModel = new TimeEntryViewModel(timeEntry, durationFormat);
            TimeEntries.InsertItem(timeEntryViewModel);
        }

        private void onTimeEntryRemoved(long id)
        {
            var index = TimeEntries.IndexOf(id);
            if (index.HasValue)
                TimeEntries.RemoveItemAt(index.Value.Section, index.Value.Row);
        }

        private void onPreferencesChanged(IThreadSafePreferences preferences)
        {
            durationFormat = preferences.DurationFormat;
            fetchSectionedTimeEntries();
        }

        private bool isNotRunning(IThreadSafeTimeEntry timeEntry) => !timeEntry.IsRunning();

        private void remove(long id)
        {
            var index = TimeEntries.IndexOf(id);
            if (index.HasValue)
                TimeEntries.RemoveItemAt(index.Value.Section, index.Value.Row);
        }

        private void add(TimeEntryViewModel timeEntryViewModel)
        {
            if (!TimeEntries.IndexOf(timeEntryViewModel.Id).HasValue)
            {
                TimeEntries.InsertItem(timeEntryViewModel);
            }
        }
    }
}
