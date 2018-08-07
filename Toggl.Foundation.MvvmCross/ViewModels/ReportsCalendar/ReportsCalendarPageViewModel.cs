using System;
using System.Collections.Generic;
using MvvmCross.Binding.Bindings.Target;
using Toggl.Multivac;

namespace Toggl.Foundation.MvvmCross.ViewModels.ReportsCalendar
{
    public sealed class ReportsCalendarPageViewModel
    {
        private readonly BeginningOfWeek beginningOfWeek;
        private readonly DateTimeOffset today;

        public List<ReportsCalendarDayViewModel> Days { get; }
            = new List<ReportsCalendarDayViewModel>();

        public CalendarMonth CalendarMonth { get; }

        public int RowCount { get; }

        public ReportsCalendarPageViewModel(
            CalendarMonth calendarMonth, BeginningOfWeek beginningOfWeek, DateTimeOffset today)
        {
            this.beginningOfWeek = beginningOfWeek;
            this.today = today;

            CalendarMonth = calendarMonth;

            addDaysFromPreviousMonth();
            addDaysFromCurrentMonth();
            addDaysFromNextMonth();

            RowCount = Days.Count / 7;
        }

        private void addDaysFromPreviousMonth()
        {
            var firstDayOfMonth = CalendarMonth.DayOfWeek(1);

            if (firstDayOfMonth == beginningOfWeek.ToDayOfWeekEnum()) return;

            var previousMonth = CalendarMonth.Previous();
            var daysInPreviousMonth = previousMonth.DaysInMonth;
            var daysToAdd = ((int)firstDayOfMonth - (int)beginningOfWeek.ToDayOfWeekEnum() + 7) % 7;

            for (int i = daysToAdd - 1; i >= 0; i--)
                addDay(daysInPreviousMonth - i, previousMonth, false);
        }

        private void addDaysFromCurrentMonth()
        {
            var daysInMonth = CalendarMonth.DaysInMonth;
            for (int i = 0; i < daysInMonth; i++)
                addDay(i + 1, CalendarMonth, true);
        }

        private void addDaysFromNextMonth()
        {
            var lastDayOfWeekInTargetMonth = (int)CalendarMonth
                .DayOfWeek(CalendarMonth.DaysInMonth);

            var nextMonth = CalendarMonth.AddMonths(1);
            var lastDayOfWeek = ((int)beginningOfWeek + 6) % 7;
            var daysToAdd = (lastDayOfWeek - lastDayOfWeekInTargetMonth + 7) % 7;

            for (int i = 0; i < daysToAdd; i++)
                addDay(i + 1, nextMonth, false);
        }

        private void addDay(int day, CalendarMonth month, bool isCurrentMonth)
        {
            Days.Add(new ReportsCalendarDayViewModel(day, month, isCurrentMonth, today));
        }
    }
}
