using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
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
        private Func<Task> startTimer;
        private bool showing = false;

        public SnackBar (IntPtr handle) : base (handle)
        {
        }

        public static SnackBar Create()
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

            snackBar.configure();
            return snackBar;
        }

        public void AddButton(String title, Action onTap)
        {
            var button = new UIButton(UIButtonType.Plain);
            button.TouchUpInside += (sender, e) =>
            {
                Hide();
                onTap();
            };
            button.SetAttributedTitle(new NSAttributedString(title, ButtonAttributes), UIControlState.Normal);
            button.SetContentHuggingPriority(1000, UILayoutConstraintAxis.Vertical);
            button.SetContentHuggingPriority(1000, UILayoutConstraintAxis.Horizontal);
            buttonsStackView.AddArrangedSubview(button);

            SetNeedsLayout();
        }

        public void SetTimer(float seconds, Action onTimer)
        {
            startTimer = async () =>
            {
                await Task.Delay((int) seconds * 1000);
                if (!showing) return;
                Hide();
                onTimer();
            };
        }

        public void Show(string text, UIView superView)
        {
            if (Superview != null) // Only show it once
            {
                return;
            }

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

            startTimer?.Invoke();
        }

        public void Hide()
        {
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
    }
}
