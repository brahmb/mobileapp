using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Views;
using Android.Widget;
using MvvmCross.Droid.Support.V7.AppCompat;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using MvvmCross.WeakSubscription;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.Onboarding.MainView;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.Helper;
using Toggl.Giskard.Views;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Extensions;
using Toggl.PrimeRadiant.Onboarding;
using Toggl.PrimeRadiant.Settings;
using static Toggl.Foundation.Sync.SyncProgress;
using static Toggl.Giskard.Extensions.CircularRevealAnimation.AnimationType;
using AnimationSide = Toggl.Giskard.Views.MainRecyclerViewLogViewHolder.AnimationSide;
using FoundationResources = Toggl.Foundation.Resources;

namespace Toggl.Giskard.Activities
{
    [MvxActivityPresentation]
    [Activity(Theme = "@style/AppTheme",
              ScreenOrientation = ScreenOrientation.Portrait,
              ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public sealed partial class MainActivity : MvxAppCompatActivity<MainViewModel>
    {
        private const int snackbarDuration = 5000;

        private CompositeDisposable disposeBag;
        private PopupWindow playButtonTooltipPopupWindow;
        private PopupWindow stopButtonTooltipPopupWindow;
        private PopupWindow tapToEditPopup;
        private PopupWindow swipeRightPopup;
        private PopupWindow swipeLeftPopup;

        private DismissableOnboardingStep editTimeEntryOnboardingStep;
        private IDisposable editTimeEntryOnboardingStepDisposable;

        private DismissableOnboardingStep swipeRightOnboardingStep;
        private IDisposable swipeRightOnboardingStepDisposable;
        private IDisposable swipeRightOnboardingAnimationStepDisposable;
        private IDisposable swipeToContinueWasUsedDisposable;

        private DismissableOnboardingStep swipeLeftOnboardingStep;
        private IDisposable swipeLeftOnboardingStepDisposable;
        private IDisposable swipeLeftOnboardingAnimationStepDisposable;
        private IDisposable swipeToDeleteWasUsedDisposable;

        private IOnboardingStorage onboardingStorage;
        private readonly ISubject<int> timeEntriesCountSubject = new BehaviorSubject<int>(0);

        protected override void OnCreate(Bundle bundle)
        {
            this.ChangeStatusBarColor(Color.ParseColor("#2C2C2C"));

            base.OnCreate(bundle);
            SetContentView(Resource.Layout.MainActivity);
            OverridePendingTransition(Resource.Animation.abc_fade_in, Resource.Animation.abc_fade_out);

            initializeViews();

            SetSupportActionBar(toolbar);
            SupportActionBar.SetDisplayShowHomeEnabled(false);
            SupportActionBar.SetDisplayShowTitleEnabled(false);

            runningEntryCardFrame.Visibility = ViewStates.Invisible;

            disposeBag = new CompositeDisposable();

            disposeBag.Add(ViewModel.IsTimeEntryRunning.Subscribe(onTimeEntryCardVisibilityChanged));
            disposeBag.Add(ViewModel.WeakSubscribe<PropertyChangedEventArgs>(nameof(ViewModel.SyncingProgress), onSyncChanged));

            setupOnboardingSteps();
            setupProjectDotMargin();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            disposeBag?.Dispose();
            disposeBag = null;

            editTimeEntryOnboardingStepDisposable.Dispose();
            swipeRightOnboardingStepDisposable.Dispose();
            swipeRightOnboardingAnimationStepDisposable.Dispose();
            swipeToContinueWasUsedDisposable.Dispose();
            swipeLeftOnboardingStepDisposable.Dispose();
            swipeLeftOnboardingAnimationStepDisposable.Dispose();
            swipeToDeleteWasUsedDisposable.Dispose();
        }

        protected override void OnStop()
        {
            base.OnStop();
            playButtonTooltipPopupWindow.Dismiss();
            stopButtonTooltipPopupWindow.Dismiss();
        }

        private void setupProjectDotMargin()
        {
            ViewModel.CurrentTimeEntryHasDescription
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(hasDescription =>
                {
                    var leftMargin = hasDescription ? 8.DpToPixels(this) : 0;
                    var layoutParams = (RelativeLayout.LayoutParams)projectDotView.LayoutParameters;
                    layoutParams.LeftMargin = leftMargin;
                    projectDotView.LayoutParameters = layoutParams;
                    projectDotView.RequestLayout();
                }).DisposedBy(disposeBag);
        }

        private void onSyncChanged(object sender, PropertyChangedEventArgs args)
        {
            switch (ViewModel.SyncingProgress)
            {
                case Failed:
                case Unknown:
                case OfflineModeDetected:

                    var errorMessage = ViewModel.SyncingProgress == OfflineModeDetected
                                     ? FoundationResources.Offline
                                     : FoundationResources.SyncFailed;

                    var snackbar = Snackbar.Make(coordinatorLayout, errorMessage, Snackbar.LengthLong)
                        .SetAction(FoundationResources.TapToRetry, onRetryTapped);
                    snackbar.SetDuration(snackbarDuration);
                    snackbar.Show();
                    break;
            }

            void onRetryTapped(View view)
            {
                ViewModel.RefreshCommand.Execute();
            }
        }

        private async void onTimeEntryCardVisibilityChanged(bool visible)
        {
            if (runningEntryCardFrame == null) return;

            var isCardVisible = runningEntryCardFrame.Visibility == ViewStates.Visible;
            if (isCardVisible == visible) return;

            var fabListener = new FabAsyncHideListener();
            var radialAnimation =
                runningEntryCardFrame
                    .AnimateWithCircularReveal()
                    .SetDuration(TimeSpan.FromSeconds(0.5))
                    .SetBehaviour((x, y, w, h) => (x, y + h, 0, w))
                    .SetType(() => visible ? Appear : Disappear);

            if (visible)
            {
                playButton.Hide(fabListener);
                await fabListener.HideAsync;

                radialAnimation
                    .OnAnimationEnd(_ => stopButton.Show())
                    .Start();
            }
            else
            {
                stopButton.Hide(fabListener);
                await fabListener.HideAsync;

                radialAnimation
                    .OnAnimationEnd(_ => playButton.Show())
                    .Start();
            }
        }

        private void setupOnboardingSteps()
        {
            onboardingStorage = ViewModel.OnboardingStorage;
            timeEntriesCountSubject.OnNext(ViewModel.TimeEntriesCount);
            ViewModel.WeakSubscribe(() => ViewModel.TimeEntriesCount, onTimeEntriesCountChanged).DisposedBy(disposeBag);

            setupStartTimeEntryOnboardingStep();
            setupStopTimeEntryOnboardingStep();
            setupTapToEditOnboardingStep();
            setupSwipeRightOnboardingStep();
            setupSwipeLeftOnboardingStep();
        }

        private void setupStartTimeEntryOnboardingStep()
        {
            if (playButtonTooltipPopupWindow == null)
            {
                playButtonTooltipPopupWindow = PopupWindowFactory.PopupWindowWithText(
                    this,
                    Resource.Layout.TooltipWithRightArrow,
                    Resource.Id.TooltipText,
                    Resource.String.OnboardingTapToStartTimer);
            }

            new StartTimeEntryOnboardingStep(onboardingStorage)
                .ManageDismissableTooltip(
                    playButtonTooltipPopupWindow,
                    playButton,
                    (popup, anchor) => popup.LeftVerticallyCenteredOffsetsTo(anchor, dpExtraRightMargin: 8),
                    onboardingStorage)
                .DisposedBy(disposeBag);
        }

        private void setupStopTimeEntryOnboardingStep()
        {
            if (stopButtonTooltipPopupWindow == null)
            {
                stopButtonTooltipPopupWindow = PopupWindowFactory.PopupWindowWithText(
                    this,
                    Resource.Layout.TooltipWithRightBottomArrow,
                    Resource.Id.TooltipText,
                    Resource.String.OnboardingTapToStopTimer);
            }

            new StopTimeEntryOnboardingStep(onboardingStorage, ViewModel.IsTimeEntryRunning)
                .ManageDismissableTooltip(
                    stopButtonTooltipPopupWindow,
                    stopButton,
                    (popup, anchor) => popup.TopRightFrom(anchor, dpExtraBottomMargin: 8),
                    onboardingStorage)
                .DisposedBy(disposeBag);
        }

        private void setupTapToEditOnboardingStep()
        {
            tapToEditPopup = PopupWindowFactory.PopupWindowWithText(
                    this,
                    Resource.Layout.TooltipWithLeftTopArrow,
                    Resource.Id.TooltipText,
                    Resource.String.OnboardingTapToEdit);

            editTimeEntryOnboardingStep = new EditTimeEntryOnboardingStep(onboardingStorage, Observable.Return(false))
                .ToDismissable(nameof(EditTimeEntryOnboardingStep), onboardingStorage);
            editTimeEntryOnboardingStep.DismissByTapping(tapToEditPopup, () => { });

            mainRecyclerView.FirstTimeEntryViewHolder
                            .ObserveOn(SynchronizationContext.Current)
                            .Subscribe(updateTapToEditOnboardingStep)
                            .DisposedBy(disposeBag);
        }

        private void setupSwipeRightOnboardingStep()
        {
            var shouldBeVisible = editTimeEntryOnboardingStep
                .ShouldBeVisible
                .Select(visible => !visible);

            swipeRightPopup = PopupWindowFactory.PopupWindowWithText(
                    this,
                    Resource.Layout.TooltipWithLeftTopArrow,
                    Resource.Id.TooltipText,
                    Resource.String.OnboardingSwipeRight);

            swipeRightOnboardingStep = new SwipeRightOnboardingStep(shouldBeVisible, timeEntriesCountSubject.AsObservable())
                .ToDismissable(nameof(SwipeRightOnboardingStep), onboardingStorage);
            swipeRightOnboardingStep.DismissByTapping(swipeRightPopup, () => {
                if (swipeRightOnboardingAnimationStepDisposable != null)
                {
                    swipeRightOnboardingAnimationStepDisposable.Dispose();
                    swipeRightOnboardingAnimationStepDisposable = null;
                }
            });

            swipeToContinueWasUsedDisposable = mainRecyclerView.MainRecyclerAdapter.SwipeToContinueWasUsedObservable
               .VoidSubscribe(() =>
               {
                   swipeRightOnboardingStep.Dismiss();
                   swipeToContinueWasUsedDisposable.Dispose();
                   swipeToContinueWasUsedDisposable = null;
               });

            mainRecyclerView.LastTimeEntryViewHolder
                            .ObserveOn(SynchronizationContext.Current)
                            .Subscribe(updateSwipeRightOnboardingStep)
                            .DisposedBy(disposeBag);
        }

        private void setupSwipeLeftOnboardingStep()
        {
            var shouldBeVisible = Observable.CombineLatest(
                editTimeEntryOnboardingStep.ShouldBeVisible,
                swipeRightOnboardingStep.ShouldBeVisible,
                (editTimeEntryVisible, swipeRightVisible) => !editTimeEntryVisible && !swipeRightVisible
            );

            swipeLeftPopup = PopupWindowFactory.PopupWindowWithText(
                    this,
                    Resource.Layout.TooltipWithRightTopArrow,
                    Resource.Id.TooltipText,
                    Resource.String.OnboardingSwipeLeft);

            swipeLeftOnboardingStep = new SwipeLeftOnboardingStep(shouldBeVisible, timeEntriesCountSubject.AsObservable())
                .ToDismissable(nameof(SwipeLeftOnboardingStep), onboardingStorage);
            swipeLeftOnboardingStep.DismissByTapping(swipeLeftPopup, () => {
                if (swipeLeftOnboardingAnimationStepDisposable != null)
                {
                    swipeLeftOnboardingAnimationStepDisposable.Dispose();
                    swipeLeftOnboardingAnimationStepDisposable = null;
                }
            });

            swipeToDeleteWasUsedDisposable = mainRecyclerView.MainRecyclerAdapter.SwipeToDeleteWasUsedObservable
               .VoidSubscribe(() =>
               {
                   swipeLeftOnboardingStep.Dismiss();
                   swipeToDeleteWasUsedDisposable.Dispose();
                   swipeToDeleteWasUsedDisposable = null;
               });

            mainRecyclerView.LastTimeEntryViewHolder
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(updateSwipeLeftOnboardingStep)
                .DisposedBy(disposeBag);
        }

        private void updateTapToEditOnboardingStep(MainRecyclerViewLogViewHolder firstTimeEntry)
        {
            tapToEditPopup?.Dismiss();

            if (firstTimeEntry == null)
                return;

            if (editTimeEntryOnboardingStepDisposable != null)
            {
                editTimeEntryOnboardingStepDisposable.Dispose();
                editTimeEntryOnboardingStepDisposable = null;
            }

            editTimeEntryOnboardingStepDisposable = editTimeEntryOnboardingStep
                .ManageVisibilityOf(
                    tapToEditPopup,
                    firstTimeEntry.ItemView,
                    (window, view) => PopupOffsets.FromDp(16, -4, this));
        }

        private void updateSwipeRightOnboardingStep(MainRecyclerViewLogViewHolder lastTimeEntry)
        {
            swipeRightPopup?.Dismiss();

            if (lastTimeEntry == null)
                return;

            if (swipeRightOnboardingStepDisposable != null)
            {
                swipeRightOnboardingStepDisposable.Dispose();
                swipeRightOnboardingStepDisposable = null;
            }

            swipeRightOnboardingStepDisposable = swipeRightOnboardingStep
                .ManageVisibilityOf(
                    swipeRightPopup,
                    lastTimeEntry.ItemView,
                    (window, view) => PopupOffsets.FromDp(16, -4, this));

            if (swipeRightOnboardingAnimationStepDisposable != null)
            {
                swipeRightOnboardingAnimationStepDisposable.Dispose();
                swipeRightOnboardingAnimationStepDisposable = null;
            }

            swipeRightOnboardingAnimationStepDisposable = swipeRightOnboardingStep
                .ManageSwipeActionAnimationOf(mainRecyclerView, lastTimeEntry, AnimationSide.Right);
        }

        private void updateSwipeLeftOnboardingStep(MainRecyclerViewLogViewHolder lastTimeEntry)
        {
            swipeLeftPopup?.Dismiss();

            if (lastTimeEntry == null)
                return;

            if (swipeLeftOnboardingStepDisposable != null)
            {
                swipeLeftOnboardingStepDisposable.Dispose();
                swipeLeftOnboardingStepDisposable = null;
            }

            swipeLeftOnboardingStepDisposable = swipeLeftOnboardingStep
                .ManageVisibilityOf(
                    swipeLeftPopup,
                    lastTimeEntry.ItemView,
                    (window, view) => window.BottomRightOffsetsTo(view, -16, -4));

            if (swipeLeftOnboardingAnimationStepDisposable != null)
            {
                swipeLeftOnboardingAnimationStepDisposable.Dispose();
                swipeLeftOnboardingAnimationStepDisposable = null;
            }

            swipeLeftOnboardingAnimationStepDisposable = swipeLeftOnboardingStep
                .ManageSwipeActionAnimationOf(mainRecyclerView, lastTimeEntry, AnimationSide.Left);
        }

        private void onTimeEntriesCountChanged(object sender, PropertyChangedEventArgs e)
        {
            timeEntriesCountSubject.OnNext(ViewModel.TimeEntriesCount);
        }

        private sealed class FabAsyncHideListener : FloatingActionButton.OnVisibilityChangedListener
        {
            private readonly TaskCompletionSource<object> hideTaskCompletionSource = new TaskCompletionSource<object>();

            public Task HideAsync => hideTaskCompletionSource.Task;

            public override void OnHidden(FloatingActionButton fab)
            {
                base.OnHidden(fab);
                hideTaskCompletionSource.SetResult(null);
            }
        }
    }
}
