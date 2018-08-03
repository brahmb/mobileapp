using System;
using Toggl.Foundation.Extensions;
using Toggl.Multivac;

namespace Toggl.Foundation.MvvmCross.Transformations
{
    public class DurationAndFormatToString
    {
        public static string Convert(TimeSpan duration, DurationFormat format)
        {
            return duration.ToFormattedString(format);
        }
    }
}
