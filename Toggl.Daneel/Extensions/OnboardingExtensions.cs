using System;
using CoreGraphics;
using Toggl.PrimeRadiant.Onboarding;
using UIKit;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.PrimeRadiant.Settings;
using Toggl.PrimeRadiant.Extensions;
using System.Reactive.Linq;
using Toggl.Daneel.Views;
using static System.Math;
using System.Reactive.Disposables;
using System.Threading;

namespace Toggl.Daneel.Extensions
{
    public static class Onboarding
    {
        private static readonly nfloat cellRadius = 8;

        private static readonly CGSize shadowOffset = new CGSize(0, 2);

        private static readonly CGColor shadowColor = UIColor.Black.CGColor;

        private static readonly nfloat shadowRadius = 6;

        private const float shadowOpacity = 0.1f;

        private const float closeImageSize = 6;

        private const float closeImageDistanceFromEdge = 9;

        public static void MockSuggestion(this UIView view)
        {
            view.Layer.CornerRadius = cellRadius;
            view.Layer.ShadowOffset = shadowOffset;
            view.Layer.ShadowColor = shadowColor;
            view.Layer.ShadowOpacity = shadowOpacity;
            view.Layer.ShadowRadius = shadowRadius;
        }

        public static IDisposable ManageVisibilityOf(this IOnboardingStep step, UIView view)
        {
            view.Hidden = true;

            void toggleVisibilityOnMainThread(bool shouldBeVisible)
            {
                UIApplication.SharedApplication.InvokeOnMainThread(
                    () => toggleVisibility(shouldBeVisible));
            }

            void toggleVisibility(bool shouldBeVisible)
            {
                var isVisible = view.Hidden == false;
                if (isVisible == shouldBeVisible) return;

                if (shouldBeVisible)
                {
                    view.Hidden = false;
                    view.Alpha = 0;
                    view.Transform = CGAffineTransform.MakeScale(0.01f, 0.01f);
                    AnimationExtensions.Animate(
                        Animation.Timings.LeaveTiming,
                        Animation.Curves.Bounce,
                        () =>
                        {
                            view.Alpha = 1;
                            view.Transform = CGAffineTransform.MakeScale(1f, 1f);
                        },
                        () =>
                        {
                            isVisible = true;
                        });
                }
                else
                {
                    view.Alpha = 1;
                    view.Transform = CGAffineTransform.MakeScale(1f, 1f);
                    AnimationExtensions.Animate(
                        Animation.Timings.LeaveTiming,
                        Animation.Curves.Bounce,
                        () =>
                        {
                            view.Alpha = 0;
                            view.Transform = CGAffineTransform.MakeScale(0.01f, 0.01f);
                        },
                        () =>
                        {
                            view.Hidden = true;
                            isVisible = false;
                        });
                }
            }

            return step.ShouldBeVisible.Subscribe(toggleVisibilityOnMainThread);
        }

        public static void DismissByTapping(this IDismissable step, UIView view)
        {
            var tapGestureRecognizer = new UITapGestureRecognizer(() => step.Dismiss());
            view.AddGestureRecognizer(tapGestureRecognizer);

            addTooltipCloseIcon(view);
        }

        public static IDisposable ManageDismissableTooltip(this IOnboardingStep step, UIView tooltip, IOnboardingStorage storage)
        {
            var dismissableStep = step.ToDismissable(step.GetType().FullName, storage);
            dismissableStep.DismissByTapping(tooltip);
            return dismissableStep.ManageVisibilityOf(tooltip);
        }

        public static UIPanGestureRecognizer DismissBySwiping(this DismissableOnboardingStep step, TimeEntriesLogViewCell cell, Direction direction)
        {
            async void onGesture(UIPanGestureRecognizer recognizer)
            {
                var isOneTouch = recognizer.NumberOfTouches == 1;
                var isVisible = await step.ShouldBeVisible.FirstAsync();
                var isInDesiredDirection = Sign(recognizer.VelocityInView(cell).X) == Sign((int)direction);
                if (isOneTouch && isVisible && isInDesiredDirection)
                    step.Dismiss();
            }

            var panGestureRecognizer = new UIPanGestureRecognizer(onGesture)
            {
                ShouldRecognizeSimultaneously = (a, b) => true
            };

            IDisposable visibilityDisposable = null;
            visibilityDisposable = step.ShouldBeVisible
                .Where(visible => visible == false)
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(_ =>
                {
                    cell.RemoveGestureRecognizer(panGestureRecognizer);
                    visibilityDisposable?.Dispose();
                });

            cell.AddGestureRecognizer(panGestureRecognizer);

            return panGestureRecognizer;
        }

        public static IDisposable ManageSwipeActionAnimationOf(this IOnboardingStep step, TimeEntriesLogViewCell cell, Direction direction)
        {
            IDisposable animation = null;
            void toggleVisibility(bool shouldBeVisible)
            {
                var isVisible = animation != null;
                if (isVisible == shouldBeVisible) return;

                if (shouldBeVisible)
                {
                    animation = cell.RevealSwipeActionAnimation(direction);
                }
                else
                {
                    cell.Layer.RemoveAnimation(direction.ToString());
                    animation?.Dispose();
                    animation = null;
                }
            }

            var subscriptionDisposable = step.ShouldBeVisible.Subscribe(toggleVisibility);

            return Disposable.Create(() =>
            {
                cell?.Layer.RemoveAllAnimations();

                animation?.Dispose();
                animation = null;

                subscriptionDisposable?.Dispose();
                subscriptionDisposable = null;
            });
        }

        private static void addTooltipCloseIcon(this UIView tooltip)
        {
            var closeImage = UIImage.FromBundle("icClose").ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
            var imageView = new UIImageView(closeImage);
            imageView.TintColor = UIColor.White;
            imageView.TranslatesAutoresizingMaskIntoConstraints = false;

            tooltip.AddSubview(imageView);

            imageView.WidthAnchor.ConstraintEqualTo(closeImageSize).Active = true;
            imageView.HeightAnchor.ConstraintEqualTo(closeImageSize).Active = true;
            imageView.TrailingAnchor.ConstraintEqualTo(tooltip.TrailingAnchor, -closeImageDistanceFromEdge).Active = true;
            imageView.TopAnchor.ConstraintEqualTo(tooltip.TopAnchor, closeImageDistanceFromEdge).Active = true;
        }
    }
}
