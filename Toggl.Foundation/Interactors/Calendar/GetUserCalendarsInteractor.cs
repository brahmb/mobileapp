using System;
using System.Collections.Generic;
using Toggl.Foundation.Services;
using Toggl.Multivac;

namespace Toggl.Foundation.Interactors.Calendar
{
    public sealed class GetUserCalendarsInteractor : IInteractor<IObservable<IEnumerable<UserCalendar>>>
    {
        private readonly ICalendarService calendarService;

        public GetUserCalendarsInteractor(ICalendarService calendarService)
        {
            Ensure.Argument.IsNotNull(calendarService, nameof(calendarService));

            this.calendarService = calendarService;
        }

        public IObservable<IEnumerable<UserCalendar>> Execute()
            => calendarService.UserCalendars;
    }
}
