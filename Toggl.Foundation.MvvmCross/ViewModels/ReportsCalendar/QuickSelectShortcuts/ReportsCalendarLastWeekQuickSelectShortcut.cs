using Toggl.Foundation.Analytics;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Multivac;

namespace Toggl.Foundation.MvvmCross.ViewModels.ReportsCalendar.QuickSelectShortcuts
{
    public sealed class ReportsCalendarLastWeekQuickSelectShortcut
        : ReportsCalendarBaseQuickSelectShortcut
    {
        private readonly BeginningOfWeek beginningOfWeek;

        public ReportsCalendarLastWeekQuickSelectShortcut(
            ITimeService timeService, BeginningOfWeek beginningOfWeek)
            : base(timeService, Resources.LastWeek)
        {
            this.beginningOfWeek = beginningOfWeek;
        }

        public override DateRangeParameter GetDateRange()
        {
            var now = TimeService.CurrentDateTime.Date;
            var difference = (now.DayOfWeek - beginningOfWeek.ToDayOfWeekEnum() + 7) % 7;
            var start = now.AddDays(-(difference + 7));
            var end = start.AddDays(6);
            return DateRangeParameter
                .WithDates(start, end)
                .WithSource(ReportsSource.ShortcutLastWeek);
        }
    }
}
