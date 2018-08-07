using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Foundation.Calendar;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Services;
using Toggl.Multivac;

namespace Toggl.Foundation.Interactors.Calendar
{
    public sealed class GetCalendarItemsForDateInteractor : IInteractor<IObservable<IEnumerable<CalendarItem>>>
    {
        private readonly ITimeEntriesSource timeEntriesDataSource;
        private readonly ICalendarService calendarService;
        private readonly DateTime date;

        public GetCalendarItemsForDateInteractor(
            ITimeEntriesSource timeEntriesDataSource,
            ICalendarService calendarService,
            DateTime date)
        {
            Ensure.Argument.IsNotNull(timeEntriesDataSource, nameof(timeEntriesDataSource));
            Ensure.Argument.IsNotNull(calendarService, nameof(calendarService));
            Ensure.Argument.IsNotNull(date, nameof(date));

            this.timeEntriesDataSource = timeEntriesDataSource;
            this.calendarService = calendarService;
            this.date = date;
        }

        public IObservable<IEnumerable<CalendarItem>> Execute()
            => Observable.CombineLatest(
                    calendarItemsFromTimeEntries(),
                    calendarItemsFromEvents(),
                    (timeEntries, events) => timeEntries.Concat(events))
                .Select(orderByStartTime);

        private IObservable<IEnumerable<CalendarItem>> calendarItemsFromTimeEntries()
            => timeEntriesDataSource.GetAll(timeEntry => timeEntry.Start >= date.Date && timeEntry.Start <= date.AddDays(1).Date && timeEntry.Duration != null)
                .Select(convertTimeEntriesToCalendarItems);

        private IObservable<IEnumerable<CalendarItem>> calendarItemsFromEvents()
            => calendarService.GetEventsForDate(date);

        private IEnumerable<CalendarItem> convertTimeEntriesToCalendarItems(IEnumerable<IThreadSafeTimeEntry> timeEntries)
            => timeEntries.Select(CalendarItem.From);

        private IEnumerable<CalendarItem> orderByStartTime(IEnumerable<CalendarItem> calendarItems)
            => calendarItems.OrderBy(calendarItem => calendarItem.StartTime);
    }
}
