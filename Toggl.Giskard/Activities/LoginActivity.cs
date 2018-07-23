using System.Linq;
using System.Reactive.Linq;
using Android.App;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Views;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.Extensions;
using Toggl.Multivac;

namespace Toggl.Giskard.Activities
{
    [MvxActivityPresentation]
    [Activity(Theme = "@style/AppTheme",
              ScreenOrientation = ScreenOrientation.Portrait,
              WindowSoftInputMode = SoftInput.AdjustPan | SoftInput.StateHidden,
              ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public sealed partial class LoginActivity : ReactiveActivity<LoginViewModel>
    {
        protected override void OnCreate(Bundle bundle)
        {
            this.ChangeStatusBarColor(Color.White, true);

            base.OnCreate(bundle);
            SetContentView(Resource.Layout.LoginActivity);
            OverridePendingTransition(Resource.Animation.abc_slide_in_bottom, Resource.Animation.abc_fade_out);

            InitializeViews();

            emailEditText.Text = ViewModel.Email.FirstAsync().GetAwaiter().GetResult();
            passwordEditText.Text = ViewModel.Password.FirstAsync().GetAwaiter().GetResult();

            //Text
            this.Bind(ViewModel.ErrorMessage, errorTextView.BindText());
            this.Bind(emailEditText.Text().Select(Email.From), ViewModel.SetEmail);
            this.Bind(passwordEditText.Text().Select(Password.From), ViewModel.SetPassword);
            this.Bind(ViewModel.IsLoading.Select(loginButtonTitle), loginButton.BindText());

            //Visibility
            this.Bind(ViewModel.HasError, errorTextView.BindIsVisible());
            this.Bind(ViewModel.IsLoading, progressBar.BindIsVisible());
            this.Bind(ViewModel.LoginEnabled, loginButton.BindEnabled());

            //Commands
            this.Bind(signupCard.Tapped(), ViewModel.Signup);
            this.BindVoid(loginButton.Tapped(), ViewModel.Login);
            this.BindVoid(googleLoginButton.Tapped(), ViewModel.GoogleLogin);
            this.Bind(forgotPasswordView.Tapped(), ViewModel.ForgotPassword);

            string loginButtonTitle(bool isLoading)
                => isLoading ? "" : Resources.GetString(Resource.String.Login);
        }
    }
}
