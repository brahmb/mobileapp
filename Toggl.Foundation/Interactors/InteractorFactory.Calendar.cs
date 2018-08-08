using System;
using System.Collections.Generic;
using System.Reactive;
using Toggl.Foundation.Calendar;
using Toggl.Foundation.Interactors.Calendar;
using Toggl.Multivac;

namespace Toggl.Foundation.Interactors
{
    public sealed partial class InteractorFactory : IInteractorFactory
    {
        public IInteractor<IObservable<IEnumerable<CalendarItem>>> GetCalendarItemsForDate(DateTime date)
            => new GetCalendarItemsForDateInteractor(dataSource.TimeEntries, calendarService, date);

        public IInteractor<IObservable<IEnumerable<UserCalendar>>> GetUserCalendars()
            => new GetUserCalendarsInteractor(calendarService);

        public IInteractor<Unit> SetEnabledCalendars(params string[] ids)
            => new SetEnabledCalendarsInteractor(userPreferences, ids);
    }
}
