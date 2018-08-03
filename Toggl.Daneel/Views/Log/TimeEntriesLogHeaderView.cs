using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Transformations;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;

namespace Toggl.Daneel.Views
{
    public partial class TimeEntriesLogHeaderView : UITableViewHeaderFooterView
    {
        public static readonly string Identifier = "timeEntryLogHeaderCell";


        public static readonly NSString Key = new NSString(nameof(TimeEntriesLogHeaderView));
        public static readonly UINib Nib;

        private IReadOnlyList<TimeEntryViewModel> items;
        public IReadOnlyList<TimeEntryViewModel> Items
        {
            get => items;
            set
            {
                items = value;
                UpdateView();
            }
        }

        static TimeEntriesLogHeaderView()
        {
            Nib = UINib.FromName(nameof(TimeEntriesLogHeaderView), NSBundle.MainBundle);
        }

        protected TimeEntriesLogHeaderView(IntPtr handle)
            : base(handle)
        {
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            ContentView.BackgroundColor = UIColor.White;
            DurationLabel.Font = DurationLabel.Font.GetMonospacedDigitFont();
        }

        public void UpdateView()
        {
            if (items.Count == 0)
                return;

            var firstItem = items.First();

            DateLabel.Text = DateToTitleString.Convert(firstItem.StartTime.Date);

            var totalDuration = items.Aggregate(TimeSpan.Zero, (acc, vm) => acc + (vm.Duration ?? TimeSpan.Zero));
            DurationLabel.Text = totalDuration.ToFormattedString(firstItem.DurationFormat);
        }
    }
}
