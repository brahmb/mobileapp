using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using Android.Animation;
using Android.Views;
using Android.Widget;
using MvvmCross.Base;
using Toggl.Giskard.Views;
using Toggl.Multivac;
using Toggl.PrimeRadiant.Extensions;
using Toggl.PrimeRadiant.Onboarding;
using Toggl.PrimeRadiant.Settings;

namespace Toggl.Giskard.Extensions
{
    public static class OnboardingExtensions
    {
        private const int windowTokenCheckInterval = 100;

        public static IDisposable ManageDismissableTooltip(this IOnboardingStep step, PopupWindow tooltip, View anchor, Func<PopupWindow, View, PopupOffsets> popupOffsetsGenerator, IOnboardingStorage storage)
        {
            Ensure.Argument.IsNotNull(tooltip, nameof(tooltip));
            Ensure.Argument.IsNotNull(anchor, nameof(anchor));

            var dismissableStep = step.ToDismissable(step.GetType().FullName, storage);

            dismissableStep.DismissByTapping(tooltip, () => { });

            return dismissableStep.ManageVisibilityOf(tooltip, anchor, popupOffsetsGenerator);
        }

        public static IDisposable ManageVisibilityOf(this IOnboardingStep step, PopupWindow tooltip, View anchor, Func<PopupWindow, View, PopupOffsets> popupOffsetsGenerator)
        {
            Ensure.Argument.IsNotNull(tooltip, nameof(tooltip));
            Ensure.Argument.IsNotNull(anchor, nameof(anchor));

            void toggleVisibilityOnMainThread(bool shouldBeVisible)
            {
                if (shouldBeVisible)
                {
                    showPopupTooltip(tooltip, anchor, popupOffsetsGenerator);
                }
                else
                {
                    tooltip.Dismiss();
                }
            }

            return step.ShouldBeVisible
                       .ObserveOn(SynchronizationContext.Current)
                       .combineWithWindowTokenAvailabilityFrom(anchor)
                       .Subscribe(toggleVisibilityOnMainThread);
        }

        public static IDisposable ManageSwipeActionAnimationOf(this IOnboardingStep step, MainRecyclerViewLogViewHolder viewHolder, AnimationSide side)
        {
            Ensure.Argument.IsNotNull(viewHolder, nameof(viewHolder));

            void toggleVisibilityOnMainThread(bool shouldBeVisible)
            {
                if (shouldBeVisible && !viewHolder.IsAnimating)
                {
                    viewHolder.StartAnimating(side);
                }
            }

            var subscriptionDisposable = step.ShouldBeVisible
                                             .ObserveOn(SynchronizationContext.Current)
                                             .Subscribe(toggleVisibilityOnMainThread);

            return Disposable.Create(() =>
            {
                viewHolder.StopAnimating();
                subscriptionDisposable?.Dispose();
                subscriptionDisposable = null;
            });
        }

        public static void DismissByTapping(this IDismissable step, PopupWindow popupWindow, Action cleanup = null)
        {
            Ensure.Argument.IsNotNull(popupWindow, nameof(popupWindow));

            void OnDismiss(object sender, EventArgs args)
            {
                popupWindow.Dismiss();
                step.Dismiss();
                cleanup();
            }

            popupWindow.ContentView.Click += OnDismiss;
        }

        private static void showPopupTooltip(PopupWindow popupWindow, View anchor, Func<PopupWindow, View, PopupOffsets> popupOffsetsGenerator)
        {
            anchor.Post(() =>
            {
                popupWindow.ContentView.Measure(View.MeasureSpec.MakeMeasureSpec(0, MeasureSpecMode.Unspecified), View.MeasureSpec.MakeMeasureSpec(0, MeasureSpecMode.Unspecified));
                var offsets = popupOffsetsGenerator(popupWindow, anchor);
                popupWindow.ShowAsDropDown(anchor, offsets.HorizontalOffset, offsets.VerticalOffset);
            });
        }

        private static IObservable<bool> combineWithWindowTokenAvailabilityFrom(this IObservable<bool> shouldBeVisibleObservable, View anchor)
        {
            var viewTokenObservable = Observable.Create<bool>(observer =>
            {
                if (anchor == null)
                {
                    observer.OnNext(false);
                    observer.OnCompleted();
                    return Disposable.Empty;
                }

                void checkForToken()
                {
                    if (anchor.WindowToken == null)
                    {
                        observer.OnNext(false);
                    }
                    else
                    {
                        observer.OnNext(true);
                        observer.OnCompleted();
                    }
                }

                return Observable
                    .Interval(TimeSpan.FromMilliseconds(windowTokenCheckInterval))
                    .Subscribe(_ => checkForToken());
            });

            return shouldBeVisibleObservable.CombineLatest(viewTokenObservable,
                (shouldBeVisible, windowTokenIsReady)
                    => visibleWhenBothAreReady(shouldBeVisible, windowTokenIsReady, viewTokenObservable));
        }

        private static bool visibleWhenBothAreReady(bool shouldBeVisible, bool windowTokenIsReady, IObservable<bool> tokenObservable)
        {
            if (shouldBeVisible)
            {
                return windowTokenIsReady;
            }

            tokenObservable.DisposeIfDisposable();
            return false;
        }
    }
}
