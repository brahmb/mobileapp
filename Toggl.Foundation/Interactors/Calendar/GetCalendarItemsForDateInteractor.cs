using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Toggl.Foundation.Calendar;
using Toggl.Foundation.DataSources;
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
            ITimeEntriesSource timeEntriesSource,
            ICalendarService calendarService,
            DateTime date)
        {
            Ensure.Argument.IsNotNull(timeEntriesSource, nameof(timeEntriesSource));
            Ensure.Argument.IsNotNull(calendarService, nameof(calendarService));
            Ensure.Argument.IsNotNull(date, nameof(date));
        }

        public IObservable<IEnumerable<CalendarItem>> Execute()
            => Observable.Return(new List<CalendarItem>());
    }
}
