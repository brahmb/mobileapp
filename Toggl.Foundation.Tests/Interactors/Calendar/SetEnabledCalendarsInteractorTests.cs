using System;
using System.Linq;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using NSubstitute;
using Toggl.Foundation.Interactors.Calendar;
using Toggl.Foundation.Tests.Generators;
using Xunit;

namespace Toggl.Foundation.Tests.Interactors.Calendar
{
    public sealed class SetEnabledCalendarsInteractorTests
    {
        public sealed class TheConstructor : BaseInteractorTests
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useUserPreferences, bool useCalendarIds)
            {
                Action tryingToConstructWithNulls = () => new SetEnabledCalendarsInteractor(
                    useUserPreferences ? UserPreferences : null,
                    useCalendarIds ? new string[0] : null
                );

                tryingToConstructWithNulls.Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheExecuteMethod : BaseInteractorTests
        {
            [Property]
            public void CallsTheCalendarService(NonEmptySet<NonEmptyString> nonEmptySet)
            {
                var calendarIds = nonEmptySet
                    .Get
                    .Select(str => str.Get)
                    .ToArray();

                InteractorFactory.SetEnabledCalendars(calendarIds).Execute();

                UserPreferences.Received().SetEnabledCalendars(calendarIds);
            }
        }
    }
}
