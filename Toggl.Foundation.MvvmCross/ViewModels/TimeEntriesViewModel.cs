using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
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

        private CompositeDisposable disposeBag = new CompositeDisposable();
        private bool areContineButtonsEnabled = true;
        private DurationFormat durationFormat;

        public ObservableGroupedOrderedCollection<TimeEntryViewModel> TimeEntries { get; }
        public IObservable<bool> Empty => TimeEntries.Empty;
        public IObservable<int> Count => TimeEntries.TotalCount;

        public TimeEntriesViewModel (ITogglDataSource dataSource,
                                     IInteractorFactory interactorFactory)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));

            this.dataSource = dataSource;
            this.interactorFactory = interactorFactory;

            TimeEntries = new ObservableGroupedOrderedCollection<TimeEntryViewModel>(
                indexKey: t => t.Id,
                orderingKey: t => t.StartTime,
                groupingKey: t => t.StartTime.LocalDateTime.Date,
                descending: true
            );
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
                var index = TimeEntries.IndexOf(timeEntry.Id);
                if (index.HasValue)
                    TimeEntries.RemoveItemAt(index.Value.Section, index.Value.Row);
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
    }
}
