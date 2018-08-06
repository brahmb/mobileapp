using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Toggl.Foundation.Calendar;
using Toggl.Foundation.Services;

namespace Toggl.Giskard.Services
{
    public sealed class CalendarService : ICalendarService
    {
        public IObservable<IEnumerable<CalendarItem>> GetEventsForDate(DateTime date)
            => Observable.Return(new List<CalendarItem>());
    }
}
