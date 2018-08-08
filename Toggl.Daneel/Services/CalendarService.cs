using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using EventKit;
using Toggl.Foundation.Calendar;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.PrimeRadiant.Settings;

namespace Toggl.Daneel.Services
{
    public sealed class CalendarService : ICalendarService
    {
        private readonly IUserPreferences userPreferences;
        private readonly IPermissionsService permissionsService;
        private readonly EKEventStore eventStore = new EKEventStore();

        public CalendarService(
            IUserPreferences userPreferences,
            IPermissionsService permissionsService)
        {
            Ensure.Argument.IsNotNull(userPreferences, nameof(userPreferences));
            Ensure.Argument.IsNotNull(permissionsService, nameof(permissionsService));

            this.userPreferences = userPreferences;
            this.permissionsService = permissionsService;
        }

        public IObservable<IEnumerable<CalendarItem>> GetEventsForDate(DateTime date)
            => Observable.Return(new List<CalendarItem>());

        public IObservable<IEnumerable<UserCalendar>> UserCalendars
            => Observable.DeferAsync(async (cancellactionToken) =>
                {
                    var isAuthorized = await permissionsService.CalendarAuthorizationStatus;
                    if (!isAuthorized)
                    {
                        return Observable.Throw<IEnumerable<UserCalendar>>(
                            new NotAuthorizedException("You don't have permission to access calendars")
                        );
                    }

                    var enabledIds = userPreferences.EnabledCalendarIds().ToHashSet();
                    return Observable.Return(selectCalendars(enabledIds));
                });

        private IEnumerable<UserCalendar> selectCalendars(HashSet<string> selectedIds)
            => eventStore
                .GetCalendars(EKEntityType.Event)
                .Select(ekCalendar => userCalendarFromEKCalendar(
                    calendar: ekCalendar,
                    selected: selectedIds.Contains(ekCalendar.CalendarIdentifier))
                );
        
        private UserCalendar userCalendarFromEKCalendar(EKCalendar calendar, bool selected)
            => new UserCalendar(
                calendar.CalendarIdentifier,
                calendar.Title,
                calendar.Source.Title,
                selected);
    }
}
