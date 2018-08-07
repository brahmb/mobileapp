using System;
using System.Collections.Generic;
using Foundation;
using MvvmCross.Commands;
using MvvmCross.Platforms.Ios.Binding.Views;
using Toggl.Daneel.Views;
using Toggl.Foundation.MvvmCross.ViewModels.ReportsCalendar;
using UIKit;

namespace Toggl.Daneel.ViewSources
{
    public sealed class ReportsCalendarCollectionViewSource : MvxCollectionViewSource
    {
        private const string cellIdentifier = nameof(ReportsCalendarViewCell);

        private List<ReportsCalendarPageViewModel> months
        => (List<ReportsCalendarPageViewModel>)ItemsSource;

        public IMvxCommand<ReportsCalendarDayViewModel> CellTappedCommand { get; set; }

        public ReportsCalendarCollectionViewSource(UICollectionView collectionView)
            : base(collectionView)
        {
            collectionView.RegisterNibForCell(ReportsCalendarViewCell.Nib, cellIdentifier);
        }

        public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
        {
            var item = GetItemAt(indexPath);
            var cell = GetOrCreateCellFor(collectionView, indexPath, item);

            if (cell is ReportsCalendarViewCell calendarCell)
            {
                calendarCell.DataContext = item;
                calendarCell.CellTappedCommand = CellTappedCommand;
            }

            return cell;
        }

        protected override UICollectionViewCell GetOrCreateCellFor(UICollectionView collectionView, NSIndexPath indexPath, object item)
            => collectionView.DequeueReusableCell(cellIdentifier, indexPath) as UICollectionViewCell;

        public override nint NumberOfSections(UICollectionView collectionView)
            => months.Count;

        public override nint GetItemsCount(UICollectionView collectionView, nint section)
            => months[(int)section].Days.Count;

        protected override object GetItemAt(NSIndexPath indexPath)
            => months[indexPath.Section].Days[(int)indexPath.Item];
    }
}
