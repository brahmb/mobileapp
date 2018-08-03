using System;
using System.Reactive;
using System.Reactive.Disposables;
using Foundation;
using MvvmCross.Plugin.Color.Platforms.Ios;
using MvvmCross.UI;
using Toggl.Daneel.Cells;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Transformations;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.Transformations;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;

namespace Toggl.Daneel.Views
{
    public partial class TimeEntriesLogViewCell : BaseTableViewCell<TimeEntryViewModel>
    {
        public static readonly string Identifier = "timeEntryCell";

        private const float noProjectDistance = 24;
        private const float hasProjectDistance = 14;

        private const float hasTagsBillableSpacing = 37;
        private const float noTagsBillableSpacing = 10;

        private ProjectTaskClientToAttributedString projectTaskClientToAttributedString;

        public static readonly NSString Key = new NSString(nameof(TimeEntriesLogViewCell));
        public static readonly UINib Nib;

        public CompositeDisposable DisposeBag = new CompositeDisposable();

        public IObservable<Unit> ContinueButtonTap
            => ContinueButton.Tapped();

        static TimeEntriesLogViewCell()
        {
            Nib = UINib.FromName(nameof(TimeEntriesLogViewCell), NSBundle.MainBundle);
        }

        protected TimeEntriesLogViewCell(IntPtr handle)
            : base(handle)
        {
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            FadeView.FadeRight = true;

            TimeLabel.Font = TimeLabel.Font.GetMonospacedDigitFont();

            projectTaskClientToAttributedString = new ProjectTaskClientToAttributedString(
                ProjectTaskClientLabel.Font.CapHeight,
                Color.TimeEntriesLog.ClientColor.ToNativeColor(),
                true
            );
        }

        public override void PrepareForReuse()
        {
            base.PrepareForReuse();
            DisposeBag.Dispose();
            DisposeBag = new CompositeDisposable();
        }

        protected override void UpdateView()
        {
            // Text
            var projectColor = MvxColor.ParseHexString(Item.ProjectColor).ToNativeColor();
            DescriptionLabel.Text = Item.Description;
            ProjectTaskClientLabel.AttributedText = projectTaskClientToAttributedString.Convert(Item.ProjectName, Item.TaskName, Item.ClientName, projectColor);
            TimeLabel.Text = Item.Duration.HasValue
                ? DurationAndFormatToString.Convert(Item.Duration.Value, Item.DurationFormat)
                : "";

            // Constraints
            DescriptionTopDistanceConstraint.Constant = Item.HasProject ? hasProjectDistance : noProjectDistance;
            AddDescriptionTopDistanceConstraint.Constant = Item.HasProject ? hasProjectDistance : noProjectDistance;
            FadeViewTrailingConstraint.Constant = calculateFadeViewConstraint(Item.IsBillable, Item.HasTags);
            BillableImageViewSpacingConstraint.Constant = Item.HasTags ? hasTagsBillableSpacing : noTagsBillableSpacing;

            // Visibility
            ProjectTaskClientLabel.Hidden = !Item.HasProject;
            AddDescriptionLabel.Hidden = Item.HasDescription;
            SyncErrorImageView.Hidden = Item.CanSync;
            UnsyncedImageView.Hidden = !Item.NeedsSync;
            ContinueButton.Hidden = !Item.CanSync;
            ContinueImageView.Hidden = !Item.CanSync;
            BillableImageView.Hidden = !Item.IsBillable;
            TagsImageView.Hidden = !Item.HasTags;
        }

        private nfloat calculateFadeViewConstraint(bool isBillable, bool hasTags)
        {
            if (isBillable && hasTags)
            {
                return 136;
            }

            if (isBillable || hasTags)
            {
                return 112;
            }

            return 96;
        }

        protected override void Dispose(bool disposing)
        {
            DisposeBag.Dispose();
            base.Dispose(disposing);
        }
    }
}
