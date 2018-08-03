using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using NSubstitute;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Exceptions;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Toggl.Multivac;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Network;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class SignupViewModelTests
    {
        public abstract class SignupViewModelTest : BaseViewModelTests<SignupViewModel>
        {
            protected Email ValidEmail { get; } = Email.From("susancalvin@psychohistorian.museum");
            protected Email InvalidEmail { get; } = Email.From("foo@");

            protected Password ValidPassword { get; } = Password.From("123456");
            protected Password InvalidPassword { get; } = Password.Empty;

            protected ILocation Location { get; } = Substitute.For<ILocation>();
            protected ILastTimeUsageStorage LastTimeUsageStorage { get; } = Substitute.For<ILastTimeUsageStorage>();

            protected override SignupViewModel CreateViewModel()
                => new SignupViewModel(
                    ApiFactory,
                    LoginManager,
                    AnalyticsService,
                    OnboardingStorage,
                    NavigationService,
                    ErrorHandlingService,
                    LastTimeUsageStorage,
                    TimeService);

            protected override void AdditionalSetup()
            {
                Location.CountryCode.Returns("LV");
                Location.CountryName.Returns("Latvia");

                Api.Location.Get().Returns(Observable.Return(Location));

                ApiFactory.CreateApiWith(Arg.Any<Credentials>()).Returns(Api);
            }
        }

        public sealed class TheConstructor : SignupViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useApiFactory,
                bool userLoginManager,
                bool useAnalyticsService,
                bool useOnboardingStorage,
                bool userNavigationService,
                bool useApiErrorHandlingService,
                bool useLastTimeUsageStorage,
                bool useTimeService)
            {
                var apiFactory = useApiFactory ? ApiFactory : null;
                var loginManager = userLoginManager ? LoginManager : null;
                var analyticsSerivce = useAnalyticsService ? AnalyticsService : null;
                var onboardingStorage = useOnboardingStorage ? OnboardingStorage : null;
                var navigationService = userNavigationService ? NavigationService : null;
                var apiErrorHandlingService = useApiErrorHandlingService ? ErrorHandlingService : null;
                var lastTimeUsageService = useLastTimeUsageStorage ? LastTimeUsageStorage : null;
                var timeService = useTimeService ? TimeService : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new SignupViewModel(
                        apiFactory,
                        loginManager,
                        analyticsSerivce,
                        onboardingStorage,
                        navigationService,
                        apiErrorHandlingService,
                        lastTimeUsageService,
                        timeService);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheInitializeMethod : SignupViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task GetstheCurrentLocation()
            {
                await ViewModel.Initialize();

                await Api.Location.Received().Get();
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheCountryButtonTitleToCountryNameWhenApiCallSucceeds()
            {
                await ViewModel.Initialize();

                ViewModel.CountryButtonTitle.Should().Be(Location.CountryName);
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheCountryButtonTitleToSelectCountryWhenApiCallFails()
            {
                Api.Location.Get().Returns(Observable.Throw<ILocation>(new Exception()));

                await ViewModel.Initialize();

                ViewModel.CountryButtonTitle.Should().Be(Resources.SelectCountry);
            }

            [Fact, LogIfTooSlow]
            public async Task SetsFailedToGetCountryToTrueWhenApiCallFails()
            {
                Api.Location.Get().Returns(Observable.Throw<ILocation>(new Exception()));

                await ViewModel.Initialize();

                ViewModel.IsCountryErrorVisible.Should().BeTrue();
            }
        }

        public sealed class ThePickCountryCommand : SignupViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task NavigatesToSelectCountryViewModelPassingNullIfLocationApiFailed()
            {
                Api.Location.Get().Returns(Observable.Throw<ILocation>(new Exception()));
                await ViewModel.Initialize();

                await ViewModel.PickCountryCommand.ExecuteAsync();

                await NavigationService.Received().Navigate<SelectCountryViewModel, long?, long?>(null);
            }

            [Fact, LogIfTooSlow]
            public async Task NavigatesToSelectCountryViewModelPassingCountryIdIfLocationApiSucceeded()
            {
                await ViewModel.Initialize();
                var selectedCountryId = await new GetAllCountriesInteractor()
                    .Execute()
                    .Select(countries => countries.Single(country => country.CountryCode == Location.CountryCode))
                    .Select(country => country.Id);

                await ViewModel.PickCountryCommand.ExecuteAsync();

                await NavigationService
                    .Received()
                    .Navigate<SelectCountryViewModel, long?, long?>(selectedCountryId);
            }

            [Fact, LogIfTooSlow]
            public async Task UpdatesTheCountryButtonTitle()
            {
                var selectedCountry = await new GetAllCountriesInteractor()
                    .Execute()
                    .Select(countries => countries.Single(country => country.Id == 1));
                NavigationService
                    .Navigate<SelectCountryViewModel, long?, long?>(Arg.Any<long?>())
                    .Returns(selectedCountry.Id);
                await ViewModel.Initialize();

                ViewModel.PickCountryCommand.Execute();

                ViewModel.CountryButtonTitle.Should().Be(selectedCountry.Name);
            }

            [Fact, LogIfTooSlow]
            public async Task SetsFailedToGetCountryToFalse()
            {
                Api.Location.Get().Returns(Observable.Throw<ILocation>(new Exception()));
                NavigationService
                    .Navigate<SelectCountryViewModel, long?, long?>(Arg.Any<long?>())
                    .Returns(1);
                await ViewModel.Initialize();

                ViewModel.IsCountryErrorVisible.Should().BeTrue();

                await ViewModel.PickCountryCommand.ExecuteAsync();

                ViewModel.IsCountryErrorVisible.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task IsCountryValidShouldBeFalseWhenNetworkFailed()
            {
                Api.Location.Get().Returns(Observable.Throw<ILocation>(new Exception()));

                await ViewModel.Initialize();

                ViewModel.IsCountryValid.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task IsCountryValidShouldBeTrueWhenApiSucceeds()
            {
                await ViewModel.Initialize();

                ViewModel.IsCountryValid.Should().BeTrue();
            }
        }

        public sealed class TheGoogleSignupCommand : SignupViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void CallsTheLoginManager()
            {
                ViewModel.GoogleSignupCommand.Execute();

                LoginManager.Received().SignUpWithGoogle();
            }

            [Fact, LogIfTooSlow]
            public void DoesNothingWhenThePageIsCurrentlyLoading()
            {
                var never = Observable.Never<ITogglDataSource>();
                LoginManager.SignUpWithGoogle().Returns(never);
                ViewModel.GoogleSignupCommand.Execute();

                ViewModel.GoogleSignupCommand.Execute();

                LoginManager.Received(1).SignUpWithGoogle();
            }

            [Fact, LogIfTooSlow]
            public void SetsTheIsLoadingPropertyToTrue()
            {
                LoginManager.SignUpWithGoogle().Returns(
                    Observable.Never<ITogglDataSource>());

                ViewModel.GoogleSignupCommand.Execute();

                ViewModel.IsLoading.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void TracksGoogleSignupEvent()
            {
                LoginManager.SignUpWithGoogle().Returns(
                    Observable.Return(Substitute.For<ITogglDataSource>()));

                ViewModel.GoogleSignupCommand.Execute();

                AnalyticsService.SignUp.Received().Track(AuthenticationMethod.Google);
            }

            [Fact, LogIfTooSlow]
            public void StopsTheViewModelLoadStateWhenItErrors()
            {
                LoginManager.SignUpWithGoogle().Returns(
                    Observable.Throw<ITogglDataSource>(new GoogleLoginException(false)));

                ViewModel.GoogleSignupCommand.Execute();

                ViewModel.IsLoading.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void DoesNotNavigateWhenTheLoginFails()
            {
                LoginManager.SignUpWithGoogle().Returns(
                    Observable.Throw<ITogglDataSource>(new GoogleLoginException(false)));

                ViewModel.GoogleSignupCommand.Execute();

                NavigationService.DidNotReceive().Navigate<MainViewModel>();
            }

            [Fact, LogIfTooSlow]
            public void DoesNotDisplayAnErrormessageWhenTheUserCancelsTheRequestOnTheGoogleService()
            {
                LoginManager.SignUpWithGoogle().Returns(
                    Observable.Throw<ITogglDataSource>(new GoogleLoginException(true)));

                ViewModel.GoogleSignupCommand.Execute();

                ViewModel.HasError.Should().BeFalse();
                ViewModel.ErrorText.Should().BeEmpty();
            }

            public sealed class WhenSignupSucceeds : SuccessfulSignupTest
            {
                protected override void ExecuteCommand()
                {
                    ViewModel.GoogleSignupCommand.Execute();
                }

                protected override void AdditionalViewModelSetup()
                {
                    base.AdditionalViewModelSetup();

                    LoginManager
                        .SignUpWithGoogle()
                        .Returns(Observable.Return(DataSource));
                }

                [Property]
                public void SavesTheTimeOfLastLogin(DateTimeOffset now)
                {
                    var viewModel = CreateViewModel();
                    TimeService.CurrentDateTime.Returns(now);

                    viewModel.GoogleSignupCommand.Execute();

                    LastTimeUsageStorage.Received().SetLogin(now);
                }
            }
        }

        public sealed class TheTogglePasswordVisibilityCommand : SignupViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void SetsIsPasswordMaskedToFalseWhenItIsTrue()
            {
                ViewModel.TogglePasswordVisibilityCommand.Execute();

                ViewModel.IsPasswordMasked.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void SetsIsPasswordMaskedToTrueWhenItIsFalse()
            {
                ViewModel.TogglePasswordVisibilityCommand.Execute();

                ViewModel.TogglePasswordVisibilityCommand.Execute();

                ViewModel.IsPasswordMasked.Should().BeTrue();
            }
        }

        public sealed class TheLoginCommand : SignupViewModelTest
        {
            [Property]
            public void NavigatesToLoginViewModel(
                NonEmptyString email, NonEmptyString password)
            {
                ViewModel.Email = Email.From(email.Get);
                ViewModel.Password = Password.From(password.Get);

                ViewModel.LoginCommand.Execute();

                NavigationService
                    .Received()
                    .Navigate<LoginViewModel, CredentialsParameter>(
                        Arg.Is<CredentialsParameter>(parameter
                            => parameter.Email.Equals(ViewModel.Email)
                                && parameter.Password.Equals(ViewModel.Password)
                        )
                    );
            }

            [Fact, LogIfTooSlow]
            public void PassesTheCredentialsToLoginViewModelIfUserTriedToSignUpWithExistingEmail()
            {
                var request = Substitute.For<IRequest>();
                request.Endpoint.Returns(new Uri("http://any.url.com"));
                var exception = new EmailIsAlreadyUsedException(
                    new BadRequestException(
                        request, Substitute.For<IResponse>()
                    )
                );
                LoginManager
                    .SignUp(Arg.Any<Email>(), Arg.Any<Password>(), true, Arg.Any<int>())
                    .Returns(
                        Observable.Throw<ITogglDataSource>(exception)
                    );
                NavigationService.Navigate<bool>(typeof(TermsOfServiceViewModel)).Returns(true);
                ViewModel.Email = ValidEmail;
                ViewModel.Password = ValidPassword;
                ViewModel.SignupCommand.Execute();

                ViewModel.LoginCommand.Execute();

                NavigationService
                    .Received()
                    .Navigate<LoginViewModel, CredentialsParameter>(
                        Arg.Is<CredentialsParameter>(
                            parameter => parameter.Email.Equals(ViewModel.Email)
                                && parameter.Password.Equals(ViewModel.Password)
                        )
                    );
            }
        }

        public sealed class TheSignupCommand : SignupViewModelTest
        {
            protected override void AdditionalViewModelSetup()
            {
                base.AdditionalViewModelSetup();

                ViewModel.Email = ValidEmail;
                ViewModel.Password = ValidPassword;

                ViewModel.Initialize().Wait();
            }

            [Fact, LogIfTooSlow]
            public async Task NavigatesToTheTermsOfServiceViewModel()
            {
                await ViewModel.SignupCommand.ExecuteAsync();

                await NavigationService.Received().Navigate<bool>(typeof(TermsOfServiceViewModel));
            }

            [Fact, LogIfTooSlow]
            public void DoesNotTryToSignUpIfUserDoesNotAcceptTermsOfService()
            {
                NavigationService
                    .Navigate<bool>(typeof(TermsOfServiceViewModel))
                    .Returns(false);

                ViewModel.SignupCommand.Execute();

                LoginManager.DidNotReceive().SignUp(
                    Arg.Any<Email>(),
                    Arg.Any<Password>(),
                    Arg.Any<bool>(),
                    Arg.Any<int>());
            }

            [Fact, LogIfTooSlow]
            public void ShowsTheTermsOfServiceViewModelOnlyOnceIfUserAcceptsTheTerms()
            {
                NavigationService
                    .Navigate<bool>(typeof(TermsOfServiceViewModel))
                    .Returns(true);
                LoginManager
                    .SignUp(Arg.Any<Email>(), Arg.Any<Password>(), Arg.Any<bool>(), Arg.Any<int>())
                    .Returns(Observable.Throw<ITogglDataSource>(new Exception()));

                ViewModel.SignupCommand.Execute();
                ViewModel.SignupCommand.Execute();

                NavigationService.Received(1).Navigate<bool>(typeof(TermsOfServiceViewModel));
            }

            public sealed class WhenUserAcceptsTheTermsOfService : SignupViewModelTest
            {
                protected override void AdditionalSetup()
                {
                    base.AdditionalSetup();

                    NavigationService
                        .Navigate<bool>(typeof(TermsOfServiceViewModel))
                        .Returns(true);
                }

                protected override void AdditionalViewModelSetup()
                {
                    base.AdditionalViewModelSetup();

                    ViewModel.Initialize().Wait();

                    ViewModel.Email = ValidEmail;
                    ViewModel.Password = ValidPassword;
                    ViewModel.SignupCommand.Execute();
                }

                [Fact, LogIfTooSlow]
                public void SetsIsLoadingToTrue()
                {
                    ViewModel.IsLoading.Should().BeTrue();
                }

                [Fact, LogIfTooSlow]
                public void TriesToSignUp()
                {
                    LoginManager.Received().SignUp(
                        ViewModel.Email,
                        ViewModel.Password,
                        true,
                        Arg.Any<int>());
                }

                [Fact, LogIfTooSlow]
                public void TracksSignupEvent()
                {
                    AnalyticsService.SignUp.Received().Track(AuthenticationMethod.EmailAndPassword);
                }

                public sealed class WhenSignupSucceeds : SuccessfulSignupTest
                {
                    protected override void ExecuteCommand()
                    {
                        ViewModel.SignupCommand.Execute();
                    }

                    protected override void AdditionalViewModelSetup()
                    {
                        base.AdditionalViewModelSetup();

                        ViewModel.Initialize().Wait();

                        ViewModel.Email = ValidEmail;
                        ViewModel.Password = ValidPassword;
                        LoginManager
                            .SignUp(Arg.Any<Email>(), Arg.Any<Password>(), Arg.Any<bool>(), Arg.Any<int>())
                            .Returns(Observable.Return(DataSource));
                        NavigationService
                            .Navigate<bool>(typeof(TermsOfServiceViewModel))
                            .Returns(true);
                    }

                    [Property]
                    public void SavesTheTimeOfLastLogin(DateTimeOffset now)
                    {
                        var viewModel = CreateViewModel();
                        viewModel.Initialize().Wait();
                        viewModel.Email = ValidEmail;
                        viewModel.Password = ValidPassword;
                        TimeService.CurrentDateTime.Returns(now);

                        viewModel.SignupCommand.Execute();

                        LastTimeUsageStorage.Received().SetLogin(now);
                    }
                }

                public sealed class WhenSignupFails : SignupViewModelTest
                {
                    private void prepareException(Exception exception)
                    {
                        LoginManager
                            .SignUp(Arg.Any<Email>(), Arg.Any<Password>(), Arg.Any<bool>(), Arg.Any<int>())
                            .Returns(Observable.Throw<ITogglDataSource>(exception));
                    }

                    protected override void AdditionalViewModelSetup()
                    {
                        base.AdditionalViewModelSetup();

                        ViewModel.Initialize().Wait();

                        ViewModel.Email = ValidEmail;
                        ViewModel.Password = ValidPassword;

                        NavigationService
                            .Navigate<bool>(typeof(TermsOfServiceViewModel))
                            .Returns(true);
                    }

                    [Fact, LogIfTooSlow]
                    public void SetsIsLoadingToFalse()
                    {
                        prepareException(new Exception());

                        ViewModel.SignupCommand.Execute();

                        ViewModel.IsLoading.Should().BeFalse();
                    }

                    [Fact, LogIfTooSlow]
                    public void SetsIncorrectEmailOrPasswordErrorIfReceivedUnautghorizedException()
                    {
                        prepareException(new UnauthorizedException(
                            Substitute.For<IRequest>(),
                            Substitute.For<IResponse>()));

                        ViewModel.SignupCommand.Execute();

                        ViewModel.ErrorText.Should().Be(Resources.IncorrectEmailOrPassword);
                    }

                    [Fact, LogIfTooSlow]
                    public void SetsEmailAlreadyUsedErrorIfReceivedEmailIsAlreadyusedException()
                    {
                        var request = Substitute.For<IRequest>();
                        request.Endpoint.Returns(new Uri("https://any.url.com"));
                        prepareException(new EmailIsAlreadyUsedException(
                            new BadRequestException(
                                request,
                                Substitute.For<IResponse>()
                            )
                        ));

                        ViewModel.SignupCommand.Execute();

                        ViewModel.ErrorText.Should().Be(Resources.EmailIsAlreadyUsedError);
                    }

                    [Fact, LogIfTooSlow]
                    public void SetsGenereicErrorForAnyOtherException()
                    {
                        prepareException(new Exception());

                        ViewModel.SignupCommand.Execute();

                        ViewModel.ErrorText.Should().Be(Resources.GenericSignUpError);
                    }
                }
            }
        }

        public abstract class SuccessfulSignupTest : SignupViewModelTest
        {
            protected abstract void ExecuteCommand();

            [Fact, LogIfTooSlow]
            public void StartsSyncing()
            {
                ExecuteCommand();

                DataSource.Received().StartSyncing();
            }

            [Fact, LogIfTooSlow]
            public void SetsIsNewUserToTrue()
            {
                ExecuteCommand();

                OnboardingStorage.Received().SetIsNewUser(true);
            }

            [Fact, LogIfTooSlow]
            public void SetsUserSignedUp()
            {
                ExecuteCommand();

                OnboardingStorage.Received().SetUserSignedUp();
            }

            [Fact, LogIfTooSlow]
            public void NavigatesToMainViewModel()
            {
                ExecuteCommand();

                NavigationService.Received().Navigate<MainViewModel>();
            }
        }

        public sealed class TheSignupEnabledProperty : SignupViewModelTest
        {
            protected override void AdditionalViewModelSetup()
            {
                base.AdditionalViewModelSetup();

                ViewModel.Initialize().Wait();
            }

            [Fact, LogIfTooSlow]
            public void ReturnsTrueWhenEmailAndPasswordAreValidAndIsNotLoading()
            {
                ViewModel.Email = ValidEmail;
                ViewModel.Password = ValidPassword;

                ViewModel.SignupEnabled.Should().BeTrue();
            }

            [Theory]
            [InlineData("not an email", "123")]
            [InlineData("not an email", "1234567")]
            [InlineData("this@is.email", "123")]
            public void ReturnsFalseWhenEmailOrPasswordIsInvalid(string email, string password)
            {
                ViewModel.Email = Email.From(email);
                ViewModel.Password = Password.From(password);

                ViewModel.SignupEnabled.Should().BeFalse();
            }

            [Fact]
            public void ReturnsFlaseWhenIsLoading()
            {
                ViewModel.Email = ValidEmail;
                ViewModel.Password = ValidPassword;
                NavigationService
                    .Navigate<bool>(typeof(TermsOfServiceViewModel))
                    .Returns(true);
                LoginManager
                    .SignUp(Arg.Any<Email>(), Arg.Any<Password>(), Arg.Any<bool>(), Arg.Any<int>())
                    .Returns(Observable.Never<ITogglDataSource>());
                ViewModel.SignupCommand.Execute();

                ViewModel.SignupEnabled.Should().BeFalse();
            }
        }

        public sealed class ThePrepareCommand : SignupViewModelTest
        {
            [Property]
            public void SetsTheEmail(NonEmptyString emailString)
            {
                var email = Email.From(emailString.Get);
                var password = Password.Empty;
                var parameter = CredentialsParameter.With(email, password);

                ViewModel.Prepare(parameter);

                ViewModel.Email.Should().Be(email);
            }

            [Property]
            public void SetsThePassword(NonEmptyString passwordString)
            {
                var email = Email.Empty;
                var password = Password.From(passwordString.Get);
                var parameter = CredentialsParameter.With(email, password);

                ViewModel.Prepare(parameter);

                ViewModel.Password.Should().Be(password);
            }
        }
    }
}
