using System;
using System.Linq;
using Foundation;
using MvvmCross.Platforms.Ios.Presenters.Attributes;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;
using Toggl.Daneel.Views.Calendar;
using System.Collections.Generic;
using MvvmCross;
using Toggl.Foundation;

namespace Toggl.Daneel.ViewControllers
{
    [MvxChildPresentation]
    public sealed partial class CalendarViewController : ReactiveViewController<CalendarViewModel>, IUICollectionViewDataSource, ICalendarCollectionViewLayoutDataSource
    {
        List<CalendarCollectionViewItemLayoutAttributes> layoutAttributes;

        public CalendarViewController() : base(null)
        {
            layoutAttributes = new List<CalendarCollectionViewItemLayoutAttributes>
            {
                new CalendarCollectionViewItemLayoutAttributes(new DateTime(2018, 08, 08, 08, 00, 00), TimeSpan.FromMinutes(30), 1, 0),
                new CalendarCollectionViewItemLayoutAttributes(new DateTime(2018, 08, 08, 08, 40, 00), TimeSpan.FromMinutes(30), 2, 0),
                new CalendarCollectionViewItemLayoutAttributes(new DateTime(2018, 08, 08, 08, 50, 00), TimeSpan.FromMinutes(30), 2, 1),
                new CalendarCollectionViewItemLayoutAttributes(new DateTime(2018, 08, 08, 10, 30, 00), TimeSpan.FromMinutes(30), 3, 0),
                new CalendarCollectionViewItemLayoutAttributes(new DateTime(2018, 08, 08, 10, 45, 00), TimeSpan.FromMinutes(30), 3, 1),
                new CalendarCollectionViewItemLayoutAttributes(new DateTime(2018, 08, 08, 11, 00, 00), TimeSpan.FromMinutes(30), 3, 2),

                new CalendarCollectionViewItemLayoutAttributes(new DateTime(2018, 08, 08, 12, 00, 00), TimeSpan.FromMinutes(30), 1, 0),
                new CalendarCollectionViewItemLayoutAttributes(new DateTime(2018, 08, 08, 12, 40, 00), TimeSpan.FromMinutes(30), 2, 0),
                new CalendarCollectionViewItemLayoutAttributes(new DateTime(2018, 08, 08, 12, 50, 00), TimeSpan.FromMinutes(30), 2, 1),
                new CalendarCollectionViewItemLayoutAttributes(new DateTime(2018, 08, 08, 14, 30, 00), TimeSpan.FromMinutes(30), 3, 0),
                new CalendarCollectionViewItemLayoutAttributes(new DateTime(2018, 08, 08, 14, 45, 00), TimeSpan.FromMinutes(30), 3, 1),
                new CalendarCollectionViewItemLayoutAttributes(new DateTime(2018, 08, 08, 15, 00, 00), TimeSpan.FromMinutes(30), 3, 2),

                new CalendarCollectionViewItemLayoutAttributes(new DateTime(2018, 08, 08, 16, 00, 00), TimeSpan.FromMinutes(30), 1, 0),
                new CalendarCollectionViewItemLayoutAttributes(new DateTime(2018, 08, 08, 16, 40, 00), TimeSpan.FromMinutes(30), 2, 0),
                new CalendarCollectionViewItemLayoutAttributes(new DateTime(2018, 08, 08, 16, 50, 00), TimeSpan.FromMinutes(30), 2, 1),
                new CalendarCollectionViewItemLayoutAttributes(new DateTime(2018, 08, 08, 18, 30, 00), TimeSpan.FromMinutes(30), 3, 0),
                new CalendarCollectionViewItemLayoutAttributes(new DateTime(2018, 08, 08, 18, 45, 00), TimeSpan.FromMinutes(30), 3, 1),
                new CalendarCollectionViewItemLayoutAttributes(new DateTime(2018, 08, 08, 19, 00, 00), TimeSpan.FromMinutes(30), 3, 2),
            };
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var timeService = Mvx.Resolve<ITimeService>();
            var layout = new CalendarCollectionViewLayout(timeService, this);

            CalendarCollectionView.RegisterNibForCell(CalendarItemView.Nib, "item");
            CalendarCollectionView.RegisterNibForSupplementaryView(HourSupplementaryView.Nib,
                                                                   CalendarCollectionViewLayout.HourSupplementaryViewKind,
                                                                   new NSString("hours"));
            CalendarCollectionView.RegisterNibForSupplementaryView(CurrentTimeSupplementaryView.Nib,
                                                                   CalendarCollectionViewLayout.CurrentTimeSupplementaryViewKind,
                                                                   new NSString("currentTime"));

            CalendarCollectionView.SetCollectionViewLayout(layout, false);

            CalendarCollectionView.DataSource = this;
        }

        public UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
        {
            var cell = collectionView.DequeueReusableCell("item", indexPath) as CalendarItemView;
            return cell;
        }

        [Export("collectionView:viewForSupplementaryElementOfKind:atIndexPath:")]
        public UICollectionReusableView GetViewForSupplementaryElement(UICollectionView collectionView, NSString elementKind, NSIndexPath indexPath)
        {
            if (elementKind == CalendarCollectionViewLayout.HourSupplementaryViewKind)
            {
                var reusableView = collectionView.DequeueReusableSupplementaryView(elementKind, "hours", indexPath);
                // TODO: Set hour label
                return reusableView;
            }
            return collectionView.DequeueReusableSupplementaryView(elementKind, "currentTime", indexPath);
        }

        public nint GetItemsCount(UICollectionView collectionView, nint section)
        {
            return layoutAttributes.Count;
        }

        public IEnumerable<NSIndexPath> IndexPathsOfCalendarItemsBetweenHours(int minHour, int maxHour)
        {
            var indices = layoutAttributes
                .Select((value, index) => new { value, index })
                .Where(t => t.value.StartTime.Hour >= minHour && t.value.EndTime.Hour <= maxHour)
                .Select(t => t.index);

            return indices.Select(index => NSIndexPath.FromItemSection(index, 0));
        }

        public CalendarCollectionViewItemLayoutAttributes LayoutAttributesForItemAtIndexPath(NSIndexPath indexPath)
        {
            return layoutAttributes[(int)(indexPath.Item)];
        }
    }
}
