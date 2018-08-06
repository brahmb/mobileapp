using System;
using System.Collections.Generic;
using Toggl.Foundation.Calendar;
using Toggl.Foundation.Interactors.Calendar;

namespace Toggl.Foundation.Interactors
{
    public sealed partial class InteractorFactory : IInteractorFactory
    {
        public IInteractor<IObservable<IEnumerable<CalendarItem>>> GetCalendarItemsForDate(DateTime date)
            => new GetCalendarItemsForDateInteractor(dataSource.TimeEntries, calendarService, date);
    }
}
