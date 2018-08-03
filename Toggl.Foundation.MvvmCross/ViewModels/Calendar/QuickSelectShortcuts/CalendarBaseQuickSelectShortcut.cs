using MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Multivac;

namespace Toggl.Foundation.MvvmCross.ViewModels.Calendar.QuickSelectShortcuts
{
    [Preserve(AllMembers = true)]
    public abstract class CalendarBaseQuickSelectShortcut : MvxNotifyPropertyChanged
    {
        protected ITimeService TimeService { get; private set; }

        public string Title { get; }

        public bool Selected { get; private set; }

        protected CalendarBaseQuickSelectShortcut(
            ITimeService timeService, string title)
        {
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(title, nameof(title));

            TimeService = timeService;
            Title = title;
        }

        public void OnDateRangeChanged(ReportsDateRangeParameter dateRange)
        {
            var thisActionDateRange = GetDateRange();

            Selected = dateRange.StartDate.Date == thisActionDateRange.StartDate.Date
                    && dateRange.EndDate.Date == thisActionDateRange.EndDate.Date;
        }

        public abstract ReportsDateRangeParameter GetDateRange();
    }
}
