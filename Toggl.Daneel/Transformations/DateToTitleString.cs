using System;
using System.Globalization;

namespace Toggl.Daneel.Transformations
{
    public class DateToTitleString
    {
        private static readonly CultureInfo cultureInfo = CultureInfo.CreateSpecificCulture("en-US");

        public static string Convert(DateTime date)
        {
            return date.ToString("ddd, dd MMM", cultureInfo);
        }
    }
}
