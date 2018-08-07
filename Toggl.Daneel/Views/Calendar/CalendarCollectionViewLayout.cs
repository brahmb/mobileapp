using System;
using System.Collections.Generic;
using System.Linq;
using CoreGraphics;
using CoreText;
using Foundation;
using UIKit;

namespace Toggl.Daneel.Views.Calendar
{
    public interface CalendarCollectionViewLayouDataSource
    {
        IEnumerable<NSIndexPath> IndexPathsOfCalendarItemsBetweenHours(int minHour, int maxHour);

        CalendarCollectionViewItemLayoutAttributes LayoutAttributesForItemAtIndexPath(NSIndexPath indexPath);
    }

    public sealed class CalendarCollectionViewLayout : UICollectionViewLayout
    {
        private const int hoursPerDay = 24;

        private static readonly nfloat hourHeight = 56;
        private static readonly nfloat minItemHeight = 20;
        private static readonly nfloat leftPadding = 76;
        private static readonly nfloat rightPadding = 16;

        public CalendarCollectionViewLayouDataSource DataSource { get; set; }

        public override CGSize CollectionViewContentSize
        {
            get
            {
                var width = CollectionView.Bounds.Width;
                var height = hoursPerDay * hourHeight;
                return new CGSize(width, height);
            }
        }

        public override bool ShouldInvalidateLayoutForBoundsChange(CGRect newBounds)
            => true;

        public override UICollectionViewLayoutAttributes[] LayoutAttributesForElementsInRect(CGRect rect)
        {
            var eventsIndexPaths = indexPathsForVisibleItemsInRect(rect);
            var itemsAttributes = eventsIndexPaths.Select(LayoutAttributesForItem);
            return itemsAttributes.ToArray();
        }

        public override UICollectionViewLayoutAttributes LayoutAttributesForItem(NSIndexPath indexPath)
        {
            var calendarItemLayoutAttributes = DataSource.LayoutAttributesForItemAtIndexPath(indexPath);

            var attributes = UICollectionViewLayoutAttributes.CreateForCell(indexPath);
            attributes.Frame = frameForItemWithLayoutAttributes(calendarItemLayoutAttributes);
            attributes.ZIndex = 1;

            return attributes;
        }

        private IEnumerable<NSIndexPath> indexPathsForVisibleItemsInRect(CGRect rect)
        {
            var minHour = (int)Math.Floor(rect.GetMinY() / hourHeight);
            var maxHour = (int)Math.Floor(rect.GetMaxY() / hourHeight);

            return DataSource.IndexPathsOfCalendarItemsBetweenHours(minHour, maxHour);
        }

        private CGRect frameForItemWithLayoutAttributes(CalendarCollectionViewItemLayoutAttributes attrs)
        {
            var yHour = hourHeight * attrs.StartTime.Hour;
            var yMins = hourHeight * attrs.StartTime.Minute / 60;

            var width = (CollectionViewContentSize.Width - leftPadding - rightPadding) / attrs.OverlappingItemsCount;
            var height = Math.Max(minItemHeight, hourHeight * attrs.Duration.Minutes / 60);
            var x = leftPadding + width * attrs.PositionInOverlappingGroup;
            var y = yHour + yMins;

            return new CGRect(x, y, width, height);
        }
    }
}
