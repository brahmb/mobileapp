using System;
using MvvmCross.Platforms.Ios.Presenters.Attributes;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Daneel.Extensions;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac.Extensions;
using UIKit;
using Math = System.Math;

namespace Toggl.Daneel.ViewControllers
{
    [MvxChildPresentation]
    public partial class SettingsViewController : ReactiveViewController<SettingsViewModel>
    {
        private const int verticalSpacing = 24;

        public SettingsViewController()
            : base(nameof(SettingsViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            prepareViews();

            Title = ViewModel.Title;
            VersionLabel.Text = ViewModel.Version;

            LoggingOutView.Hidden = true;
            SendFeedbackSuccessView.Hidden = true;

            this.Bind(ViewModel.Email, EmailLabel.BindText());
            this.Bind(ViewModel.IsSynced, SyncedView.BindIsVisible());
            this.Bind(ViewModel.WorkspaceName, WorkspaceLabel.BindText());
            this.Bind(ViewModel.DurationFormat, DurationFormatLabel.BindText());
            this.Bind(ViewModel.IsRunningSync, SyncingView.BindIsVisible());
            this.Bind(ViewModel.DateFormat, DateFormatLabel.BindText());
            this.Bind(ViewModel.IsManualModeEnabled, ManualModeSwitch.BindIsOn());
            this.Bind(ViewModel.BeginningOfWeek, BeginningOfWeekLabel.BindText());
            this.Bind(ViewModel.UseTwentyFourHourFormat, TwentyFourHourClockSwitch.BindIsOn());
            this.BindVoid(ViewModel.LoggingOut, () =>
            {
                LoggingOutView.Hidden = false;
                SyncingView.Hidden = true;
                SyncedView.Hidden = true;
            });
            this.Bind(ViewModel.IsFeedbackSuccessViewShowing, SendFeedbackSuccessView.BindAnimatedIsVisible());

            this.Bind(HelpView.Tapped(), ViewModel.OpenHelpView);
            this.Bind(LogoutButton.Tapped(), ViewModel.TryLogout);
            this.Bind(AboutView.Tapped(), ViewModel.OpenAboutView);
            this.Bind(FeedbackView.Tapped(), ViewModel.SubmitFeedback);
            this.Bind(DateFormatView.Tapped(), ViewModel.SelectDateFormat);
            this.Bind(WorkspaceView.Tapped(), ViewModel.PickDefaultWorkspace);
            this.BindVoid(ManualModeView.Tapped(), ViewModel.ToggleManualMode);
            this.Bind(DurationFormatView.Tapped(), ViewModel.SelectDurationFormat);
            this.Bind(BeginningOfWeekView.Tapped(), ViewModel.SelectBeginningOfWeek);
            this.Bind(TwentyFourHourClockView.Tapped(), ViewModel.ToggleUseTwentyFourHourClock);
            this.BindVoid(SendFeedbackSuccessView.Tapped(), ViewModel.CloseFeedbackSuccessView);

            UIApplication.Notifications
                .ObserveWillEnterForeground((sender, e) => startAnimations())
                .DisposedBy(DisposeBag);
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            startAnimations();
        }

        public override void ViewWillLayoutSubviews()
        {
            base.ViewWillLayoutSubviews();
            tryAlignLogoutButtonWithBottomEdge();
        }

        private void prepareViews()
        {
            // Syncing indicator colors
            setIndicatorSyncColor(SyncedIcon);
            setIndicatorSyncColor(SyncingIndicator);
            setIndicatorSyncColor(LoggingOutIndicator);

            // Resize Switches
            TwentyFourHourClockSwitch.Resize();
            ManualModeSwitch.Resize();
        }

        private void setIndicatorSyncColor(UIImageView imageView)
        {
            imageView.Image = imageView.Image.ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
            imageView.TintColor = Color.Settings.SyncStatusText.ToNativeColor();
        }

        private void startAnimations()
        {
            SyncingActivityIndicatorView.StartAnimation();
            LoggingOutActivityIndicatorView.StartAnimation();
        }

        private void tryAlignLogoutButtonWithBottomEdge()
        {
            var contentHeight = LogoutContainerView.Frame.Top - LogoutVerticalOffsetConstraint.Constant + LogoutContainerView.Frame.Height;
            var bottomOffset = verticalSpacing;
            var idealDistance = ScrollView.Frame.Height - contentHeight - bottomOffset;
            var distance = Math.Max(idealDistance, verticalSpacing);
            LogoutVerticalOffsetConstraint.Constant = (nfloat)distance;
        }
    }
}
