using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels;
using System.Reactive.Linq;
using Toggl.Daneel.Views;
using Toggl.Multivac.Extensions;
using UIKit;

namespace Toggl.Daneel.ViewControllers.Settings
{
    [ModalCardPresentation]
    public sealed partial class SendFeedbackViewController : ReactiveViewController<SendFeedbackViewModel>
    {

        public SendFeedbackViewController()
            : base(nameof(SendFeedbackViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            prepareViews();
            prepareIndicatorView();

            this.Bind(CloseButton.Tapped(), ViewModel.CloseButtonTapped);
            this.Bind(FeedbackTextView.Text(), ViewModel.FeedbackText);
            this.Bind(ErrorView.Tapped(), ViewModel.ErrorViewTapped);

            this.Bind(SendButton.Tapped(), ViewModel.SendButtonTapped);
            SendButton.TouchUpInside += (sender, args) => { FeedbackTextView.ResignFirstResponder(); };

            this.Bind(ViewModel.IsFeedbackEmpty, FeedbackPlaceholderTextView.BindIsVisible());
            this.Bind(ViewModel.ErrorViewVisible, ErrorView.BindAnimatedIsVisible());
            this.Bind(ViewModel.SendEnabled, SendButton.BindIsEnabled());

            var isLoading = ViewModel.IsLoading.AsDriver(false);
            this.Bind(isLoading.Invert(), SendButton.BindIsVisible());
            this.Bind(isLoading, IndicatorView.BindIsVisible());
            this.Bind(isLoading, UIApplication.SharedApplication.BindNetworkActivityIndicatorVisible());
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            IndicatorView.StartAnimation();
        }

        private void prepareViews()
        {
            ErrorView.Hidden = true;
            FeedbackTextView.TintColor = Color.Feedback.Cursor.ToNativeColor();
            FeedbackPlaceholderTextView.TintColor = Color.Feedback.Cursor.ToNativeColor();
        }

        private void prepareIndicatorView()
        {
            IndicatorView.Image = IndicatorView.Image.ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
            IndicatorView.TintColor = Color.Feedback.ActivityIndicator.ToNativeColor();
            IndicatorView.Hidden = true;
        }
    }
}

