using System;
using System.Collections.Generic;
using System.Linq;
using CoreGraphics;
using Foundation;
using Toggl.Foundation;
using Toggl.Multivac;
using UIKit;
using Math = System.Math;

namespace Toggl.Daneel.Views.Calendar
{
    public sealed class CalendarCollectionViewLayout : UICollectionViewLayout
    {
        private const int hoursPerDay = 24;

        private static readonly nfloat hourHeight = 56;
        private static readonly nfloat minItemHeight = hourHeight / 4;
        private static readonly nfloat leftPadding = 76;
        private static readonly nfloat rightPadding = 16;
        private static readonly nfloat hourSupplementaryLabelHeight = 20;
        private static readonly nfloat currentTimeSupplementaryLeftOffset = -18;

        private ITimeService timeService { get; }
        private ICalendarCollectionViewLayoutDataSource dataSource { get; }

        public static NSString HourSupplementaryViewKind = new NSString("Hour");
        public static NSString CurrentTimeSupplementaryViewKind = new NSString("CurrentTime");

        public CalendarCollectionViewLayout(ITimeService timeService, ICalendarCollectionViewLayoutDataSource dataSource) : base()
        {
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));

            this.timeService = timeService;
            this.dataSource = dataSource;
        }

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

            var hoursIndexPaths = indexPathsForHoursInRect(rect);
            var hoursAttributes = hoursIndexPaths.Select(layoutAttributesForHourView);

            var currentTimeAttributes = layoutAttributesForCurrentTime();

            var attributes = itemsAttributes
                .Concat(hoursAttributes)
                .Append(currentTimeAttributes);

            return attributes.ToArray();
        }

        public override UICollectionViewLayoutAttributes LayoutAttributesForItem(NSIndexPath indexPath)
        {
            var calendarItemLayoutAttributes = dataSource.LayoutAttributesForItemAtIndexPath(indexPath);

            var attributes = UICollectionViewLayoutAttributes.CreateForCell(indexPath);
            attributes.Frame = frameForItemWithLayoutAttributes(calendarItemLayoutAttributes);
            attributes.ZIndex = 1;

            return attributes;
        }

        public override UICollectionViewLayoutAttributes LayoutAttributesForSupplementaryView(NSString kind, NSIndexPath indexPath)
        {
            if (kind == HourSupplementaryViewKind)
            {
                var attributes = UICollectionViewLayoutAttributes.CreateForSupplementaryView(kind, indexPath);
                attributes.Frame = frameForHour((int)indexPath.Item);
                attributes.ZIndex = 0;
                return attributes;
            }
            else {
                var attributes = UICollectionViewLayoutAttributes.CreateForSupplementaryView(kind, indexPath);
                attributes.Frame = frameForCurrentTime();
                attributes.ZIndex = 2;
                return attributes;
            }
        }

        private UICollectionViewLayoutAttributes layoutAttributesForHourView(NSIndexPath indexPath)
            => LayoutAttributesForSupplementaryView(HourSupplementaryViewKind, indexPath);

        private UICollectionViewLayoutAttributes layoutAttributesForCurrentTime()
            => LayoutAttributesForSupplementaryView(CurrentTimeSupplementaryViewKind, NSIndexPath.FromItemSection(0, 0));

        private IEnumerable<NSIndexPath> indexPathsForVisibleItemsInRect(CGRect rect)
        {
            var minHour = (int)Math.Floor(rect.GetMinY() / hourHeight);
            var maxHour = (int)Math.Floor(rect.GetMaxY() / hourHeight);

            return dataSource.IndexPathsOfCalendarItemsBetweenHours(minHour, maxHour);
        }

        private IEnumerable<NSIndexPath> indexPathsForHoursInRect(CGRect rect)
        {
            var minHour = (int)Math.Max(0, Math.Floor(rect.GetMinY() / hourHeight));
            var maxHour = (int)Math.Min(24, Math.Floor(rect.GetMaxY() / hourHeight));

            return Enumerable
                .Range(minHour, maxHour)
                .Select(hour => NSIndexPath.FromItemSection(hour, 0))
                .ToArray();
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

        private CGRect frameForHour(int hour)
        {
            var width = CollectionViewContentSize.Width - rightPadding;
            var height = hourSupplementaryLabelHeight;
            var x = 0;
            var y = hourHeight * hour - height / 2;

            return new CGRect(x, y, width, height);
        }

        private CGRect frameForCurrentTime()
        {
            var now = timeService.CurrentDateTime;

            var yHour = hourHeight * now.Hour;
            var yMins = hourHeight * now.Minute / 60;

            var width = CollectionViewContentSize.Width - leftPadding - rightPadding - currentTimeSupplementaryLeftOffset;
            var height = 8;
            var x = leftPadding + currentTimeSupplementaryLeftOffset;
            var y = yHour + yMins - height / 2;

            return new CGRect(x, y, width, height);
        }
    }
}
