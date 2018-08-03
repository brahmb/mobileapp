using System;
using System.ComponentModel;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Multivac;

namespace Toggl.Foundation.MvvmCross.ViewModels.Calendar
{
    [Preserve(AllMembers = true)]
    public sealed class CalendarDayViewModel : INotifyPropertyChanged
    {
        private readonly DateTimeOffset dateTime;

        public event PropertyChangedEventHandler PropertyChanged;

        public int Day { get; }

        public CalendarMonth CalendarMonth { get; }

        public bool IsInCurrentMonth { get; }

        public bool IsToday { get; }

        public bool Selected { get; private set; }

        public bool IsStartOfSelectedPeriod { get; private set; }

        public bool IsEndOfSelectedPeriod { get; private set; }

        public DateTimeOffset DateTimeOffset => dateTime;

        public CalendarDayViewModel(int day, CalendarMonth month, bool isInCurrentMonth, DateTimeOffset today)
        {
            Day = day;
            CalendarMonth = month;
            IsInCurrentMonth = isInCurrentMonth;
            dateTime = new DateTimeOffset(month.Year, month.Month, Day, 0, 0, 0, TimeSpan.Zero);
            IsToday = today.Date == dateTime.Date;
        }

        public void OnSelectedRangeChanged(ReportsDateRangeParameter selectedRange)
        {
            Selected = selectedRange != null && selectedRange.StartDate.Date <= dateTime.Date && selectedRange.EndDate.Date >= dateTime.Date;

            IsStartOfSelectedPeriod = selectedRange != null && selectedRange.StartDate.Date == dateTime.Date;

            IsEndOfSelectedPeriod = selectedRange != null && selectedRange.EndDate.Date == dateTime.Date;
        }
    }
}
