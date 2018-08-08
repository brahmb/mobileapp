using System;
using CoreGraphics;
using Foundation;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Ios.Binding;
using MvvmCross.Platforms.Ios.Views;
using MvvmCross.Platforms.Ios.Presenters.Attributes;
using Toggl.Daneel.ViewSources;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac.Extensions;
using UIKit;
using static Toggl.Daneel.Extensions.AnimationExtensions;
using Toggl.Daneel.Converters;
using Toggl.Foundation.Models.Interfaces;
using System.Collections.Generic;

namespace Toggl.Daneel.ViewControllers
{
    [MvxChildPresentation]
    public sealed partial class ReportsViewController : MvxViewController<ReportsViewModel>
    {
        private const string boundsKey = "bounds";

        private const double maximumWorkspaceNameLabelWidth = 144;

        private nfloat calendarHeight => CalendarContainer.Bounds.Height;

        private UIButton titleButton;

        private ReportsTableViewSource source;

        private IDisposable calendarSizeDisposable;

        internal UIView CalendarContainerView => CalendarContainer;

        internal bool CalendarIsVisible { get; private set; }

        public ReportsViewController() : base(nameof(ReportsViewController), null)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            prepareViews();

            calendarSizeDisposable = CalendarContainer.AddObserver(boundsKey, NSKeyValueObservingOptions.New, onCalendarSizeChanged);

            source = new ReportsTableViewSource(ReportsTableView);
            source.OnScroll += onReportsTableScrolled;
            ReportsTableView.Source = source;

            var areThereEnoughWorkspaces = new LambdaConverter<IDictionary<string, IThreadSafeWorkspace>, bool>(workspaces => workspaces.Count > 1);
            var isWorkspaceNameTooLong = new LambdaConverter<string, bool>(workspaceName =>
            {
                var attributes = new UIStringAttributes { Font = WorkspaceLabel.Font };
                var size = new NSString(workspaceName).GetSizeUsingAttributes(attributes);
                return size.Width >= maximumWorkspaceNameLabelWidth;
            });

            var bindingSet = this.CreateBindingSet<ReportsViewController, ReportsViewModel>();

            bindingSet.Bind(source).To(vm => vm.Segments);
            bindingSet.Bind(titleButton).To(vm => vm.ToggleCalendarCommand);
            bindingSet.Bind(titleButton)
                      .For(v => v.BindTitle())
                      .To(vm => vm.CurrentDateRangeString);

            bindingSet.Bind(source)
                      .For(v => v.ViewModel)
                      .To(vm => vm);

            bindingSet.Bind(ReportsTableView)
                      .For(v => v.BindTap())
                      .To(vm => vm.HideCalendarCommand);

            bindingSet.Bind(WorkspaceButton)
                      .For(v => v.BindVisible())
                      .To(vm => vm.Workspaces)
                      .WithConversion(areThereEnoughWorkspaces);

            bindingSet.Bind(WorkspaceButton)
                      .For(v => v.BindTap())
                      .To(vm => vm.SelectWorkspace);

            bindingSet.Bind(WorkspaceLabel)
                      .To(vm => vm.WorkspaceName);

            bindingSet.Bind(WorkspaceFadeView)
                      .For(v => v.FadeRight)
                      .To(vm => vm.WorkspaceName)
                      .WithConversion(isWorkspaceNameTooLong);

            bindingSet.Apply();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            source.OnScroll -= onReportsTableScrolled;

            calendarSizeDisposable?.Dispose();
            calendarSizeDisposable = null;
        }

        private void onReportsTableScrolled(object sender, CGPoint offset)
        {
            if (CalendarIsVisible)
            {
                var topConstant = (TopCalendarConstraint.Constant + offset.Y).Clamp(0, calendarHeight);
                TopCalendarConstraint.Constant = topConstant;

                if (topConstant == 0) return;

                // we need to adjust the offset of the scroll view so that it doesn't fold
                // under the calendar while scrolling up
                var adjustedOffset = new CGPoint(offset.X, ReportsTableView.ContentOffset.Y - offset.Y);
                ReportsTableView.SetContentOffset(adjustedOffset, false);
                View.LayoutIfNeeded();

                if (topConstant == calendarHeight)
                {
                    HideCalendar();
                }
            }
        }

        internal void ShowCalendar()
        {
            TopCalendarConstraint.Constant = 0;
            Animate(
                Animation.Timings.EnterTiming,
                Animation.Curves.SharpCurve,
                () => View.LayoutSubviews(),
                () => CalendarIsVisible = true);
        }

        internal void HideCalendar()
        {
            TopCalendarConstraint.Constant = calendarHeight;
            Animate(
                Animation.Timings.EnterTiming,
                Animation.Curves.SharpCurve,
                () => View.LayoutSubviews(),
                () => CalendarIsVisible = false);
        }

        private void prepareViews()
        {
            // Title view
            NavigationItem.TitleView = titleButton = new UIButton(new CGRect(0, 0, 200, 40));
            titleButton.Font = UIFont.SystemFontOfSize(14, UIFontWeight.Medium);
            titleButton.SetTitleColor(UIColor.Black, UIControlState.Normal);

            // Calendar configuration
            TopCalendarConstraint.Constant = calendarHeight;

            // Workspace button settings
            WorkspaceFadeView.FadeWidth = 32;
            WorkspaceButton.Layer.ShadowColor = UIColor.Black.CGColor;
            WorkspaceButton.Layer.ShadowRadius = 10;
            WorkspaceButton.Layer.ShadowOffset = new CGSize(0, 2);
            WorkspaceButton.Layer.ShadowOpacity = 0.10f;
        }

        private void onCalendarSizeChanged(NSObservedChange change)
        {
            if (CalendarIsVisible)
                TopCalendarConstraint.Constant = 0;
            else
                TopCalendarConstraint.Constant = calendarHeight;
        }
    }
}

