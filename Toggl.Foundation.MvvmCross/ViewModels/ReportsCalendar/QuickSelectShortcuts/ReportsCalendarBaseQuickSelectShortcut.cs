using MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Multivac;

namespace Toggl.Foundation.MvvmCross.ViewModels.ReportsCalendar.QuickSelectShortcuts
{
    [Preserve(AllMembers = true)]
    public abstract class ReportsCalendarBaseQuickSelectShortcut : MvxNotifyPropertyChanged
    {
        protected ITimeService TimeService { get; private set; }

        public string Title { get; }

        public bool Selected { get; private set; }

        protected ReportsCalendarBaseQuickSelectShortcut(
            ITimeService timeService, string title)
        {
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(title, nameof(title));

            TimeService = timeService;
            Title = title;
        }

        public void OnDateRangeChanged(DateRangeParameter dateRange)
        {
            var thisActionDateRange = GetDateRange();

            Selected = dateRange.StartDate.Date == thisActionDateRange.StartDate.Date
                    && dateRange.EndDate.Date == thisActionDateRange.EndDate.Date;
        }

        public abstract DateRangeParameter GetDateRange();
    }
}
