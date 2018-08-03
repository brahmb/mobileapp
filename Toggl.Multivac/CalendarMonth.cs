using System;
using System.Runtime.CompilerServices;

namespace Toggl.Multivac
{
    public struct CalendarMonth
    {
        public int Year { get; }

        /// <summary>
        /// Ranged from 1 to 12 for January - December
        /// </summary>
        public int Month { get; }

        public int DaysInMonth
            => DateTime.DaysInMonth(Year, Month);

        public CalendarMonth(int year, int month)
        {
            if (year < 0)
                throw new ArgumentOutOfRangeException(
                    $"{nameof(year)} can't be negative");

            if (month < 1 || month > 12)
                throw new ArgumentOutOfRangeException(
                    $"{nameof(month)} must be in range [1 - 12]");

            Year = year;
            Month = month;
        }

        public CalendarMonth Next()
            => AddMonths(1);

        public CalendarMonth Previous()
            => AddMonths(-1);

        public CalendarMonth AddMonths(int months)
        {
            var yearsToAdd = months / 12;
            var monthsToAdd = months % 12;

            var newMonth = (Month + monthsToAdd);
            if (newMonth > 12)
            {
                newMonth -= 12;
                yearsToAdd++;
            }

            if (newMonth < 1)
            {
                newMonth += 12;
                yearsToAdd--;
            }
            
            return new CalendarMonth(Year + yearsToAdd, newMonth);
        }

        public DayOfWeek DayOfWeek(int date)
            => new DateTime(Year, Month, date).DayOfWeek;

        public static bool operator ==(CalendarMonth m1, CalendarMonth m2)
            => m1.Year == m2.Year &&
               m1.Month == m2.Month;

        public static bool operator !=(CalendarMonth m1, CalendarMonth m2)
            => !(m1 == m2);

        public override bool Equals(object obj)
        {
            if (obj is CalendarMonth calendarMonth)
                return this == calendarMonth;

            return false;
        }

        public override int GetHashCode()
            => HashCode.From(Year, Month);
    }
}
