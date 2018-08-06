using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Calendar;
using Toggl.Foundation.Interactors.Calendar;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Tests.Mocks;
using Toggl.PrimeRadiant.Models;
using Xunit;

namespace Toggl.Foundation.Tests.Interactors.Calendar
{
    public sealed class GetCalendarItemsForDateInteractorTests
    {
        public sealed class TheExecuteMethod : BaseInteractorTests
        {
            private readonly DateTime date;
            private readonly GetCalendarItemsForDateInteractor interactor;

            private readonly List<CalendarItem> calendarEvents;

            private readonly List<IThreadSafeTimeEntry> timeEntries;
            private readonly List<CalendarItem> calendarItemsFromTimeEntries;

            public TheExecuteMethod()
            {
                calendarEvents = new List<CalendarItem>
                {
                    new CalendarItem(CalendarItemSource.Calendar,
                        new DateTimeOffset(2018, 08, 06, 10, 30, 00, TimeSpan.Zero),
                        TimeSpan.FromMinutes(30),
                        "Important meeting",
                        "#0000ff"),
                    new CalendarItem(CalendarItemSource.Calendar,
                        new DateTimeOffset(2018, 08, 06, 10, 00, 00, TimeSpan.Zero),
                        TimeSpan.FromMinutes(90),
                        "F**** timesheets",
                        "#0000ff"),
                    new CalendarItem(CalendarItemSource.Calendar,
                        new DateTimeOffset(2018, 08, 06, 09, 00, 00, TimeSpan.Zero),
                        TimeSpan.FromMinutes(15),
                        "Not so important meeting",
                        "#0000ff")
                };

                timeEntries = new List<IThreadSafeTimeEntry>
                {
                    new MockTimeEntry()
                    {
                        Id = 1,
                        Description = "Something in project A",
                        Start = new DateTimeOffset(2018, 08, 06, 13, 00, 00, TimeSpan.Zero),
                        Duration = 45
                    },
                    new MockTimeEntry()
                    {
                        Id = 2,
                        Description = "Something in project B",
                        Start = new DateTimeOffset(2018, 08, 06, 11, 45, 00, TimeSpan.Zero),
                        Duration = 15
                    },
                    new MockTimeEntry()
                    {
                        Id = 3,
                        Description = "Something without project",
                        Start = new DateTimeOffset(2018, 08, 06, 09, 45, 00, TimeSpan.Zero),
                        Duration = 10
                    }
                };

                calendarItemsFromTimeEntries = timeEntries.Select(CalendarItem.From).ToList();

                date = new DateTime(2018, 08, 06);

                CalendarService
                    .GetEventsForDate(Arg.Is(date))
                    .Returns(Observable.Return(calendarEvents));

                DataSource
                    .TimeEntries
                    .GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>())
                    .Returns(Observable.Return(timeEntries));

                interactor = new GetCalendarItemsForDateInteractor(DataSource.TimeEntries, CalendarService, date);
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsAllCalendarItemsForTheGivenDate()
            {
                var calendarItems = await interactor.Execute();

                calendarItems.Should().Contain(calendarEvents);
                calendarItems.Should().Contain(calendarItemsFromTimeEntries);
                calendarItems.Count().Should().Be(calendarEvents.Count + calendarItemsFromTimeEntries.Count);
            }

            [Fact, LogIfTooSlow]
            public async Task RetunsAllCalendarItemsOrderedByStartDate()
            {
                var calendarItems = await interactor.Execute();

                calendarItems.Should().BeInAscendingOrder(calendarItem => calendarItem.StartTime);
            }
        }
    }
}
