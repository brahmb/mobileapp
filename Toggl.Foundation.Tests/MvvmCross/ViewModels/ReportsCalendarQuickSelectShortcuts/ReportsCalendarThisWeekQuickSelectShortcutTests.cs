using System;
using Toggl.Foundation.MvvmCross.ViewModels.ReportsCalendar.QuickSelectShortcuts;
using Toggl.Multivac;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels.ReportsCalendarQuickSelectShortcuts
{
    public abstract class ReportsCalendarThisWeekQuickSelectShortcutTests
            : BaseReportsCalendarQuickSelectShortcutTests<ReportsCalendarThisWeekQuickSelectShortcut>
    {
        protected abstract BeginningOfWeek BeginningOfWeek { get; }

        protected sealed override ReportsCalendarThisWeekQuickSelectShortcut CreateQuickSelectShortcut()
            => new ReportsCalendarThisWeekQuickSelectShortcut(TimeService, BeginningOfWeek);

        protected sealed override ReportsCalendarThisWeekQuickSelectShortcut TryToCreateQuickSelectShortCutWithNull()
            => new ReportsCalendarThisWeekQuickSelectShortcut(null, BeginningOfWeek);

        public sealed class WhenBeginningOfWeekIsMonday
            : ReportsCalendarThisWeekQuickSelectShortcutTests
        {
            protected override BeginningOfWeek BeginningOfWeek => BeginningOfWeek.Monday;
            protected override DateTimeOffset CurrentTime => new DateTimeOffset(2017, 12, 26, 0, 0, 0, TimeSpan.Zero);
            protected override DateTime ExpectedStart => new DateTime(2017, 12, 25);
            protected override DateTime ExpectedEnd => new DateTime(2017, 12, 31);
        }

        public sealed class WhenBeginningOfWeekIsTuesday
           : ReportsCalendarThisWeekQuickSelectShortcutTests
        {
            protected override BeginningOfWeek BeginningOfWeek => BeginningOfWeek.Tuesday;
            protected override DateTimeOffset CurrentTime => new DateTimeOffset(2017, 12, 26, 0, 0, 0, TimeSpan.Zero);
            protected override DateTime ExpectedStart => new DateTime(2017, 12, 26);
            protected override DateTime ExpectedEnd => new DateTime(2018, 1, 1);
        }

        public sealed class WhenBeginningOfWeekIsWednesday
           : ReportsCalendarThisWeekQuickSelectShortcutTests
        {
            protected override BeginningOfWeek BeginningOfWeek => BeginningOfWeek.Wednesday;
            protected override DateTimeOffset CurrentTime => new DateTimeOffset(2017, 12, 26, 0, 0, 0, TimeSpan.Zero);
            protected override DateTime ExpectedStart => new DateTime(2017, 12, 20);
            protected override DateTime ExpectedEnd => new DateTime(2017, 12, 26);
        }

        public sealed class WhenBeginningOfWeekIsThursday
           : ReportsCalendarThisWeekQuickSelectShortcutTests
        {
            protected override BeginningOfWeek BeginningOfWeek => BeginningOfWeek.Thursday;
            protected override DateTimeOffset CurrentTime => new DateTimeOffset(2017, 12, 26, 0, 0, 0, TimeSpan.Zero);
            protected override DateTime ExpectedStart => new DateTime(2017, 12, 21);
            protected override DateTime ExpectedEnd => new DateTime(2017, 12, 27);
        }

        public sealed class WhenBeginningOfWeekIsFriday
           : ReportsCalendarThisWeekQuickSelectShortcutTests
        {
            protected override BeginningOfWeek BeginningOfWeek => BeginningOfWeek.Friday;
            protected override DateTimeOffset CurrentTime => new DateTimeOffset(2017, 12, 26, 0, 0, 0, TimeSpan.Zero);
            protected override DateTime ExpectedStart => new DateTime(2017, 12, 22);
            protected override DateTime ExpectedEnd => new DateTime(2017, 12, 28);
        }

        public sealed class WhenBeginningOfWeekIsSaturday
           : ReportsCalendarThisWeekQuickSelectShortcutTests
        {
            protected override BeginningOfWeek BeginningOfWeek => BeginningOfWeek.Saturday;
            protected override DateTimeOffset CurrentTime => new DateTimeOffset(2017, 12, 26, 0, 0, 0, TimeSpan.Zero);
            protected override DateTime ExpectedStart => new DateTime(2017, 12, 23);
            protected override DateTime ExpectedEnd => new DateTime(2017, 12, 29);
        }

        public sealed class WhenBeginningOfWeekIsSunday
           : ReportsCalendarThisWeekQuickSelectShortcutTests
        {
            protected override BeginningOfWeek BeginningOfWeek => BeginningOfWeek.Sunday;
            protected override DateTimeOffset CurrentTime => new DateTimeOffset(2017, 12, 26, 0, 0, 0, TimeSpan.Zero);
            protected override DateTime ExpectedStart => new DateTime(2017, 12, 24);
            protected override DateTime ExpectedEnd => new DateTime(2017, 12, 30);
        }
    }
}
