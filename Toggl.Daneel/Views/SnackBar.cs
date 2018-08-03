using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Toggl.Foundation.MvvmCross.Extensions;
using System.Threading;
using CoreGraphics;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace Toggl.Daneel
{
    public partial class SnackBar : UIView
    {
        private const int yAnimationOffset = 100;
        private const int cornerRadius = 10;

        private enum ButtonPositionType
        {
            Right,
            Bottom
        }

        public NSLayoutYAxisAnchor SnackBottomAnchor
        {
            get => bottomAnchor;
            set
            {
                bottomAnchor = value;
                SetNeedsUpdateConstraints();
            }
        }

        public UIStringAttributes TextAttributes { get; set; }
        public UIStringAttributes ButtonAttributes { get; set; }

        private NSLayoutYAxisAnchor bottomAnchor;
        private NSLayoutConstraint bottomConstraint;
        private ButtonPositionType buttonPosition = ButtonPositionType.Right;

        private Subject<Unit> timerSubject = new Subject<Unit>();
        private IDisposable timerDisposable;
        private Action timerAction;

        private bool showing = false;
        private string text;

        public SnackBar (IntPtr handle) : base (handle)
        {
        }

        public static SnackBar Create(string text)
        {
            var arr = NSBundle.MainBundle.LoadNib("SnackBar", null, null);
            var snackBar = Runtime.GetNSObject<SnackBar>(arr.ValueAt(0));

            snackBar.TextAttributes = new UIStringAttributes{
                ForegroundColor = snackBar.label.TextColor,
                Font = snackBar.label.Font
            };

            snackBar.ButtonAttributes = new UIStringAttributes{
                ForegroundColor = snackBar.label.TextColor,
                Font = UIFont.SystemFontOfSize(14, UIFontWeight.Semibold)
            };

            snackBar.text = text;
            snackBar.configure();
            return snackBar;
        }

        public void AddButton(String title, Action onTap)
        {
            var button = new UIButton(UIButtonType.Plain);
            button.TouchUpInside += (sender, e) =>
            {
                timerDisposable.Dispose();
                Hide(false);
                onTap();
            };
            button.SetAttributedTitle(new NSAttributedString(title, ButtonAttributes), UIControlState.Normal);
            button.SetContentHuggingPriority(1000, UILayoutConstraintAxis.Vertical);
            button.SetContentHuggingPriority(1000, UILayoutConstraintAxis.Horizontal);
            buttonsStackView.AddArrangedSubview(button);

            SetNeedsLayout();
        }

        public void SetTimer(double seconds, Action onTimer)
        {
            timerAction = onTimer;

            timerDisposable = timerSubject
                .Delay(TimeSpan.FromSeconds(seconds))
                .ObserveOn(SynchronizationContext.Current)
                .VoidSubscribe(() =>
                {
                    Hide(true);
                });
        }

        public void Show(UIView superView)
        {
            if (Superview != null) // Only show it once
            {
                return;
            }

            if (showing) return;

            showing = true;
            Alpha = 0;
            superView.AddSubview(this);
            TranslatesAutoresizingMaskIntoConstraints = false;
            LeadingAnchor.ConstraintEqualTo(Superview.LeadingAnchor, 10).Active = true;
            TrailingAnchor.ConstraintEqualTo(Superview.TrailingAnchor, -10).Active = true;

            label.AttributedText = new NSAttributedString(text, TextAttributes);

            SetNeedsUpdateConstraints();
            SetNeedsLayout();

            Transform = CGAffineTransform.MakeTranslation(0, yAnimationOffset);
            UIView.Animate(
                0.3, () =>
                {
                    Transform = CGAffineTransform.MakeIdentity();
                    Alpha = 1;
                }
            );

            startTimer();
        }

        public void Hide(bool execute = true)
        {
            if (!showing) return;

            showing = false;
            UIView.Animate(
                0.3,
                () =>
                {
                    Transform = CGAffineTransform.MakeTranslation(0, yAnimationOffset);
                    Alpha = 0;
                },
                () =>
                {
                    RemoveFromSuperview();

                    if (execute && timerAction != null)
                    {
                        timerAction.Invoke();
                    }

                    stopTimer();
                }
            );
        }

        public override void UpdateConstraints()
        {
            if (bottomConstraint != null)
                Superview.RemoveConstraint(bottomConstraint);

            bottomConstraint = BottomAnchor.ConstraintEqualTo(bottomAnchor ?? Superview.BottomAnchor, -10);
            bottomConstraint.Active = true;

            base.UpdateConstraints();
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            if (buttonPosition == ButtonPositionType.Right)
            {
                LayoutIfNeeded(); // Seems silly, but I need this line so the label returns the correct width
                var labelWidth = label.Frame.Width;
                var requiredWidth = ((NSString)label.Text).GetBoundingRect(new CGSize(float.MaxValue, float.MaxValue),
                    NSStringDrawingOptions.UsesDeviceMetrics, TextAttributes, null).Width;
                if (labelWidth < requiredWidth)
                {
                    buttonPosition = ButtonPositionType.Bottom;
                    configureLayout();
                    LayoutIfNeeded(); // Seems silly, but this one is needed too
                }
            }
        }

        private void configure()
        {
            Layer.CornerRadius = cornerRadius;
            configureLayout();
        }

        private void configureLayout()
        {
            switch (buttonPosition)
            {
                case ButtonPositionType.Right:
                    stackView.Axis = UILayoutConstraintAxis.Horizontal;
                    buttonsStackView.Axis = UILayoutConstraintAxis.Vertical;
                    break;
                case ButtonPositionType.Bottom:
                    stackView.Axis = UILayoutConstraintAxis.Vertical;
                    buttonsStackView.Axis = UILayoutConstraintAxis.Horizontal;
                    break;
            }

            SetNeedsLayout();
        }

        private void startTimer()
        {
            timerSubject.OnNext(Unit.Default);
        }

        private void stopTimer()
        {
            timerDisposable.Dispose();
            timerAction = null;
        }

        public static SnackBar Undo(Action onTap, Action onTimer)
        {
            var snackBar = SnackBar.Create("Time entry was deleted");
            snackBar.AddButton("UNDO", onTap);
            snackBar.SetTimer(5, onTimer);
            return snackBar;
        }
    }
}
