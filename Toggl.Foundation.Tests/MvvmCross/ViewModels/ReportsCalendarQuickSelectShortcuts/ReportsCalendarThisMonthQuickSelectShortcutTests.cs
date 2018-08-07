using System;
using Toggl.Foundation.MvvmCross.ViewModels.ReportsCalendar.QuickSelectShortcuts;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels.ReportsCalendarQuickSelectShortcuts
{
    public sealed class ReportsCalendarThisMonthQuickSelectShortcutTests
        : BaseReportsCalendarQuickSelectShortcutTests<ReportsCalendarThisMonthQuickSelectShortcut>
    {
        protected override DateTimeOffset CurrentTime => new DateTimeOffset(2017, 11, 28, 0, 0, 0, TimeSpan.Zero);
        protected override DateTime ExpectedStart => new DateTime(2017, 11, 1);
        protected override DateTime ExpectedEnd => new DateTime(2017, 11, 30);

        protected override ReportsCalendarThisMonthQuickSelectShortcut CreateQuickSelectShortcut()
            => new ReportsCalendarThisMonthQuickSelectShortcut(TimeService);

        protected override ReportsCalendarThisMonthQuickSelectShortcut TryToCreateQuickSelectShortCutWithNull()
            => new ReportsCalendarThisMonthQuickSelectShortcut(null);
    }
}
