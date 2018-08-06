using MvvmCross.Binding;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Ios.Binding;
using MvvmCross.Platforms.Ios.Views;
using MvvmCross.Platforms.Ios.Presenters.Attributes;
using MvvmCross.Plugin.Color.Platforms.Ios;
using MvvmCross.Plugin.Visibility;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.Converters;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;
using static Toggl.Daneel.Extensions.ViewBindingExtensions;
using static Toggl.Daneel.Extensions.LoginSignupViewExtensions;
using static Toggl.Daneel.Extensions.ViewExtensions;

namespace Toggl.Daneel.ViewControllers
{
    [MvxRootPresentation(WrapInNavigationController = true)]
    public sealed partial class SignupViewController : MvxViewController<SignupViewModel>
    {
        private const int iPhoneSeScreenHeight = 568;

        private bool keyboardIsOpen = false;

        private const int topConstraintForBiggerScreens = 72;
        private const int topConstraintForBiggerScreensWithKeyboard = 40;

        private const int emailTopConstraint = 42;
        private const int emailTopConstraintWithKeyboard = 12;

        public SignupViewController() : base(nameof(SignupViewController), null)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var signupButtonTitleConverter = new BoolToConstantValueConverter<string>("", Resources.SignUpTitle);
            var invertedVisibilityConverter = new MvxInvertedVisibilityValueConverter();

            var bindingSet = this.CreateBindingSet<SignupViewController, SignupViewModel>();

            UIKeyboard.Notifications.ObserveWillShow(KeyboardWillShow);
            UIKeyboard.Notifications.ObserveWillHide(KeyboardWillHide);

            //Text
            bindingSet.Bind(ErrorLabel).To(vm => vm.ErrorText);
            bindingSet.Bind(EmailTextField)
                      .To(vm => vm.Email)
                      .WithConversion(new EmailToStringValueConverter());

            bindingSet.Bind(PasswordTextField)
                      .To(vm => vm.Password)
                      .WithConversion(new PasswordToStringValueConverter());

            bindingSet.Bind(SignupButton)
                      .For(v => v.BindAnimatedTitle())
                      .To(vm => vm.IsLoading)
                      .WithConversion(signupButtonTitleConverter);

            bindingSet.Bind(SelectCountryButton)
                      .For(v => v.BindAnimatedTitle())
                      .To(vm => vm.CountryButtonTitle);

            //Commands
            bindingSet.Bind(SignupButton).To(vm => vm.SignupCommand);
            bindingSet.Bind(GoogleSignupButton).To(vm => vm.GoogleSignupCommand);
            bindingSet.Bind(ShowPasswordButton).To(vm => vm.TogglePasswordVisibilityCommand);
            bindingSet.Bind(SelectCountryButton).To(vm => vm.PickCountryCommand);
            bindingSet.Bind(LoginCard)
                      .For(v => v.BindTap())
                      .To(vm => vm.LoginCommand);

            //Visibility
            bindingSet.Bind(ErrorLabel)
                      .For(v => v.BindAnimatedVisibility())
                      .To(vm => vm.HasError);

            bindingSet.Bind(ActivityIndicator)
                      .For(v => v.BindVisibilityWithFade())
                      .To(vm => vm.IsLoading);

            bindingSet.Bind(PasswordTextField)
                      .For(v => v.BindSecureTextEntry())
                      .To(vm => vm.IsPasswordMasked);

            bindingSet.Bind(ShowPasswordButton)
                      .For(v => v.BindVisible())
                      .To(vm => vm.IsShowPasswordButtonVisible);

            bindingSet.Bind(PasswordTextField)
                      .For(v => v.BindFirstResponder())
                      .To(vm => vm.IsShowPasswordButtonVisible)
                      .Mode(MvxBindingMode.OneWayToSource);

            bindingSet.Bind(CountryNotSelectedImageView)
                      .For(v => v.BindAnimatedVisibility())
                      .To(vm => vm.IsCountryErrorVisible);

            bindingSet.Apply();

            prepareViews();
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            if (View.Frame.Height > iPhoneSeScreenHeight && !keyboardIsOpen)
                TopConstraint.Constant = topConstraintForBiggerScreens;

            LoginCard.SetupBottomCard();
            GoogleSignupButton.SetupGoogleButton();
        }

        private void KeyboardWillShow(object sender, UIKeyboardEventArgs e)
        {
            keyboardIsOpen = true;
            if (View.Frame.Height > iPhoneSeScreenHeight)
            {
                TopConstraint.Constant = topConstraintForBiggerScreensWithKeyboard;
            }
            else
            {
                EmailFieldTopConstraint.Constant = emailTopConstraintWithKeyboard;
            }
            UIView.Animate(Animation.Timings.EnterTiming, () => View.LayoutIfNeeded());
        }

        private void KeyboardWillHide(object sender, UIKeyboardEventArgs e)
        {
            keyboardIsOpen = false;
            if (View.Frame.Height > iPhoneSeScreenHeight)
            {
                TopConstraint.Constant = topConstraintForBiggerScreens;
            }
            else
            {
                EmailFieldTopConstraint.Constant = emailTopConstraint;
            }
            UIView.Animate(Animation.Timings.EnterTiming, () => View.LayoutIfNeeded());
        }

        private void prepareViews()
        {
            NavigationController.NavigationBarHidden = true;

            ActivityIndicator.Alpha = 0;
            ActivityIndicator.StartAnimation();

            SignupButton.SetTitleColor(
                Color.Login.DisabledButtonColor.ToNativeColor(),
                UIControlState.Disabled
            );

            EmailTextField.ShouldReturn += _ => {
                PasswordTextField.BecomeFirstResponder();
                return false;
            };

            PasswordTextField.ShouldReturn += _ =>
            {
                ViewModel.SignupCommand.Execute();
                PasswordTextField.ResignFirstResponder();
                return false;
            };

            View.AddGestureRecognizer(new UITapGestureRecognizer(() =>
            {
                EmailTextField.ResignFirstResponder();
                PasswordTextField.ResignFirstResponder();
            }));

            SignupShakeTriggerButton.TouchUpInside += (sender, e) =>
            {
                if (!ViewModel.Email.IsValid)
                {
                    EmailTextField.Shake();
                }
                if (!ViewModel.Password.IsValid)
                {
                    PasswordTextField.Shake();
                }
                if (!ViewModel.IsCountryValid)
                {
                    SelectCountryButton.Shake();
                    CountryNotSelectedImageView.Shake();
                    CountryDropDownCaretImageView.Shake();
                }
            };

            ShowPasswordButton.SetupShowPasswordButton();

            EmailTextField.ResignFirstResponder();
            PasswordTextField.ResignFirstResponder();
        }
    }
}

