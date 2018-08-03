using System;
using Toggl.Foundation.Analytics;

namespace Toggl.Foundation.MvvmCross.Parameters
{
    public sealed class ReportsDateRangeParameter
    {
        public DateTimeOffset StartDate { get; set; }

        public DateTimeOffset EndDate { get; set; }

        public ReportsSource Source { get; set; }

        public static ReportsDateRangeParameter WithDates(
            DateTimeOffset start,
            DateTimeOffset end
        )
        {
            if (start > end)
                (start, end) = (end, start);

            return new ReportsDateRangeParameter { StartDate = start, EndDate = end, Source = ReportsSource.Other };
        }

        public ReportsDateRangeParameter WithSource(ReportsSource source)
        {
            return new ReportsDateRangeParameter { StartDate = this.StartDate, EndDate = this.EndDate, Source = source };
        }
    }
}
