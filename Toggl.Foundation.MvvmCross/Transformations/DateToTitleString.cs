using System;
using System.Globalization;

namespace Toggl.Foundation.MvvmCross.Transformations
{
    public sealed class DateToTitleString
    {
        public static string Convert(DateTimeOffset offset, CultureInfo cultureInfo = null)
        {
            if (offset.ToLocalTime().Date == DateTimeOffset.Now.Date)
                return Resources.Today;

            if (offset.ToLocalTime().Date.AddDays(1) == DateTimeOffset.Now.Date)
                return Resources.Yesterday;

            return offset.ToString("ddd, dd MMM", cultureInfo ?? CultureInfo.CreateSpecificCulture("en-US"));
        }
    }
}
