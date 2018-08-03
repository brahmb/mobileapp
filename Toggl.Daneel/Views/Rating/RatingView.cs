using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CoreGraphics;
using Foundation;
using MvvmCross.Commands;
using MvvmCross.Platforms.Ios.Binding.Views;
using ObjCRuntime;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;
using static Toggl.Daneel.Extensions.UIKitRxExtensions;
using static Toggl.Multivac.Extensions.CommonFunctions;

namespace Toggl.Daneel
{
    public partial class RatingView : MvxView, IReactiveBindingHolder
    {
        private readonly UIStringAttributes descriptionStringAttributes = new UIStringAttributes
        {
            ParagraphStyle = new NSMutableParagraphStyle
            {
                MaximumLineHeight = 22,
                MinimumLineHeight = 22,
                Alignment = UITextAlignment.Center,
            }
        };

        private NSLayoutConstraint heightConstraint;

        public IMvxCommand CTATappedCommand { get; set; }
        public IMvxCommand DismissTappedCommand { get; set; }
        public IMvxCommand<bool> ImpressionTappedCommand { get; set; }

        public new RatingViewModel DataContext
        {
            get => base.DataContext as RatingViewModel;
            set
            {
                base.DataContext = value;
                updateBindings();
            }
        }

        public CompositeDisposable DisposeBag { get; } = new CompositeDisposable();

        public RatingView (IntPtr handle) : base (handle)
        {
        }

        public static RatingView Create()
        {
            var arr = NSBundle.MainBundle.LoadNib(nameof(RatingView), null, null);
            return Runtime.GetNSObject<RatingView>(arr.ValueAt(0));
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            heightConstraint = HeightAnchor.ConstraintEqualTo(1);
            heightConstraint.Active = true;
        }

        private void updateBindings()
        {
            this.Bind(DataContext.CtaTitle, CtaTitle.BindText());
            this.Bind(DataContext.CtaButtonTitle, CtaButton.BindTitle());
            this.Bind(
                DataContext.Impression.Select(impression => impression.HasValue),
                CtaView.BindIsVisibleWithFade());
            this.Bind(
                DataContext.CtaDescription.Select(attributedDescription),
                CtaDescription.BindAttributedText());
            this.Bind(
                DataContext
                    .Impression
                    .Select(impression => impression.HasValue)
                    .Select(Invert),
                QuestionView.BindIsVisibleWithFade());
            this.Bind(
                DataContext
                    .Impression
                    .Select(impression => impression.HasValue)
                    .Select(gotImpression => (nfloat)(gotImpression ? 289 : 262)),
                heightConstraint.BindConstant());

            this.BindVoid(YesView.Tapped(), () => DataContext.RegisterImpression(true));
            this.BindVoid(NotReallyView.Tapped(), () => DataContext.RegisterImpression(false));
            this.Bind(CtaButton.Tapped(), DataContext.PerformMainAction);
            this.BindVoid(CtaButton.Tapped(), DataContext.Dismiss);
            this.BindVoid(DismissButton.Tapped(), DataContext.Dismiss);
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            SetupAsCard(QuestionView);
            SetupAsCard(CtaView);

            CtaButton.Layer.CornerRadius = 8;
            CtaView.Layer.MasksToBounds = false;
        }

        private NSAttributedString attributedDescription(string text)
            => new NSAttributedString(text, descriptionStringAttributes);

        private void SetupAsCard(UIView view)
        {
            var shadowPath = UIBezierPath.FromRect(view.Bounds);
            view.Layer.ShadowPath?.Dispose();
            view.Layer.ShadowPath = shadowPath.CGPath;

            view.Layer.CornerRadius = 8;
            view.Layer.ShadowRadius = 4;
            view.Layer.ShadowOpacity = 0.1f;
            view.Layer.MasksToBounds = false;
            view.Layer.ShadowOffset = new CGSize(0, 2);
            view.Layer.ShadowColor = UIColor.Black.CGColor;
        }
    }
}
