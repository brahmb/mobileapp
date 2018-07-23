using MvvmCross.Binding;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Ios.Presenters.Attributes;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.Converters;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;
using static Toggl.Daneel.Extensions.ViewBindingExtensions;

namespace Toggl.Daneel.ViewControllers
{
    [MvxChildPresentation]
    public sealed partial class ForgotPasswordViewController
        : KeyboardAwareViewController<ForgotPasswordViewModel>
    {
        private const int distanceFromTop = 136;
        private const int backButtonFontSize = 14;
        private const int iPhoneSeScreenHeight = 568;
        private const int resetButtonBottomSpacing = 32;

        private bool viewInitialized;

        public ForgotPasswordViewController() : base(nameof(ForgotPasswordViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            Title = Resources.LoginForgotPassword;
            
            prepareViews();

            var boolInverter = new BoolToConstantValueConverter<bool>(false, true);
            var resetPasswordButtonTitleConverter = new BoolToConstantValueConverter<string>("", Resources.GetPasswordResetLink);

            var bindingSet = this.CreateBindingSet<ForgotPasswordViewController, ForgotPasswordViewModel>();

            //Text
            bindingSet.Bind(ErrorLabel).To(vm => vm.ErrorMessage);
            bindingSet.Bind(EmailTextField)
                      .To(vm => vm.Email)
                      .WithConversion(new EmailToStringValueConverter());

            bindingSet.Bind(ResetPasswordButton)
                      .For(v => v.BindAnimatedTitle())
                      .To(vm => vm.IsLoading)
                      .WithConversion(resetPasswordButtonTitleConverter);

            //Visibility
            bindingSet.Bind(ErrorLabel)
                      .For(v => v.BindAnimatedVisibility())
                      .To(vm => vm.HasError);

            bindingSet.Bind(DoneCard)
                      .For(v => v.BindVisibilityWithFade())
                      .To(vm => vm.PasswordResetSuccessful);

            bindingSet.Bind(ResetPasswordButton)
                      .For(v => v.BindVisibilityWithFade())
                      .To(vm => vm.PasswordResetSuccessful)
                      .WithConversion(boolInverter);

            bindingSet.Bind(EmailTextField)
                      .For(v => v.BindFirstResponder())
                      .To(vm => vm.PasswordResetSuccessful)
                      .Mode(MvxBindingMode.OneWay)
                      .WithConversion(boolInverter);

            bindingSet.Bind(ActivityIndicator)
                      .For(v => v.BindVisibilityWithFade())
                      .To(vm => vm.IsLoading);

            //Commands
            bindingSet.Bind(ResetPasswordButton).To(vm => vm.ResetCommand);

            bindingSet.Apply();
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            if (viewInitialized) return;

            viewInitialized = true;

            if (View.Frame.Height > iPhoneSeScreenHeight)
                TopConstraint.Constant = distanceFromTop;
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            EmailTextField.BecomeFirstResponder();
        }

        protected override void KeyboardWillShow(object sender, UIKeyboardEventArgs e)
        {
            ResetPasswordButtonBottomConstraint.Constant = e.FrameEnd.Height + resetButtonBottomSpacing;
            UIView.Animate(Animation.Timings.EnterTiming, () => View.LayoutIfNeeded());
        }

        protected override void KeyboardWillHide(object sender, UIKeyboardEventArgs e)
        {
            ResetPasswordButtonBottomConstraint.Constant = resetButtonBottomSpacing;
            UIView.Animate(Animation.Timings.EnterTiming, () => View.LayoutIfNeeded());
        }

        private void prepareViews()
        {
            NavigationController.NavigationBarHidden = false;

            ResetPasswordButton.SetTitleColor(
                Color.Login.DisabledButtonColor.ToNativeColor(),
                UIControlState.Disabled
            );

            EmailTextField.ShouldReturn = _ =>
            {
                ViewModel.ResetCommand.Execute();
                return false;
            };

            ActivityIndicator.StartAnimation();

            prepareBackbutton();
        }

        private void prepareBackbutton()
        {
            var image = UIImage
                .FromBundle("icBackNoPadding")
                .ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
            var color = Color.NavigationBar.BackButton.ToNativeColor();
            var backButton = new UIButton();
            backButton.TintColor = color;
            backButton.SetImage(image, UIControlState.Normal);
            backButton.SetTitleColor(color, UIControlState.Normal);
            backButton.SetTitle(Resources.Back, UIControlState.Normal);
            backButton.TitleLabel.Font = UIFont.SystemFontOfSize(backButtonFontSize, UIFontWeight.Medium);

            backButton.TouchUpInside += (sender, e) =>
            {
                ViewModel.CloseCommand.Execute();
            };

            //Spacing between button image and title
            var spacing = 6;
            backButton.ImageEdgeInsets = new UIEdgeInsets(0, 0, 0, spacing);
            backButton.TitleEdgeInsets = new UIEdgeInsets(0, spacing, 0, 0);

            NavigationItem.HidesBackButton = true;
            NavigationItem.LeftItemsSupplementBackButton = false;
            NavigationItem.SetLeftBarButtonItem(new UIBarButtonItem(backButton), true);

            //Otherwise title gets clipped
            var frame = backButton.Frame;
            frame.Width = 90;
            backButton.Frame = frame;
            backButton.HorizontalAlignment = UIControlContentHorizontalAlignment.Left;
        }
    }
}
