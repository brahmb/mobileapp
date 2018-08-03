using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using CoreGraphics;
using Foundation;
using MvvmCross.Commands;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Daneel.Views;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.Sync;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using UIKit;
using static Toggl.Daneel.Extensions.TextExtensions;

namespace Toggl.Daneel.ViewSources
{
    public sealed class RefreshControl
    {
        private CompositeDisposable disposeBag = new CompositeDisposable();

        private const float syncBarHeight = 26;
        private const float activityIndicatorSize = 14;
        private const float syncLabelFontSize = 12;

        private static readonly float scrollThreshold = 3 * syncBarHeight;

        private readonly UIColor pullToRefreshColor = Color.Main.PullToRefresh.ToNativeColor();
        private readonly UIColor syncingColor = Color.Main.Syncing.ToNativeColor();
        private readonly UIColor syncFailedColor = Color.Main.SyncFailed.ToNativeColor();
        private readonly UIColor offlineColor = Color.Main.Offline.ToNativeColor();
        private readonly UIColor syncCompletedColor = Color.Main.SyncCompleted.ToNativeColor();

        private bool wasReleased;
        private bool isSyncing = false;
        private bool needsRefresh;
        private bool shouldCalculateOnDeceleration;
        private bool shouldRefreshOnTap;

        private readonly UIView syncStateView = new UIView();
        private readonly UILabel syncStateLabel = new UILabel();
        private readonly UIButton dismissSyncBarButton = new UIButton();
        private readonly ActivityIndicatorView activityIndicatorView = new ActivityIndicatorView();

        private NSLayoutConstraint heightConstraint;

        private Subject<Unit> refreshSubject = new Subject<Unit>();
        public IObservable<Unit> Refresh
            => refreshSubject.AsObservable();

        private UIScrollView scrollView;

        public RefreshControl(IObservable<SyncProgress> syncProgress, IObservableScroll observableScroll)
        {
            syncProgress
                .Subscribe(syncProgressChanged)
                .DisposedBy(disposeBag);

            observableScroll.ScrollOffset
                .Subscribe(didScroll)
                .DisposedBy(disposeBag);

            observableScroll.IsDragging
                .Subscribe(draggingChanged)
                .DisposedBy(disposeBag);
        }

        public void Configure(UIScrollView scrollView)
        {
            if (scrollView.Superview == null)
            {
                throw new ArgumentException("RefreshControl cannot be added to UISCrollView not in view hierarchy.");
            }

            this.scrollView = scrollView;

            syncStateView.AddSubview(syncStateLabel);
            syncStateView.AddSubview(activityIndicatorView);
            syncStateView.AddSubview(dismissSyncBarButton);
            scrollView.Superview.AddSubview(syncStateView);

            prepareSyncStateView();
            prepareSyncStateLabel();
            prepareActivityIndicatorView();
            prepareDismissSyncBarImageView();
        }

        private void prepareSyncStateView()
        {
            syncStateView.BackgroundColor = pullToRefreshColor;
            syncStateView.TranslatesAutoresizingMaskIntoConstraints = false;
            syncStateView.TopAnchor.ConstraintEqualTo(scrollView.Superview.TopAnchor).Active = true;
            syncStateView.WidthAnchor.ConstraintEqualTo(scrollView.WidthAnchor).Active = true;
            syncStateView.CenterXAnchor.ConstraintEqualTo(scrollView.CenterXAnchor).Active = true;
            heightConstraint = syncStateView.HeightAnchor.ConstraintEqualTo(0);
            heightConstraint.Active = true;
        }

        private void prepareSyncStateLabel()
        {
            syncStateLabel.TextColor = UIColor.White;
            syncStateLabel.TranslatesAutoresizingMaskIntoConstraints = false;
            syncStateLabel.Font = syncStateLabel.Font.WithSize(syncLabelFontSize);
            syncStateLabel.CenterXAnchor.ConstraintEqualTo(syncStateView.CenterXAnchor).Active = true;
            syncStateLabel.BottomAnchor.ConstraintEqualTo(syncStateView.BottomAnchor, -6).Active = true;

            var tapGestureRecognizer = new UITapGestureRecognizer(onStatusLabelTap);
            syncStateLabel.UserInteractionEnabled = true;
            syncStateLabel.AddGestureRecognizer(tapGestureRecognizer);
        }

        private void prepareActivityIndicatorView()
        {
            activityIndicatorView.TranslatesAutoresizingMaskIntoConstraints = false;
            activityIndicatorView.WidthAnchor.ConstraintEqualTo(activityIndicatorSize).Active = true;
            activityIndicatorView.HeightAnchor.ConstraintEqualTo(activityIndicatorSize).Active = true;
            activityIndicatorView.CenterYAnchor.ConstraintEqualTo(syncStateLabel.CenterYAnchor).Active = true;
            activityIndicatorView.LeadingAnchor.ConstraintEqualTo(syncStateLabel.TrailingAnchor, 6).Active = true;
            activityIndicatorView.StartAnimation();
        }

        private void prepareDismissSyncBarImageView()
        {
            dismissSyncBarButton.Hidden = true;
            dismissSyncBarButton.SetImage(UIImage.FromBundle("icClose").ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate), UIControlState.Normal);
            dismissSyncBarButton.TintColor = UIColor.White;
            dismissSyncBarButton.TranslatesAutoresizingMaskIntoConstraints = false;
            dismissSyncBarButton.CenterYAnchor.ConstraintEqualTo(syncStateLabel.CenterYAnchor).Active = true;
            dismissSyncBarButton.TrailingAnchor.ConstraintEqualTo(syncStateView.TrailingAnchor, -16).Active = true;
            dismissSyncBarButton.TouchUpInside += onDismissSyncBarButtonTap;
        }

        private async void syncProgressChanged(SyncProgress syncProgress)
        {
            bool hideIndicator = false;
            shouldRefreshOnTap = false;
            isSyncing = false;

            switch (syncProgress)
            {
                case SyncProgress.Unknown:
                    return;

                case SyncProgress.Syncing:
                    setSyncIndicatorTextAndBackground(
                        new NSAttributedString(Resources.Syncing),
                        syncingColor);
                    setActivityIndicatorVisible(true);
                    isSyncing = true;
                    break;

                case SyncProgress.OfflineModeDetected:
                    setSyncIndicatorTextAndBackground(
                        Resources.Offline.EndingWithRefreshIcon(syncStateLabel.Font.CapHeight),
                        offlineColor);
                    dismissSyncBarButton.Hidden = false;
                    setActivityIndicatorVisible(false);
                    shouldRefreshOnTap = true;
                    break;

                case SyncProgress.Synced:
                    setSyncIndicatorTextAndBackground(
                        Resources.SyncCompleted.EndingWithTick(syncStateLabel.Font.CapHeight),
                        syncCompletedColor);
                    hideIndicator = true;
                    setActivityIndicatorVisible(false);
                    break;

                case SyncProgress.Failed:
                    setSyncIndicatorTextAndBackground(
                        Resources.SyncFailed.EndingWithRefreshIcon(syncStateLabel.Font.CapHeight),
                        syncFailedColor);
                    dismissSyncBarButton.Hidden = false;
                    setActivityIndicatorVisible(false);
                    break;

                default:
                    throw new ArgumentException(nameof(SyncProgress));
            }

            showSyncBar();

            if (!hideIndicator) return;

            await hideSyncBar();
        }

        private void didScroll(CGPoint offset)
        {
            if (!scrollView.Dragging || wasReleased) return;

            if (offset.Y >= 0) return;

            heightConstraint.Constant = -offset.Y;
            syncStateView.SetNeedsLayout();

            var needsMorePulling = System.Math.Abs(offset.Y) < scrollThreshold;
            needsRefresh = !needsMorePulling;

            if (isSyncing) return;

            dismissSyncBarButton.Hidden = true;
            setSyncIndicatorTextAndBackground(
                new NSAttributedString(needsMorePulling ? Resources.PullDownToRefresh : Resources.ReleaseToRefresh),
                pullToRefreshColor);
        }

        private void draggingChanged(bool isDragging)
        {
            if (isDragging)
            {
                wasReleased = false;
            }
            else
            {
                var offset = scrollView.ContentOffset.Y;
                if (offset >= 0) return;

                wasReleased = true;

                if (shouldCalculateOnDeceleration) return;
                refreshIfNeeded();
            }
        }

        private void refreshIfNeeded()
        {
            if (!needsRefresh)
            {
                hideSyncBar(false);
                return;
            }

            needsRefresh = false;
            shouldCalculateOnDeceleration = false;

            if (isSyncing)
            {
                showSyncBar();
                return;
            }

            refreshSubject.OnNext(Unit.Default);
        }

        private void setSyncIndicatorTextAndBackground(NSAttributedString text, UIColor backgroundColor)
        {
            UIView.Animate(Animation.Timings.EnterTiming, () =>
            {
                syncStateLabel.AttributedText = text;
                syncStateView.BackgroundColor = backgroundColor;
            });
        }

        private void showSyncBar()
        {
            if (scrollView.Dragging) return;
            if (scrollView.ContentOffset.Y > 0) return;

            scrollView.SetContentOffset(new CGPoint(0, -syncBarHeight), true);
            heightConstraint.Constant = syncBarHeight;
            UIView.Animate(Animation.Timings.EnterTiming, () =>
            {
                syncStateView.LayoutIfNeeded();
            });
        }

        private async Task hideSyncBar(bool withDelay = true)
        {
            if (withDelay)
                await Task.Delay(Animation.Timings.HideSyncStateViewDelay);

            if (scrollView.ContentOffset.Y > 0) return;

            scrollView.SetContentOffset(CGPoint.Empty, true);
            heightConstraint.Constant = 0;
            UIView.Animate(Animation.Timings.EnterTiming, () =>
            {
                syncStateView.LayoutIfNeeded();
            });
        }

        private void setActivityIndicatorVisible(bool visible)
        {
            activityIndicatorView.Hidden = !visible;

            if (visible)
                activityIndicatorView.StartAnimation();
            else
                activityIndicatorView.StopAnimation();
        }

        private void onStatusLabelTap()
        {
            if (shouldRefreshOnTap == false) return;

            refreshSubject.OnNext(Unit.Default);
        }

        private void onDismissSyncBarButtonTap(object sender, EventArgs e)
        {
            hideSyncBar(false);
        }
    }
}
