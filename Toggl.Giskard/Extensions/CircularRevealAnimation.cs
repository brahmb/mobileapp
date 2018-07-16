using System;
using Android.Views;

namespace Toggl.Giskard.Extensions
{
    public sealed class CircularRevealAnimation
    {
        private View view;
        private int cx, cy, initialRadius, finalRadius;
        private int? duration;
        private AnimationType type;
        private Action<View> onAnimationEnd;

        public CircularRevealAnimation(View view)
        {
            this.view = view;
            setInitialBehaviour();
        }

        private void setInitialBehaviour() 
        {
            type = AnimationType.Appear;

            var position = new int[2];
            view.GetLocationInWindow(position);

            cx = position[0] + view.MeasuredWidth / 2;
            cy = position[1] + view.MeasuredHeight / 2;
            initialRadius = 0;
            finalRadius = Math.Max(view.MeasuredWidth, view.MeasuredHeight) / 2;
        }

        public CircularRevealAnimation SetBehaviour(int cx, int cy, int initialRadiusWhenAppearing, int finalRadiusWhenAppearing)
        {
            this.cx = cx;
            this.cy = cy;
            initialRadius = initialRadiusWhenAppearing;
            finalRadius = finalRadiusWhenAppearing;

            return this;
        }

        public CircularRevealAnimation SetBehaviour(
            Func<int, int, int, int, (int centerX, int centerY, int initialRadius, int finalRadius)> behavior)
        {
            var position = new int[2];
            view.GetLocationInWindow(position);

            var (centerX, centerY, initial, final) 
                = behavior(position[0], position[1], view.MeasuredWidth, view.MeasuredHeight);

            cx = centerX;
            cy = centerY;
            initialRadius = initial;
            finalRadius = final;

            return this;
        }

        public CircularRevealAnimation FromBottomToTop()
        {
            var position = new int[2];
            view.GetLocationInWindow(position);

            cx = position[0] + view.MeasuredWidth / 2;
            cy = position[1] + view.Height;
            initialRadius = 0;
            finalRadius = Math.Max(view.MeasuredWidth / 2, view.MeasuredHeight);

            return this;
        }

        public CircularRevealAnimation FromBottomLeftToTopRight() 
        { 
            var position = new int[2];
            view.GetLocationInWindow(position);

            cx = position[0];
            cy = position[1] + view.Height;
            initialRadius = 0;
            finalRadius = Math.Max(view.MeasuredWidth, view.MeasuredHeight);

            return this;
        }

        public CircularRevealAnimation SetDuration(TimeSpan span)
        {
            duration = (int)span.TotalMilliseconds;

            return this;
        }

        public CircularRevealAnimation SetType(Func<AnimationType> determineAnimationType)
        {
            type = determineAnimationType();

            return this;
        }

        public CircularRevealAnimation OnAnimationEnd(Action<View> action)
        {
            this.onAnimationEnd = action;

            return this;
        }

        public void Start()
        {
            view.RunWhenAttachedToWindow(() =>
            {
                var initialRadius = this.initialRadius;
                var finalRadius = this.finalRadius;

                if (type == AnimationType.Disappear)
                    (initialRadius, finalRadius) = (finalRadius, initialRadius);

                var animator = ViewAnimationUtils.CreateCircularReveal(view, cx, cy, initialRadius, finalRadius);

                if (duration.HasValue)
                    animator.SetDuration(duration.Value);

                EventHandler onEnd = null;
                onEnd = (o, s) =>
                {
                    if (type == AnimationType.Disappear)
                        view.Visibility = ViewStates.Invisible;

                    animator.AnimationEnd -= onEnd;
                    onAnimationEnd?.Invoke(view);
                };

                animator.AnimationEnd += onEnd;

                if (type == AnimationType.Appear)
                    view.Visibility = ViewStates.Visible;

                animator.Start();
            });
        }

        public enum AnimationType
        {
            Appear,
            Disappear
        }
    }

    public static class CircularRevealExtensions
    {
        public static CircularRevealAnimation AnimateWithCircularReveal(this View view)
            => new CircularRevealAnimation(view);
    }
}
