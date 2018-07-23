using CoreText;
using Foundation;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Ios.Binding;
using MvvmCross.Platforms.Ios.Presenters.Attributes;
using MvvmCross.Plugin.Visibility;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.Converters;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;
using static Toggl.Daneel.Extensions.ViewBindingExtensions;

namespace Toggl.Daneel.ViewControllers
{
    [MvxRootPresentation(WrapInNavigationController = true)]
    public partial class TokenResetViewController : KeyboardAwareViewController<TokenResetViewModel>
    {
        private const int forgotPasswordLabelOffset = 27;

        private readonly UIBarButtonItem nextButton =
            new UIBarButtonItem { Title = Resources.LoginNextButton, TintColor = UIColor.White };

        public TokenResetViewController()
            : base(nameof(TokenResetViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            Title = Resources.LoginTitle;

            prepareViews();

            var invertedBoolConverter = new BoolToConstantValueConverter<bool>(false, true);

            var bindingSet = this.CreateBindingSet<TokenResetViewController, TokenResetViewModel>();

            //Text
            bindingSet.Bind(ErrorLabel).To(vm => vm.Error);
            bindingSet.Bind(PasswordTextField)
                      .For(v => v.BindSecureTextEntry())
                      .To(vm => vm.IsPasswordMasked);

            bindingSet.Bind(EmailLabel)
                      .To(vm => vm.Email)
                      .WithConversion(new EmailToStringValueConverter());

            bindingSet.Bind(PasswordTextField)
                      .To(vm => vm.Password)
                      .WithConversion(new PasswordToStringValueConverter());

            //Commands
            bindingSet.Bind(SignOutButton).To(vm => vm.SignOutCommand);

            bindingSet.Bind(nextButton)
                      .For(v => v.BindCommand())
                      .To(vm => vm.DoneCommand);

            bindingSet.Bind(ShowPasswordButton)
                      .For(v => v.BindTap())
                      .To(vm => vm.TogglePasswordVisibilityCommand);

            //Enabled
            bindingSet.Bind(PasswordTextField)
                      .For(v => v.BindShouldReturn())
                      .To(vm => vm.DoneCommand);

            bindingSet.Bind(nextButton)
                      .For(v => v.Enabled)
                      .To(vm => vm.NextIsEnabled);

            //Visibility
            bindingSet.Bind(ErrorView)
                      .For(v => v.BindVisible())
                      .To(vm => vm.HasError);

            bindingSet.Bind(ShowPasswordButton)
                      .For(v => v.BindVisibility())
                      .To(vm => vm.IsLoading)
                      .WithConversion(new MvxInvertedVisibilityValueConverter());

            bindingSet.Bind(ActivityIndicatorView)
                      .For(v => v.BindVisible())
                      .To(vm => vm.IsLoading);

            //State
            bindingSet.Apply();

            PasswordTextField.BecomeFirstResponder();
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            NavigationController.NavigationBar.UserInteractionEnabled = true;
        }

        protected override void KeyboardWillShow(object sender, UIKeyboardEventArgs e)
        {
            BottomConstraint.Constant = e.FrameEnd.Height + forgotPasswordLabelOffset;
            UIView.Animate(Animation.Timings.EnterTiming, () => View.LayoutIfNeeded());
        }

        protected override void KeyboardWillHide(object sender, UIKeyboardEventArgs e)
        {
            BottomConstraint.Constant = forgotPasswordLabelOffset;
            UIView.Animate(Animation.Timings.EnterTiming, () => View.LayoutIfNeeded());
        }

        private void prepareViews()
        {
            prepareTextFields();
            prepareNavigationBar();
        }

        private void prepareTextFields()
        {
            var placeholderAttributes = new CTStringAttributes(
                new UIStringAttributes { ForegroundColor = UIColor.White.ColorWithAlpha(0.5f) }.Dictionary
            );

            PasswordTextField.TintColor = UIColor.White;
            PasswordTextField.AttributedPlaceholder =
                new NSAttributedString(Resources.LoginSignUpPasswordPlaceholder, placeholderAttributes);
        }

        private void prepareNavigationBar()
        {
            var attributes = new UITextAttributes { Font = UIFont.SystemFontOfSize(14, UIFontWeight.Medium) };
            var spaceFix = new UIBarButtonItem(UIBarButtonSystemItem.FixedSpace) { Width = 8 };

            NavigationItem.RightBarButtonItems = new[] { spaceFix, nextButton };

            nextButton.SetTitleTextAttributes(attributes, UIControlState.Normal);
            nextButton.SetTitleTextAttributes(attributes, UIControlState.Disabled);
        }
    }
}

