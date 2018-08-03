using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using CoreGraphics;
using Foundation;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Ios.Binding;
using MvvmCross.Platforms.Ios.Presenters.Attributes;
using MvvmCross.Plugin.Color.Platforms.Ios;
using MvvmCross.Plugin.Visibility;
using Toggl.Daneel.Combiners;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Suggestions;
using Toggl.Daneel.Views;
using Toggl.Daneel.ViewSources;
using Toggl.Foundation.MvvmCross.Combiners;
using Toggl.Foundation.MvvmCross.Converters;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.Onboarding.MainView;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Extensions;
using Toggl.PrimeRadiant.Onboarding;
using Toggl.PrimeRadiant.Settings;
using UIKit;
using static Toggl.Foundation.MvvmCross.Helper.Animation;

namespace Toggl.Daneel.ViewControllers
{
    [MvxRootPresentation(WrapInNavigationController = true)]
    public partial class MainViewController : ReactiveViewController<MainViewModel>
    {
        private const float showCardDelay = 0.1f;

        private const float spiderHingeCornerRadius = 0.8f;
        private const float spiderHingeWidth = 16;
        private const float spiderHingeHeight = 2;

        private const float tooltipOffset = 7;
        private const float emptyViewTopMargin = 32;

        private readonly UIView spiderContainerView = new UIView();
        private readonly SpiderOnARopeView spiderBroView = new SpiderOnARopeView();
        private readonly UIButton reportsButton = new UIButton(new CGRect(0, 0, 30, 40));
        private readonly UIButton settingsButton = new UIButton(new CGRect(0, 0, 30, 40));
        private readonly UIButton syncFailuresButton = new UIButton(new CGRect(0, 0, 30, 40));
        private readonly UIImageView titleImage = new UIImageView(UIImage.FromBundle("togglLogo"));
        private readonly TimeEntriesEmptyLogView emptyStateView = TimeEntriesEmptyLogView.Create();

        private TimeEntriesLogViewCell firstTimeEntryCell;

        private bool viewInitialized;
        private CancellationTokenSource cardAnimationCancellation;

        private DismissableOnboardingStep tapToEditStep;
        private DismissableOnboardingStep swipeLeftStep;
        private DismissableOnboardingStep swipeRightStep;

        private UIGestureRecognizer swipeLeftGestureRecognizer;

        private CompositeDisposable disposeBag = new CompositeDisposable();

        private IDisposable swipeLeftAnimationDisposable;
        private IDisposable swipeRightAnimationDisposable;

        private readonly UIView tableHeader = new UIView();
        private readonly UIView suggestionsContaier = new UIView { TranslatesAutoresizingMaskIntoConstraints = false };
        private readonly UIView ratingViewContainer = new UIView { TranslatesAutoresizingMaskIntoConstraints = false };
        private readonly SuggestionsView suggestionsView = new SuggestionsView { TranslatesAutoresizingMaskIntoConstraints = false };

        private TimeEntriesLogViewSource tableViewSource;

        public MainViewController()
            : base(nameof(MainViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            prepareViews();
            prepareOnboarding();
            setupTableViewHeader();

            var visibilityConverter = new MvxVisibilityValueConverter();
            var projectTaskClientCombiner = new ProjectTaskClientValueCombiner(
                CurrentTimeEntryProjectTaskClientLabel.Font.CapHeight,
                Color.Main.CurrentTimeEntryClientColor.ToNativeColor(),
                true
            );
            var startTimeEntryButtonManualModeIconConverter = new BoolToConstantValueConverter<UIImage>(
                UIImage.FromBundle("manualIcon"),
                UIImage.FromBundle("playIcon")
            );
            var durationCombiner = new DurationValueCombiner();

            var bindingSet = this.CreateBindingSet<MainViewController, MainViewModel>();

            // Table view
            tableViewSource = new TimeEntriesLogViewSource(ViewModel.TimeEntries, TimeEntriesLogViewCell.Identifier);
            TimeEntriesLogTableView
                .Bind(tableViewSource)
                .DisposedBy(disposeBag);

            this.Bind(tableViewSource.FirstCell, f =>
            {
                onFirstTimeEntryChanged(f);
                firstTimeEntryCell = f;
            });

            this.Bind(tableViewSource.ScrollOffset, onTableScroll);

            var continueTimeEntry = Observable.Merge(
                tableViewSource.ContinueTap,
                tableViewSource.SwipeToContinue
            );
            this.Bind(continueTimeEntry, ViewModel.ContinueTimeEntry);
            this.Bind(tableViewSource.SwipeToDelete, ViewModel.DeleteTimeEntry);
            this.Bind(tableViewSource.ItemSelected, ViewModel.SelectTimeEntry);

            tableViewSource.SwipeToContinue
                .VoidSubscribe(() =>
                {
                    swipeRightStep.Dismiss();
                })
                .DisposedBy(disposeBag);

            tableViewSource.SwipeToDelete
                .VoidSubscribe(() =>
                {
                    swipeLeftStep.Dismiss();
                })
                .DisposedBy(disposeBag);

            // Refresh Control
            var refreshControl = new RefreshControl(ViewModel.SyncProgressState.AsDriver(), tableViewSource);
            this.Bind(refreshControl.Refresh, ViewModel.RefreshAction);
            TimeEntriesLogTableView.CustomRefreshControl = refreshControl;

            //Commands
            bindingSet.Bind(reportsButton).To(vm => vm.OpenReportsCommand);
            bindingSet.Bind(settingsButton).To(vm => vm.OpenSettingsCommand);
            bindingSet.Bind(StopTimeEntryButton).To(vm => vm.StopTimeEntryCommand);
            bindingSet.Bind(StartTimeEntryButton).To(vm => vm.StartTimeEntryCommand);
            bindingSet.Bind(EditTimeEntryButton).To(vm => vm.EditTimeEntryCommand);
            bindingSet.Bind(syncFailuresButton).To(vm => vm.OpenSyncFailuresCommand);

            bindingSet.Bind(CurrentTimeEntryCard)
                      .For(v => v.BindTap())
                      .To(vm => vm.EditTimeEntryCommand);

            bindingSet.Bind(suggestionsView)
                      .For(v => v.SuggestionTappedCommad)
                      .To(vm => vm.SuggestionsViewModel.StartTimeEntryCommand);

            bindingSet.Bind(StartTimeEntryButton)
                      .For(v => v.BindLongPress())
                      .To(vm => vm.AlternativeStartTimeEntryCommand);

            //Visibility
            var shouldWelcomeBack = ViewModel.ShouldShowWelcomeBack.AsDriver();
            this.Bind(ViewModel.ShouldShowEmptyState.AsDriver(), visible => emptyStateView.Hidden = !visible);
            this.Bind(shouldWelcomeBack, WelcomeBackView.BindIsVisible());
            this.Bind(shouldWelcomeBack, spiderContainerView.BindIsVisible());
            this.Bind(shouldWelcomeBack, visible =>
            {
                if (visible)
                    spiderBroView.Show();
                else
                    spiderBroView.Hide();
            });

            //Text
            bindingSet.Bind(CurrentTimeEntryDescriptionLabel).To(vm => vm.CurrentTimeEntryDescription);

            bindingSet.Bind(CurrentTimeEntryElapsedTimeLabel)
                      .ByCombining(durationCombiner,
                          vm => vm.CurrentTimeEntryElapsedTime,
                          vm => vm.CurrentTimeEntryElapsedTimeFormat);

            bindingSet.Bind(CurrentTimeEntryProjectTaskClientLabel)
                      .For(v => v.AttributedText)
                      .ByCombining(projectTaskClientCombiner,
                                   v => v.CurrentTimeEntryProject,
                                   v => v.CurrentTimeEntryTask,
                                   v => v.CurrentTimeEntryClient,
                                   v => v.CurrentTimeEntryProjectColor);

            //The start button
            bindingSet.Bind(StartTimeEntryButton)
                      .For(v => v.BindImage())
                      .To(vm => vm.IsInManualMode)
                      .WithConversion(startTimeEntryButtonManualModeIconConverter);

            //The sync failures button
            bindingSet.Bind(syncFailuresButton)
                .For(v => v.BindVisibility())
                .To(vm => vm.NumberOfSyncFailures)
                .WithConversion(visibilityConverter);

            bindingSet.Apply();

            this.Bind(ViewModel.RatingViewModel.IsFeedbackSuccessViewShowing,
                SendFeedbackSuccessView.BindAnimatedIsVisible());
            this.BindVoid(SendFeedbackSuccessView.Tapped(), ViewModel.RatingViewModel.CloseFeedbackSuccessView);

            View.SetNeedsLayout();
            View.LayoutIfNeeded();
        }

        private void setupTableViewHeader()
        {
            TimeEntriesLogTableView.TableHeaderView = tableHeader;

            tableHeader.TranslatesAutoresizingMaskIntoConstraints = false;
            tableHeader.WidthAnchor.ConstraintEqualTo(TimeEntriesLogTableView.WidthAnchor).Active = true;

            tableHeader.AddSubview(suggestionsContaier);
            tableHeader.AddSubview(ratingViewContainer);

            suggestionsContaier.ConstrainToViewSides(tableHeader);
            ratingViewContainer.ConstrainToViewSides(tableHeader);

            suggestionsContaier.TopAnchor.ConstraintEqualTo(tableHeader.TopAnchor).Active = true;
            suggestionsContaier.BottomAnchor.ConstraintEqualTo(ratingViewContainer.TopAnchor).Active = true;
            ratingViewContainer.BottomAnchor.ConstraintEqualTo(tableHeader.BottomAnchor).Active = true;

            suggestionsContaier.AddSubview(suggestionsView);
            suggestionsView.ConstrainInView(suggestionsContaier);
            suggestionsView.DataContext = ViewModel.SuggestionsViewModel;
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            NavigationItem.TitleView = titleImage;
            NavigationItem.RightBarButtonItems = new[]
            {
                new UIBarButtonItem(settingsButton),
                new UIBarButtonItem(reportsButton)
            };

#if DEBUG
            NavigationItem.LeftBarButtonItems = new[]
            {
                new UIBarButtonItem(syncFailuresButton)
            };
#endif
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            spiderBroView.Dispose();
            ViewModel.NavigationService.AfterNavigate -= onNavigate;

            disposeBag?.Dispose();
            disposeBag = null;
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            if (viewInitialized) return;

            viewInitialized = true;

            ViewModel.IsTimeEntryRunning
                .ObserveOn(SynchronizationContext.Current)
                .Where(visible => visible)
                .VoidSubscribe(showTimeEntryCard)
                .DisposedBy(disposeBag);

            ViewModel.IsTimeEntryRunning
                .ObserveOn(SynchronizationContext.Current)
                .Where(visible => !visible)
                .VoidSubscribe(hideTimeEntryCard)
                .DisposedBy(disposeBag);
        }

        RatingView ratingView;
        public void ShowRatingView()
        {
            ratingView = RatingView.Create();
            ratingView.TranslatesAutoresizingMaskIntoConstraints = false;
            ratingView.DataContext = ViewModel.RatingViewModel;
            ratingViewContainer.AddSubview(ratingView);
            ratingView.ConstrainInView(ratingViewContainer);
            Reload();
        }

        public void HideRatingView()
        {
            ratingView.RemoveFromSuperview();
            ratingView.Dispose();
            ratingView = null;

            //We have to scroll a little to update the header size.
            //Using LayoutSubviews(), SetNeedsLayout(), LayoutIfNeeded() etc. does not work.
            var offset = TimeEntriesLogTableView.ContentOffset;
            var rect = new CGRect
            {
                X = offset.X,
                Y = offset.Y == 0
                    ? TimeEntriesLogTableView.Frame.Height + 1
                    : offset.Y - 1,
                Width = 1,
                Height = 1
            };
            TimeEntriesLogTableView.ScrollRectToVisible(rect, true);
        }

        private void prepareViews()
        {
            //Prevent bounces in UIScrollView
            AutomaticallyAdjustsScrollViewInsets = false;

            //Card border
            CurrentTimeEntryCard.Layer.CornerRadius = 8;
            CurrentTimeEntryCard.Layer.ShadowColor = UIColor.Black.CGColor;
            CurrentTimeEntryCard.Layer.ShadowOffset = new CGSize(0, -2);
            CurrentTimeEntryCard.Layer.ShadowOpacity = 0.1f;
            CurrentTimeEntryElapsedTimeLabel.Font = CurrentTimeEntryElapsedTimeLabel.Font.GetMonospacedDigitFont();

            // Card animations
            StopTimeEntryButton.Hidden = true;
            CurrentTimeEntryCard.Hidden = true;

            //Hide play button for later animating it
            StartTimeEntryButton.Transform = CGAffineTransform.MakeScale(0.01f, 0.01f);

            //Prepare Navigation bar images
            reportsButton.SetImage(UIImage.FromBundle("icReports"), UIControlState.Normal);
            settingsButton.SetImage(UIImage.FromBundle("icSettings"), UIControlState.Normal);
            syncFailuresButton.SetImage(UIImage.FromBundle("icWarning"), UIControlState.Normal);

            RunningEntryDescriptionFadeView.FadeLeft = true;
            RunningEntryDescriptionFadeView.FadeRight = true;

            // Send Feedback Success View Setup
            SendFeedbackSuccessView.Hidden = true;

            prepareSpiderViews();
            prepareEmptyStateView();

            View.BackgroundColor = Color.Main.BackgroundColor.ToNativeColor();
        }

        private void showTimeEntryCard()
        {
            StopTimeEntryButton.Hidden = false;
            CurrentTimeEntryCard.Hidden = false;

            cardAnimationCancellation?.Cancel();
            cardAnimationCancellation = new CancellationTokenSource();

            AnimationExtensions.Animate(Timings.EnterTiming, showCardDelay, Curves.EaseOut,
                () => StartTimeEntryButton.Transform = CGAffineTransform.MakeScale(0.01f, 0.01f),
                () =>
                {
                    AnimationExtensions.Animate(Timings.LeaveTimingFaster, Curves.EaseIn,
                        () => StopTimeEntryButton.Transform = CGAffineTransform.MakeScale(1.0f, 1.0f),
                        cancellationToken: cardAnimationCancellation.Token);

                    AnimationExtensions.Animate(Timings.LeaveTiming, Curves.CardOutCurve,
                        () => CurrentTimeEntryCard.Transform = CGAffineTransform.MakeTranslation(0, 0),
                        cancellationToken: cardAnimationCancellation.Token);
                },
                cancellationToken: cardAnimationCancellation.Token);
        }

        private void hideTimeEntryCard()
        {
            cardAnimationCancellation?.Cancel();
            cardAnimationCancellation = new CancellationTokenSource();

            AnimationExtensions.Animate(Timings.LeaveTimingFaster, Curves.EaseIn,
                () => StopTimeEntryButton.Transform = CGAffineTransform.MakeScale(0.01f, 0.01f),
                () => StopTimeEntryButton.Hidden = true,
                cancellationToken: cardAnimationCancellation.Token);

            AnimationExtensions.Animate(Timings.LeaveTiming, Curves.CardOutCurve,
                () => CurrentTimeEntryCard.Transform = CGAffineTransform.MakeTranslation(0, CurrentTimeEntryCard.Frame.Height),
                () =>
                {
                    CurrentTimeEntryCard.Hidden = true;

                    AnimationExtensions.Animate(Timings.EnterTiming, Curves.EaseOut,
                        () => StartTimeEntryButton.Transform = CGAffineTransform.MakeScale(1f, 1f),
                        cancellationToken: cardAnimationCancellation.Token);
                },
                cancellationToken: cardAnimationCancellation.Token);
        }

        //Spider is added in code, because IB doesn't allow adding subviews
        //to a UITableView and the spider needs to be a subview of the table
        //view so it reacts to pulling down to refresh
        private void prepareSpiderViews()
        {
            var spiderHinge = new UIView();

            spiderHinge.Layer.CornerRadius = spiderHingeCornerRadius;
            spiderHinge.TranslatesAutoresizingMaskIntoConstraints = false;
            spiderHinge.BackgroundColor = Color.Main.SpiderHinge.ToNativeColor();
            spiderContainerView.TranslatesAutoresizingMaskIntoConstraints = false;
            spiderBroView.TranslatesAutoresizingMaskIntoConstraints = false;
            spiderContainerView.BackgroundColor = UIColor.Clear;

            spiderContainerView.AddSubview(spiderHinge);
            spiderContainerView.AddSubview(spiderBroView);
            TimeEntriesLogTableView.AddSubview(spiderContainerView);

            //Container constraints
            spiderContainerView.WidthAnchor.ConstraintEqualTo(TimeEntriesLogTableView.WidthAnchor).Active = true;
            spiderContainerView.HeightAnchor.ConstraintEqualTo(TimeEntriesLogTableView.HeightAnchor).Active = true;
            spiderContainerView.CenterYAnchor.ConstraintEqualTo(TimeEntriesLogTableView.CenterYAnchor).Active = true;
            spiderContainerView.CenterXAnchor.ConstraintEqualTo(TimeEntriesLogTableView.CenterXAnchor).Active = true;

            //Hinge constraints
            spiderHinge.WidthAnchor.ConstraintEqualTo(spiderHingeWidth).Active = true;
            spiderHinge.HeightAnchor.ConstraintEqualTo(spiderHingeHeight).Active = true;
            spiderHinge.TopAnchor.ConstraintEqualTo(spiderContainerView.TopAnchor).Active = true;
            spiderHinge.CenterXAnchor.ConstraintEqualTo(spiderContainerView.CenterXAnchor).Active = true;

            //Spider constraints
            spiderBroView.TopAnchor.ConstraintEqualTo(spiderContainerView.TopAnchor).Active = true;
            spiderBroView.WidthAnchor.ConstraintEqualTo(spiderContainerView.WidthAnchor).Active = true;
            spiderBroView.BottomAnchor.ConstraintEqualTo(spiderContainerView.BottomAnchor).Active = true;
            spiderBroView.CenterXAnchor.ConstraintEqualTo(spiderContainerView.CenterXAnchor).Active = true;
        }

        private void prepareEmptyStateView()
        {
            emptyStateView.BackgroundColor = UIColor.Clear;
            emptyStateView.TranslatesAutoresizingMaskIntoConstraints = false;

            TimeEntriesLogTableView.AddSubview(emptyStateView);

            emptyStateView.WidthAnchor.ConstraintEqualTo(TimeEntriesLogTableView.WidthAnchor).Active = true;
            emptyStateView.HeightAnchor.ConstraintEqualTo(TimeEntriesLogTableView.HeightAnchor).Active = true;
            emptyStateView.CenterYAnchor.ConstraintEqualTo(TimeEntriesLogTableView.CenterYAnchor).Active = true;
            emptyStateView.TopAnchor.ConstraintEqualTo(TimeEntriesLogTableView.TopAnchor, emptyViewTopMargin).Active = true;
        }

        private void prepareOnboarding()
        {
            var storage = ViewModel.OnboardingStorage;

            var timelineIsEmpty = ViewModel.LogEmpty.AsDriver();

            new StartTimeEntryOnboardingStep(storage)
                .ManageDismissableTooltip(StartTimeEntryOnboardingBubbleView, storage)
                .DisposedBy(disposeBag);

            new StopTimeEntryOnboardingStep(storage, ViewModel.IsTimeEntryRunning)
                .ManageDismissableTooltip(StopTimeEntryOnboardingBubbleView, storage)
                .DisposedBy(disposeBag);

            tapToEditStep = new EditTimeEntryOnboardingStep(storage, timelineIsEmpty)
                .ToDismissable(nameof(EditTimeEntryOnboardingStep), storage);

            tapToEditStep.DismissByTapping(TapToEditBubbleView);
            tapToEditStep.ManageVisibilityOf(TapToEditBubbleView).DisposedBy(disposeBag);

            prepareSwipeGesturesOnboarding(storage, tapToEditStep.ShouldBeVisible);

            ViewModel.NavigationService.AfterNavigate += onNavigate;
        }

        private void prepareSwipeGesturesOnboarding(IOnboardingStorage storage, IObservable<bool> tapToEditStepIsVisible)
        {
            var timeEntriesCount = ViewModel.TimeEntriesCount.AsDriver();

            var swipeRightCanBeShown =
                UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
                    ? tapToEditStepIsVisible.Select(isVisible => !isVisible)
                    : Observable.Return(false);

            swipeRightStep = new SwipeRightOnboardingStep(swipeRightCanBeShown, timeEntriesCount)
                .ToDismissable(nameof(SwipeRightOnboardingStep), storage);

            var swipeLeftCanBeShown = Observable.CombineLatest(
                tapToEditStepIsVisible,
                swipeRightStep.ShouldBeVisible,
                (tapToEditIsVisible, swipeRightIsVisble) => !tapToEditIsVisible && !swipeRightIsVisble);
            swipeLeftStep = new SwipeLeftOnboardingStep(swipeLeftCanBeShown, timeEntriesCount)
                .ToDismissable(nameof(SwipeLeftOnboardingStep), storage);

            swipeLeftStep.DismissByTapping(SwipeLeftBubbleView);
            swipeLeftStep.ManageVisibilityOf(SwipeLeftBubbleView).DisposedBy(disposeBag);
            swipeLeftAnimationDisposable = swipeLeftStep.ManageSwipeActionAnimationOf(firstTimeEntryCell, Direction.Left);

            swipeRightStep.DismissByTapping(SwipeRightBubbleView);
            swipeRightStep.ManageVisibilityOf(SwipeRightBubbleView).DisposedBy(disposeBag);
            swipeRightAnimationDisposable = swipeRightStep.ManageSwipeActionAnimationOf(firstTimeEntryCell, Direction.Right);

            updateSwipeDismissGestures(firstTimeEntryCell);
        }

        private void onTableScroll(CGPoint offset)
        {
            updateTooltipPositions();
        }

        private void onFirstTimeEntryChanged(TimeEntriesLogViewCell nextFirstTimeEntry)
        {
            updateSwipeDismissGestures(nextFirstTimeEntry);
            firstTimeEntryCell = nextFirstTimeEntry;
            updateTooltipPositions();
        }

        private void onNavigate(object sender, EventArgs e)
        {
            bool isHidden = false;
            InvokeOnMainThread(() => isHidden = TapToEditBubbleView.Hidden);

            if (isHidden == false)
            {
                tapToEditStep.Dismiss();
                ViewModel.NavigationService.AfterNavigate -= onNavigate;
            }
        }

        private void updateTooltipPositions()
        {
            if (TapToEditBubbleView.Hidden && SwipeLeftBubbleView.Hidden && SwipeRightBubbleView.Hidden) return;
            if (firstTimeEntryCell == null) return;

            var position = TimeEntriesLogTableView.ConvertRectToView(
                firstTimeEntryCell.Frame, TimeEntriesLogTableView.Superview);

            TapToEditBubbleViewTopConstraint.Constant = position.Bottom + tooltipOffset;
            SwipeLeftTopConstraint.Constant = position.Y - SwipeLeftBubbleView.Frame.Height - tooltipOffset;
            SwipeRightTopConstraint.Constant = position.Y - SwipeRightBubbleView.Frame.Height - tooltipOffset;
        }

        private void updateSwipeDismissGestures(TimeEntriesLogViewCell nextFirstTimeEntry)
        {
            if (swipeLeftGestureRecognizer != null)
            {
                firstTimeEntryCell?.RemoveGestureRecognizer(swipeLeftGestureRecognizer);
            }

            swipeLeftAnimationDisposable?.Dispose();
            swipeRightAnimationDisposable?.Dispose();

            if (nextFirstTimeEntry == null) return;

            swipeLeftAnimationDisposable = swipeLeftStep.ManageSwipeActionAnimationOf(nextFirstTimeEntry, Direction.Left);
            swipeRightAnimationDisposable = swipeRightStep.ManageSwipeActionAnimationOf(nextFirstTimeEntry, Direction.Right);

            swipeLeftGestureRecognizer = swipeLeftStep.DismissBySwiping(nextFirstTimeEntry, Direction.Left);
        }

        internal void Reload()
        {
            var range = new NSRange(0, TimeEntriesLogTableView.NumberOfSections());
            var indexSet = NSIndexSet.FromNSRange(range);
            TimeEntriesLogTableView.ReloadSections(indexSet, UITableViewRowAnimation.None);
        }
    }
}
