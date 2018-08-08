using System;
using System.Collections.Generic;
using Toggl.Foundation.Calendar;
using Toggl.Multivac;

namespace Toggl.Foundation.Services
{
    public interface ICalendarService
    {
        IObservable<IEnumerable<CalendarItem>> GetEventsForDate(DateTime date);

        IObservable<IEnumerable<UserCalendar>> UserCalendars { get; }
    }
}
