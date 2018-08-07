using System;
using Toggl.Foundation.MvvmCross.ViewModels.ReportsCalendar.QuickSelectShortcuts;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels.ReportsCalendarQuickSelectShortcuts
{
    public sealed class ReportsCalendarLastMonthQuickSelectShortcutTests
        : BaseReportsCalendarQuickSelectShortcutTests<ReportsCalendarLastMonthQuickSelectShortcut>
    {
        protected override DateTimeOffset CurrentTime => new DateTimeOffset(2016, 4, 4, 1, 2, 3, TimeSpan.Zero);
        protected override DateTime ExpectedStart => new DateTime(2016, 3, 1);
        protected override DateTime ExpectedEnd => new DateTime(2016, 3, 31);

        protected override ReportsCalendarLastMonthQuickSelectShortcut CreateQuickSelectShortcut()
            => new ReportsCalendarLastMonthQuickSelectShortcut(TimeService);

        protected override ReportsCalendarLastMonthQuickSelectShortcut TryToCreateQuickSelectShortCutWithNull()
            => new ReportsCalendarLastMonthQuickSelectShortcut(null);
    }
}
