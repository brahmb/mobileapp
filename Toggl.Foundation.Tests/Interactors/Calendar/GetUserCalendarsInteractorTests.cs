using System;
using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Interactors.Calendar;
using Toggl.Multivac;
using Xunit;

namespace Toggl.Foundation.Tests.Interactors.Calendar
{
    public sealed class GetUserCalendarsInteractorTests
    {
        public sealed class TheConstructor : BaseInteractorTests
        {
            [Fact]
            public void ThrowsIfTheArgumentIsNull()
            {
                Action tryingToConstructWithNulls = () => new GetUserCalendarsInteractor(null);

                tryingToConstructWithNulls.Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheExecuteMethod : BaseInteractorTests
        {
            [Fact]
            public void ReturnsTheObservableFromCalendarService()
            {
                var observable = Substitute.For<IObservable<IEnumerable<UserCalendar>>>();
                CalendarService.UserCalendars.Returns(observable);

                InteractorFactory.GetUserCalendars().Execute().Should().BeSameAs(observable);
            }
        }
    }
}
