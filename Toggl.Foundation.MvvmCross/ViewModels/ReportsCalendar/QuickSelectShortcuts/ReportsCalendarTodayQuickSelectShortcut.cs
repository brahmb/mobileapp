using Toggl.Foundation.Analytics;
using Toggl.Foundation.MvvmCross.Parameters;

namespace Toggl.Foundation.MvvmCross.ViewModels.ReportsCalendar.QuickSelectShortcuts
{
    public sealed class ReportsCalendarTodayQuickSelectShortcut : ReportsCalendarBaseQuickSelectShortcut
    {
        public ReportsCalendarTodayQuickSelectShortcut(ITimeService timeService)
            : base(timeService, Resources.Today)
        {
        }

        public override DateRangeParameter GetDateRange()
        {
            var today = TimeService.CurrentDateTime.Date;
            return DateRangeParameter
                .WithDates(today, today)
                .WithSource(ReportsSource.ShortcutToday);
        }
    }
}
