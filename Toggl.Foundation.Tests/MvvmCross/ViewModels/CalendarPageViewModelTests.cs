using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Toggl.Foundation.MvvmCross.ViewModels.Calendar;
using Toggl.Multivac;
using Xunit;
using Math = System.Math;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class CalendarPageViewModelTests
    {
        private class CalendarPageTestDataFiveParameters : IEnumerable<object[]>
        {
            protected virtual List<object[]> getData() => new List<object[]>
            {
                new object[] { 2017, 12, BeginningOfWeek.Monday, 4, 0 },
                new object[] { 2017, 12, BeginningOfWeek.Sunday, 5, 6 },
                new object[] { 2017, 7, BeginningOfWeek.Saturday, 0, 4 },
                new object[] { 2017, 11, BeginningOfWeek.Thursday, 6, 6 },
                new object[] { 2017, 2, BeginningOfWeek.Wednesday, 0, 0 }
            };

            public IEnumerator<object[]> GetEnumerator()
                => getData().GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();
        }

        private sealed class CalendarPageTestDataFourParameters : CalendarPageTestDataFiveParameters
        {
            protected override List<object[]> getData()
                => base.getData().Select(o => o.Take(4).ToArray()).ToList();
        }

        public sealed class TheDaysProperty
        {
            private CalendarMonth calendarMonth;
            private CalendarPageViewModel viewModel;

            private void prepare(
                int year, int month, BeginningOfWeek beginningOfWeek, DateTimeOffset? today = null)
            {
                calendarMonth = new CalendarMonth(year, month);
                viewModel = new CalendarPageViewModel(calendarMonth, beginningOfWeek, today ?? DateTimeOffset.Now);
            }

            [Theory, LogIfTooSlow]
            [ClassData(typeof(CalendarPageTestDataFourParameters))]
            public void ContainsFewDaysFromPreviousMonthAtTheBeginning(
                int year,
                int month,
                BeginningOfWeek beginningOfWeek,
                int expectedDayCount)
            {
                prepare(year, month, beginningOfWeek);

                if (expectedDayCount == 0)
                    viewModel.Days.First().IsInCurrentMonth.Should().BeTrue();
                else
                    viewModel
                        .Days
                        .Take(expectedDayCount)
                        .Should()
                        .OnlyContain(day => !day.IsInCurrentMonth);
            }

            [Theory, LogIfTooSlow]
            [ClassData(typeof(CalendarPageTestDataFourParameters))]
            public void ConainsAllDaysFromCurrentMonthInTheMiddle(
                int year,
                int month,
                BeginningOfWeek beginningOfWeek,
                int daysFromLastMonth)
            {
                prepare(year, month, beginningOfWeek);

                viewModel
                    .Days
                    .Skip(daysFromLastMonth)
                    .Take(calendarMonth.DaysInMonth)
                    .Should()
                    .OnlyContain(day => day.IsInCurrentMonth);
            }

            [Theory, LogIfTooSlow]
            [ClassData(typeof(CalendarPageTestDataFiveParameters))]
            public void ContainsFewDaysFromNextMonthAtTheEnd(
                int year,
                int month,
                BeginningOfWeek beginningOfWeek,
                int daysFromLastMonth,
                int expectedDayCount)
            {
                prepare(year, month, beginningOfWeek);
                var daysFromNextMonth = viewModel
                    .Days
                    .Skip(daysFromLastMonth)
                    .Skip(calendarMonth.DaysInMonth);

                if (expectedDayCount == 0)
                    daysFromNextMonth.Should().BeEmpty();
                else
                    daysFromNextMonth
                        .Should()
                        .OnlyContain(day => !day.IsInCurrentMonth);
            }

            [Property]
            public void MarksTheCurrentDayAndNoOtherDayAsToday(
                int year,
                int month,
                int today,
                BeginningOfWeek beginningOfWeek)
            {
                year = Math.Abs(year % 25) + 2000;
                month = Math.Abs(month % 12) + 1;
                today = Math.Abs(today % DateTime.DaysInMonth(year, month)) + 1;
                prepare(year, month, beginningOfWeek, new DateTimeOffset(year, month, today, 11, 22, 33, TimeSpan.Zero));

                viewModel.Days.Should().OnlyContain(day =>
                    ((day.CalendarMonth.Month != month || day.Day != today) && day.IsToday == false)
                    || (day.CalendarMonth.Month == month && day.Day == today && day.IsToday));
            }

            [Property]
            public void AlwaysIsAMultipleOf7(
                NonNegativeInt year,
                NonNegativeInt month,
                BeginningOfWeek beginingOfWeek)
            {
                prepare(year.Get % 9999 + 2, month.Get % 12 + 1, beginingOfWeek);

                (viewModel.Days.Count % 7).Should().Be(0);
            }
        }
    }
}
