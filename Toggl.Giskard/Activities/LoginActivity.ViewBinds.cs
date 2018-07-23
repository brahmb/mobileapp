using System;
using Android.Views;
using Android.Widget;

namespace Toggl.Giskard.Activities
{
    public sealed partial class LoginActivity
    {
        private Button loginButton;

        private View signupCard;
        private View googleLoginButton;
        private View forgotPasswordView;

        private EditText emailEditText;
        private EditText passwordEditText;

        private TextView errorTextView;

        private ProgressBar progressBar;

        protected override void InitializeViews()
        {
            signupCard = FindViewById(Resource.Id.LoginSignupCardView);
            errorTextView = FindViewById<TextView>(Resource.Id.LoginError);
            loginButton = FindViewById<Button>(Resource.Id.LoginLoginButton);
            forgotPasswordView = FindViewById(Resource.Id.LoginForgotPassword);
            googleLoginButton = FindViewById<View>(Resource.Id.LoginGoogleLogin);
            progressBar = FindViewById<ProgressBar>(Resource.Id.LoginProgressBar);
            emailEditText = FindViewById<EditText>(Resource.Id.LoginEmailEditText);
            passwordEditText = FindViewById<EditText>(Resource.Id.LoginPasswordEditText);
        }
    }
}
