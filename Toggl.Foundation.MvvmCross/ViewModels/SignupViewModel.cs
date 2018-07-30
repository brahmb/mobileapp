using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Exceptions;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Interactors.Location;
using Toggl.Foundation.Login;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Network;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class SignupViewModel : MvxViewModel<CredentialsParameter>
    {
        private readonly IApiFactory apiFactory;
        private readonly ILoginManager loginManager;
        private readonly IAnalyticsService analyticsService;
        private readonly IOnboardingStorage onboardingStorage;
        private readonly IMvxNavigationService navigationService;
        private readonly IErrorHandlingService errorHandlingService;
        private readonly ILastTimeUsageStorage lastTimeUsageStorage;
        private readonly ITimeService timeService;

        private IDisposable getCountrySubscription;
        private IDisposable signupDisposable;
        private bool termsOfServiceAccepted;
        private List<ICountry> allCountries;
        private long? countryId;

        public string CountryButtonTitle { get; private set; } = Resources.SelectCountry;

        public bool IsCountryErrorVisible { get; private set; } = false;

        public bool IsCountryValid => countryId.HasValue;

        public Email Email { get; set; } = Email.Empty;

        public Password Password { get; set; } = Password.Empty;

        public bool IsLoading { get; private set; }

        public string ErrorText { get; private set; }

        public bool HasError => !string.IsNullOrEmpty(ErrorText);

        public bool IsPasswordMasked { get; private set; } = true;

        public bool SignupEnabled
            => Email.IsValid
            && Password.IsValid
            && !IsLoading
            && countryId.HasValue;

        public bool IsShowPasswordButtonVisible { get; set; }

        public IMvxCommand GoogleSignupCommand { get; }

        public IMvxCommand TogglePasswordVisibilityCommand { get; }

        public IMvxAsyncCommand SignupCommand { get; }

        public IMvxAsyncCommand LoginCommand { get; }

        public IMvxAsyncCommand PickCountryCommand { get; }

        public SignupViewModel(
            IApiFactory apiFactory,
            ILoginManager loginManager,
            IAnalyticsService analyticsService,
            IOnboardingStorage onboardingStorage,
            IMvxNavigationService navigationService,
            IErrorHandlingService errorHandlingService,
            ILastTimeUsageStorage lastTimeUsageStorage,
            ITimeService timeService)
        {
            Ensure.Argument.IsNotNull(apiFactory, nameof(apiFactory));
            Ensure.Argument.IsNotNull(loginManager, nameof(loginManager));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(onboardingStorage, nameof(onboardingStorage));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(errorHandlingService, nameof(errorHandlingService));
            Ensure.Argument.IsNotNull(lastTimeUsageStorage, nameof(lastTimeUsageStorage));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));

            this.apiFactory = apiFactory;
            this.loginManager = loginManager;
            this.analyticsService = analyticsService;
            this.onboardingStorage = onboardingStorage;
            this.navigationService = navigationService;
            this.errorHandlingService = errorHandlingService;
            this.lastTimeUsageStorage = lastTimeUsageStorage;
            this.timeService = timeService;

            LoginCommand = new MvxAsyncCommand(login);
            GoogleSignupCommand = new MvxCommand(googleSignup);
            PickCountryCommand = new MvxAsyncCommand(pickCountry);
            SignupCommand = new MvxAsyncCommand(signup, () => SignupEnabled);
            TogglePasswordVisibilityCommand = new MvxCommand(togglePasswordVisibility);
        }


        public override void Prepare(CredentialsParameter parameter)
        {
            Email = parameter.Email;
            Password = parameter.Password;
        }

        public override async Task Initialize()
        {
            await base.Initialize();

            allCountries = await new GetAllCountriesInteractor().Execute();

            var api = apiFactory.CreateApiWith(Credentials.None);
            getCountrySubscription = new GetCurrentLocationInteractor(api)
                .Execute()
                .Select(location => allCountries.Single(country => country.CountryCode == location.CountryCode))
                .Subscribe(
                    setCountryIfNeeded,
                    _ => setCountryErrorIfNeeded(),
                    () =>
                    {
                        getCountrySubscription?.Dispose();
                        getCountrySubscription = null;
                    }
                );
        }

        private void setCountryIfNeeded(ICountry country)
        {
            if (countryId.HasValue) return;
            countryId = country.Id;
            CountryButtonTitle = country.Name;
        }

        private void setCountryErrorIfNeeded()
        {
            if (countryId.HasValue) return;

            IsCountryErrorVisible = true;
        }

        private async Task signup()
        {
            if (!termsOfServiceAccepted)
                termsOfServiceAccepted = await navigationService.Navigate<bool>(typeof(TermsOfServiceViewModel));

            if (!termsOfServiceAccepted)
                return;

            IsLoading = true;

            signupDisposable =
                loginManager
                    .SignUp(Email, Password, true, (int)countryId.Value)
                    .Track(analyticsService.SignUp, AuthenticationMethod.EmailAndPassword)
                    .Subscribe(onDataSource, onError, onCompleted);
        }

        private async void onDataSource(ITogglDataSource dataSource)
        {
            lastTimeUsageStorage.SetLogin(timeService.CurrentDateTime);

            await dataSource.StartSyncing();

            onboardingStorage.SetIsNewUser(true);
            onboardingStorage.SetUserSignedUp();

            await navigationService.Navigate<MainTabBarViewModel>();
        }

        private void onError(Exception exception)
        {
            IsLoading = false;
            onCompleted();

            if (errorHandlingService.TryHandleDeprecationError(exception))
                return;

            switch (exception)
            {
                case UnauthorizedException forbidden:
                    ErrorText = Resources.IncorrectEmailOrPassword;
                    break;
                case GoogleLoginException googleEx when googleEx.LoginWasCanceled:
                    ErrorText = "";
                    break;
                case EmailIsAlreadyUsedException _:
                    ErrorText = Resources.EmailIsAlreadyUsedError;
                    break;
                default:
                    ErrorText = Resources.GenericSignUpError;
                    break;
            }
        }

        private void onCompleted()
        {
            signupDisposable?.Dispose();
            signupDisposable = null;
        }

        private void googleSignup()
        {
            if (IsLoading) return;

            IsLoading = true;

            signupDisposable = loginManager
                .SignUpWithGoogle()
                .Track(analyticsService.SignUp, AuthenticationMethod.Google)
                .Subscribe(onDataSource, onError, onCompleted);
        }

        private void togglePasswordVisibility()
            => IsPasswordMasked = !IsPasswordMasked;

        private async Task pickCountry()
        {
            getCountrySubscription?.Dispose();
            getCountrySubscription = null;

            var selectedCountryId = await navigationService
                .Navigate<SelectCountryViewModel, long?, long?>(countryId);

            if (selectedCountryId == null)
            {
                setCountryErrorIfNeeded();
                return;
            }

            var selectedCountry = allCountries
                .Single(country => country.Id == selectedCountryId.Value);

            IsCountryErrorVisible = false;
            countryId = selectedCountry.Id;
            CountryButtonTitle = selectedCountry.Name;
            SignupCommand.RaiseCanExecuteChanged();
       }

        private Task login()
        {
            var parameter = CredentialsParameter.With(Email, Password);
            return navigationService.Navigate<LoginViewModel, CredentialsParameter>(parameter);
        }

        private void OnEmailChanged()
            => SignupCommand.RaiseCanExecuteChanged();

        private void OnPasswordChanged()
            => SignupCommand.RaiseCanExecuteChanged();

        private void OnIsLoadingChanged()
            => SignupCommand.RaiseCanExecuteChanged();
    }
}
