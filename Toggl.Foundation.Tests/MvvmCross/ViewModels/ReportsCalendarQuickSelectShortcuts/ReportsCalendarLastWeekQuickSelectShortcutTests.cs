using System;
using Toggl.Foundation.MvvmCross.ViewModels.ReportsCalendar.QuickSelectShortcuts;
using Toggl.Multivac;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels.ReportsCalendarQuickSelectShortcuts
{
    public abstract class ReportsCalendarLastWeekQuickSelectShortcutTests
        : BaseReportsCalendarQuickSelectShortcutTests<ReportsCalendarLastWeekQuickSelectShortcut>
    {
        protected abstract BeginningOfWeek BeginningOfWeek { get; }

        protected sealed override ReportsCalendarLastWeekQuickSelectShortcut CreateQuickSelectShortcut()
            => new ReportsCalendarLastWeekQuickSelectShortcut(TimeService, BeginningOfWeek);

        protected sealed override ReportsCalendarLastWeekQuickSelectShortcut TryToCreateQuickSelectShortCutWithNull()
            => new ReportsCalendarLastWeekQuickSelectShortcut(null, BeginningOfWeek);

        public sealed class WhenBeginningOfWeekIsMonday
            : ReportsCalendarLastWeekQuickSelectShortcutTests
        {
            protected override BeginningOfWeek BeginningOfWeek => BeginningOfWeek.Monday;
            protected override DateTimeOffset CurrentTime => new DateTimeOffset(2017, 12, 26, 0, 0, 0, TimeSpan.Zero);
            protected override DateTime ExpectedStart => new DateTime(2017, 12, 18);
            protected override DateTime ExpectedEnd => new DateTime(2017, 12, 24);
        }

        public sealed class WhenBeginningOfWeekIsTuesday
            : ReportsCalendarLastWeekQuickSelectShortcutTests
        {
            protected override BeginningOfWeek BeginningOfWeek => BeginningOfWeek.Tuesday;
            protected override DateTimeOffset CurrentTime => new DateTimeOffset(2017, 12, 26, 0, 0, 0, TimeSpan.Zero);
            protected override DateTime ExpectedStart => new DateTime(2017, 12, 19);
            protected override DateTime ExpectedEnd => new DateTime(2017, 12, 25);
        }

        public sealed class WhenBeginningOfWeekIsWednesday
            : ReportsCalendarLastWeekQuickSelectShortcutTests
        {
            protected override BeginningOfWeek BeginningOfWeek => BeginningOfWeek.Wednesday;
            protected override DateTimeOffset CurrentTime => new DateTimeOffset(2017, 12, 26, 0, 0, 0, TimeSpan.Zero);
            protected override DateTime ExpectedStart => new DateTime(2017, 12, 13);
            protected override DateTime ExpectedEnd => new DateTime(2017, 12, 19);
        }

        public sealed class WhenBeginningOfWeekIsThursday
            : ReportsCalendarLastWeekQuickSelectShortcutTests
        {
            protected override BeginningOfWeek BeginningOfWeek => BeginningOfWeek.Thursday;
            protected override DateTimeOffset CurrentTime => new DateTimeOffset(2017, 12, 26, 0, 0, 0, TimeSpan.Zero);
            protected override DateTime ExpectedStart => new DateTime(2017, 12, 14);
            protected override DateTime ExpectedEnd => new DateTime(2017, 12, 20);
        }

        public sealed class WhenBeginningOfWeekIsFriday
            : ReportsCalendarLastWeekQuickSelectShortcutTests
        {
            protected override BeginningOfWeek BeginningOfWeek => BeginningOfWeek.Friday;
            protected override DateTimeOffset CurrentTime => new DateTimeOffset(2017, 12, 26, 0, 0, 0, TimeSpan.Zero);
            protected override DateTime ExpectedStart => new DateTime(2017, 12, 15);
            protected override DateTime ExpectedEnd => new DateTime(2017, 12, 21);
        }

        public sealed class WhenBeginningOfWeekIsSaturday
            : ReportsCalendarLastWeekQuickSelectShortcutTests
        {
            protected override BeginningOfWeek BeginningOfWeek => BeginningOfWeek.Saturday;
            protected override DateTimeOffset CurrentTime => new DateTimeOffset(2017, 12, 26, 0, 0, 0, TimeSpan.Zero);
            protected override DateTime ExpectedStart => new DateTime(2017, 12, 16);
            protected override DateTime ExpectedEnd => new DateTime(2017, 12, 22);
        }

        public sealed class WhenBeginningOfWeekIsSunday
            : ReportsCalendarLastWeekQuickSelectShortcutTests
        {
            protected override BeginningOfWeek BeginningOfWeek => BeginningOfWeek.Sunday;
            protected override DateTimeOffset CurrentTime => new DateTimeOffset(2017, 12, 26, 0, 0, 0, TimeSpan.Zero);
            protected override DateTime ExpectedStart => new DateTime(2017, 12, 17);
            protected override DateTime ExpectedEnd => new DateTime(2017, 12, 23);
        }
    }
}
