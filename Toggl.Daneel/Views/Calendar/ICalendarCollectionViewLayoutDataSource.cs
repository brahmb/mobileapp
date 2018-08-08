using System;
using System.Collections.Generic;
using Foundation;

namespace Toggl.Daneel.Views.Calendar
{
	public interface ICalendarCollectionViewLayoutDataSource
    {
        IEnumerable<NSIndexPath> IndexPathsOfCalendarItemsBetweenHours(int minHour, int maxHour);

        CalendarCollectionViewItemLayoutAttributes LayoutAttributesForItemAtIndexPath(NSIndexPath indexPath);
    }
}
