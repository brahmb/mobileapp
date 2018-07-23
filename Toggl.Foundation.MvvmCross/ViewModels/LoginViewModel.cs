using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Exceptions;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Login;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave.Exceptions;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class LoginViewModel : MvxViewModel<CredentialsParameter>
    {
        [Flags]
        public enum ShakeTargets
        {
            None = 0,
            Email = 1,
            Password = 2
        }

        private readonly ILoginManager loginManager;
        private readonly IAnalyticsService analyticsService;
        private readonly IOnboardingStorage onboardingStorage;
        private readonly IMvxNavigationService navigationService;
        private readonly IPasswordManagerService passwordManagerService;
        private readonly IErrorHandlingService errorHandlingService;
        private readonly ILastTimeUsageStorage lastTimeUsageStorage;
        private readonly ITimeService timeService;

        private IDisposable loginDisposable;

        private readonly Subject<ShakeTargets> shakeSubject = new Subject<ShakeTargets>();
        private readonly Subject<bool> isShowPasswordButtonVisibleSubject = new Subject<bool>();
        private readonly BehaviorSubject<bool> isLoadingSubject = new BehaviorSubject<bool>(false);
        private readonly BehaviorSubject<string> errorMessageSubject = new BehaviorSubject<string>("");
        private readonly BehaviorSubject<bool> isPasswordMaskedSubject = new BehaviorSubject<bool>(true);
        private readonly BehaviorSubject<Email> emailSubject = new BehaviorSubject<Email>(Multivac.Email.Empty);
        private readonly BehaviorSubject<Password> passwordSubject = new BehaviorSubject<Password>(Multivac.Password.Empty);

        public bool IsPasswordManagerAvailable { get; }

        public IObservable<string> Email { get; }

        public IObservable<string> Password { get; }

        public IObservable<bool> HasError { get; }

        public IObservable<bool> IsLoading { get; }

        public IObservable<bool> LoginEnabled { get; }

        public IObservable<ShakeTargets> Shake { get; }
        
        public IObservable<string> ErrorMessage { get; }

        public IObservable<bool> IsPasswordMasked { get; }

        public IObservable<bool> IsShowPasswordButtonVisible { get; }

        public LoginViewModel(
            ILoginManager loginManager,
            IAnalyticsService analyticsService,
            IOnboardingStorage onboardingStorage,
            IMvxNavigationService navigationService,
            IPasswordManagerService passwordManagerService,
            IErrorHandlingService errorHandlingService,
            ILastTimeUsageStorage lastTimeUsageStorage,
            ITimeService timeService)
        {
            Ensure.Argument.IsNotNull(loginManager, nameof(loginManager));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(onboardingStorage, nameof(onboardingStorage));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(passwordManagerService, nameof(passwordManagerService));
            Ensure.Argument.IsNotNull(errorHandlingService, nameof(errorHandlingService));
            Ensure.Argument.IsNotNull(lastTimeUsageStorage, nameof(lastTimeUsageStorage));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));

            this.timeService = timeService;
            this.loginManager = loginManager;
            this.analyticsService = analyticsService;
            this.onboardingStorage = onboardingStorage;
            this.navigationService = navigationService;
            this.errorHandlingService = errorHandlingService;
            this.lastTimeUsageStorage = lastTimeUsageStorage;
            this.passwordManagerService = passwordManagerService;

            var passwordObservable = passwordSubject.AsObservable();
            var emailObservable = emailSubject.AsObservable().Select(email => email.TrimmedEnd());

            Shake = shakeSubject.AsObservable();

            Email = emailObservable
                .Select(email => email.ToString())
                .DistinctUntilChanged();

            Password = passwordSubject
                .Select(password => password.ToString())
                .DistinctUntilChanged();
            
            IsLoading = isLoadingSubject
                .AsObservable()
                .DistinctUntilChanged();

            ErrorMessage = errorMessageSubject
                .AsObservable()
                .DistinctUntilChanged();

            IsPasswordMasked = isPasswordMaskedSubject
                .AsObservable()
                .DistinctUntilChanged();

            IsShowPasswordButtonVisible = Password
                .Select(password => password.Length > 1)
                .CombineLatest(isShowPasswordButtonVisibleSubject.AsObservable(), CommonFunctions.And)
                .DistinctUntilChanged();

            HasError = ErrorMessage
                .Select(string.IsNullOrEmpty)
                .Select(CommonFunctions.Invert);

            LoginEnabled = emailObservable
                .CombineLatest(
                    passwordObservable,
                    IsLoading,
                    (email, password, isLoading) => email.IsValid && password.IsValid && !isLoading)
                .DistinctUntilChanged();
            
            IsPasswordManagerAvailable = passwordManagerService.IsAvailable;
        }

        public override void Prepare(CredentialsParameter parameter)
        {
            emailSubject.OnNext(parameter.Email);
            passwordSubject.OnNext(parameter.Password);
        }

        public void SetEmail(Email email)
            => emailSubject.OnNext(email);

        public void SetPassword(Password password)
            => passwordSubject.OnNext(password);

        public void SetIsShowPasswordButtonVisible(bool visible)
            => isShowPasswordButtonVisibleSubject.OnNext(visible);

        public void Login()
        {
            var shakeTargets = ShakeTargets.None;
            shakeTargets |= emailSubject.Value.IsValid ? ShakeTargets.None : ShakeTargets.Email;
            shakeTargets |= passwordSubject.Value.IsValid ? ShakeTargets.None : ShakeTargets.Password;

            if (shakeTargets != ShakeTargets.None)
            {
                shakeSubject.OnNext(shakeTargets);
                return;
            }

            if (isLoadingSubject.Value) return;

            isLoadingSubject.OnNext(true);
            errorMessageSubject.OnNext("");

            loginDisposable =
                loginManager
                    .Login(emailSubject.Value, passwordSubject.Value)
                    .Track(analyticsService.Login, AuthenticationMethod.EmailAndPassword)
                    .Subscribe(onDataSource, onError, onCompleted);
        }

        public async Task StartPasswordManager()
        {
            if (!passwordManagerService.IsAvailable) return;
            if (isLoadingSubject.Value) return;

            analyticsService.PasswordManagerButtonClicked.Track();

            var loginInfo = await passwordManagerService.GetLoginInformation();

            emailSubject.OnNext(loginInfo.Email);
            if (!emailSubject.Value.IsValid) return;
            analyticsService.PasswordManagerContainsValidEmail.Track();

            passwordSubject.OnNext(loginInfo.Password);
            if (!passwordSubject.Value.IsValid) return;
            analyticsService.PasswordManagerContainsValidPassword.Track();

            Login();
        }

        public void TogglePasswordVisibility()
            => isPasswordMaskedSubject.OnNext(!isPasswordMaskedSubject.Value);

        public async Task ForgotPassword()
        {
            if (isLoadingSubject.Value) return;

            var emailParameter = EmailParameter.With(emailSubject.Value);
            emailParameter = await navigationService
                .Navigate<ForgotPasswordViewModel, EmailParameter, EmailParameter>(emailParameter);
            if (emailParameter != null)
                emailSubject.OnNext(emailParameter.Email);
        }

        public void GoogleLogin()
        {
            if (isLoadingSubject.Value) return;

            isLoadingSubject.OnNext(true);

            loginDisposable = loginManager
                .LoginWithGoogle()
                .Track(analyticsService.Login, AuthenticationMethod.Google)
                .Subscribe(onDataSource, onError, onCompleted);
        }

        public Task Signup()
        {
            if (isLoadingSubject.Value)
                return Task.CompletedTask;

            var parameter = CredentialsParameter.With(emailSubject.Value, passwordSubject.Value);
            return navigationService.Navigate<SignupViewModel, CredentialsParameter>(parameter);
        }

        private async void onDataSource(ITogglDataSource dataSource)
        {
            lastTimeUsageStorage.SetLogin(timeService.CurrentDateTime);

            await dataSource.StartSyncing();

            isLoadingSubject.OnNext(false);

            onboardingStorage.SetIsNewUser(false);

            await navigationService.Navigate<MainViewModel>();
        }

        private void onError(Exception exception)
        {
            isLoadingSubject.OnNext(false);
            onCompleted();

            if (errorHandlingService.TryHandleDeprecationError(exception))
                return;

            switch (exception)
            {
                case UnauthorizedException forbidden:
                    errorMessageSubject.OnNext(Resources.IncorrectEmailOrPassword);
                    break;
                case GoogleLoginException googleEx when googleEx.LoginWasCanceled:
                    errorMessageSubject.OnNext("");
                    break;
                default:
                    errorMessageSubject.OnNext(Resources.GenericLoginError);
                    break;
            }
        }

        private void onCompleted()
        {
            loginDisposable?.Dispose();
            loginDisposable = null;
        }
    }
}
