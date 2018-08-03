using Toggl.Foundation.Analytics;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Multivac;

namespace Toggl.Foundation.MvvmCross.ViewModels.Calendar.QuickSelectShortcuts
{
    public sealed class CalendarThisWeekQuickSelectShortcut
        : CalendarBaseQuickSelectShortcut
    {
        private readonly BeginningOfWeek beginningOfWeek;

        public CalendarThisWeekQuickSelectShortcut
            (ITimeService timeService, BeginningOfWeek beginningOfWeek)
            : base(timeService, Resources.ThisWeek)
        {
            this.beginningOfWeek = beginningOfWeek;
        }

        public override ReportsDateRangeParameter GetDateRange()
        {
            var now = TimeService.CurrentDateTime.Date;
            var difference = (now.DayOfWeek - beginningOfWeek.ToDayOfWeekEnum() + 7) % 7;
            var start = now.AddDays(-difference);
            var end = start.AddDays(6);
            return ReportsDateRangeParameter
                .WithDates(start, end)
                .WithSource(ReportsSource.ShortcutThisWeek);
        }
    }
}
