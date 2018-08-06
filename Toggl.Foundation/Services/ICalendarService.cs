using System;
using System.Collections.Generic;
using Toggl.Foundation.Calendar;

namespace Toggl.Foundation.Services
{
    public interface ICalendarService
    {
        IObservable<IEnumerable<CalendarItem>> GetEventsForDate(DateTime date);
    }
}
