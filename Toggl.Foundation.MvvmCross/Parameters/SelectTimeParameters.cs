using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Foundation;
using Toggl.Multivac;
using static Toggl.Foundation.Helper.Constants;

namespace Toggl.Foundation.MvvmCross.Parameters
{
    [Preserve(AllMembers = true)]
    public sealed class SelectTimeParameters
    {
        public enum Origin
        {
            StartTime,
            StartDate,
            StopTime,
            StopDate,
            Duration
        }

        public DateTimeOffset Start { get; private set; }
        public DateTimeOffset? Stop { get; private set; }

        public DateFormat DateFormat { get; private set; }
        public TimeFormat TimeFormat { get; private set; }

        public int StartingTabIndex { get; private set; }
        public bool ShouldStartOnCalendar { get; private set; }

        public static SelectTimeParameters CreateFromOrigin(Origin origin, DateTimeOffset start, DateTimeOffset? stop = null)
        {
            var allowedParameters = new Dictionary<Origin, (int, bool)>
            {
                [Origin.StartTime] = (0, false),
                [Origin.StartDate] = (0, true),
                [Origin.StopTime] = (1, false),
                [Origin.StopDate] = (1, true),
                [Origin.Duration] = (2, false)
            };

            if (!allowedParameters.Keys.Contains(origin))
                throw new ArgumentException("SelectTimeCommand binding must have one of the following (case sensitive) parameters: StartTime|StartDate|StopTime|StopDate|Duration");

            var (tabIndex, shouldStartOnCalendar) = allowedParameters[origin];

            return new SelectTimeParameters
            {
                Start = start,
                Stop = stop,
                StartingTabIndex = tabIndex,
                ShouldStartOnCalendar = shouldStartOnCalendar
            };
        }

        public SelectTimeParameters WithFormats(DateFormat dateFormat, TimeFormat timeFormat) 
        {
            DateFormat = dateFormat;
            TimeFormat = timeFormat;
            return this;
        }
    }
}
