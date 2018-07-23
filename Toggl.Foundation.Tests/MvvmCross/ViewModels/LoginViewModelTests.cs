using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Exceptions;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Toggl.Foundation.Tests.TestExtensions;
using Toggl.Multivac;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Network;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class LoginViewModelTests
    {
        public abstract class LoginViewModelTest : BaseViewModelTests<LoginViewModel>
        {
            protected Email ValidEmail { get; } = Email.From("person@company.com");
            protected Email InvalidEmail { get; } = Email.From("this is not an email");

            protected Password ValidPassword { get; } = Password.From("T0t4lly s4afe p4$$");
            protected Password InvalidPassword { get; } = Password.From("123");

            protected TestScheduler TestScheduler { get; } = new TestScheduler();

            protected ILastTimeUsageStorage LastTimeUsageStorage { get; } = Substitute.For<ILastTimeUsageStorage>();

            protected override LoginViewModel CreateViewModel()
                => new LoginViewModel(
                    LoginManager,
                    AnalyticsService,
                    OnboardingStorage,
                    NavigationService,
                    PasswordManagerService,
                    ErrorHandlingService,
                    LastTimeUsageStorage,
                    TimeService);
        }

        public sealed class TheConstructor : LoginViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ClassData(typeof(EightParameterConstructorTestData))]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool userLoginManager,
                bool useAnalyticsService,
                bool useOnboardingStorage,
                bool userNavigationService,
                bool usePasswordManagerService,
                bool useApiErrorHandlingService,
                bool useLastTimeUsageStorage,
                bool useTimeService)
            {
                var loginManager = userLoginManager ? LoginManager : null;
                var analyticsSerivce = useAnalyticsService ? AnalyticsService : null;
                var onboardingStorage = useOnboardingStorage ? OnboardingStorage : null;
                var navigationService = userNavigationService ? NavigationService : null;
                var passwordManagerService = usePasswordManagerService ? PasswordManagerService : null;
                var apiErrorHandlingService = useApiErrorHandlingService ? ErrorHandlingService : null;
                var lastTimeUsageStorage = useLastTimeUsageStorage ? LastTimeUsageStorage : null;
                var timeService = useTimeService ? TimeService : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new LoginViewModel(loginManager,
                                             analyticsSerivce,
                                             onboardingStorage,
                                             navigationService,
                                             passwordManagerService,
                                             apiErrorHandlingService,
                                             lastTimeUsageStorage,
                                             timeService);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheLoginEnabledObservable : LoginViewModelTest
        {
            [Theory]
            [InlineData("invalid email address", "123")]
            [InlineData("invalid email address", "T0tally s4afe p4a$$")]
            [InlineData("person@company.com", "123")]
            public void ReturnsFalseWhenEmailOrPasswordIsInvalid(string email, string password)
            {
                var observer = Substitute.For<IObserver<bool>>();
                ViewModel.LoginEnabled.Subscribe(observer);
                ViewModel.SetEmail(Email.From(email));
                ViewModel.SetPassword(Password.From(password));

                observer.Received(1).OnNext(false);
            }

            [Theory]
            [InlineData("invalid email address", "123")]
            [InlineData("invalid email address", "T0tally s4afe p4a$$")]
            [InlineData("person@company.com", "123")]
            [InlineData("person@company.com", "T0tally s4afe p4a$$")]
            public void ReturnsFalseWhenIsLoading(string email, string password)
            {
                var observer = Substitute.For<IObserver<bool>>();
                ViewModel.SetEmail(Email.From(email));
                ViewModel.SetPassword(Password.From(password));
                //Make sure isloading is true
                LoginManager
                    .Login(Arg.Any<Email>(), Arg.Any<Password>())
                    .Returns(Observable.Never<ITogglDataSource>());
                ViewModel.LoginEnabled.Subscribe(observer);
                ViewModel.Login();

                observer.Received(1).OnNext(false);
            }
        }

        public sealed class TheIsPasswordManagerAvailableProperty : LoginViewModelTest
        {
            [Property]
            public void ReturnsWhetherThePasswordManagerIsAvailable(bool isAvailable)
            {
                PasswordManagerService.IsAvailable.Returns(isAvailable);

                var viewModel = CreateViewModel();

                viewModel.IsPasswordManagerAvailable.Should().Be(isAvailable);
            }
        }

        public sealed class TheLoginMethod : LoginViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void CallsTheLoginManagerWhenTheEmailAndPasswordAreValid()
            {
                ViewModel.SetEmail(ValidEmail);
                ViewModel.SetPassword(ValidPassword);

                ViewModel.Login();

                LoginManager.Received().Login(Arg.Is(ValidEmail), Arg.Is(ValidPassword));
            }

            [Fact, LogIfTooSlow]
            public void DoesNothingWhenThePageIsCurrentlyLoading()
            {
                var never = Observable.Never<ITogglDataSource>();
                LoginManager.Login(Arg.Any<Email>(), Arg.Any<Password>()).Returns(never);
                ViewModel.SetEmail(ValidEmail);
                ViewModel.SetPassword(ValidPassword);
                ViewModel.Login();

                ViewModel.Login();

                LoginManager.Received(1).Login(Arg.Any<Email>(), Arg.Any<Password>());
            }

            public sealed class WhenLoginSucceeds : LoginViewModelTest
            {
                public WhenLoginSucceeds()
                {
                    ViewModel.SetEmail(ValidEmail);
                    ViewModel.SetPassword(ValidPassword);
                    LoginManager.Login(Arg.Any<Email>(), Arg.Any<Password>())
                        .Returns(Observable.Return(DataSource));
                }

                [Fact, LogIfTooSlow]
                public async Task StartsSyncing()
                {
                    ViewModel.Login();

                    await DataSource.Received().StartSyncing();
                }

                [Fact, LogIfTooSlow]
                public void SetsIsNewUserToFalse()
                {
                    ViewModel.Login();

                    OnboardingStorage.Received().SetIsNewUser(false);
                }

                [Fact, LogIfTooSlow]
                public void SetsIsLoadingToFalse()
                {
                    var observer = TestScheduler.CreateObserver<bool>();
                    ViewModel.IsLoading.Subscribe(observer);

                    ViewModel.Login();

                    observer.Messages.Last().Value.Value.Should().BeFalse();
                }

                [Fact, LogIfTooSlow]
                public void NavigatesToTheTimeEntriesViewModel()
                {
                    ViewModel.Login();

                    NavigationService.Received().Navigate<MainViewModel>();
                }

                [Fact, LogIfTooSlow]
                public void TracksTheLoginEvent()
                {
                    ViewModel.Login();

                    AnalyticsService.Received().Login.Track(AuthenticationMethod.EmailAndPassword);
                }

                [Property]
                public void SavesTheTimeOfLastLogin(DateTimeOffset now)
                {
                    TimeService.CurrentDateTime.Returns(now);

                    ViewModel.Login();

                    LastTimeUsageStorage.Received().SetLogin(Arg.Is(now));
                }
            }

            public sealed class WhenLoginFails : LoginViewModelTest
            {
                public WhenLoginFails()
                {
                    ViewModel.SetEmail(ValidEmail);
                    ViewModel.SetPassword(ValidPassword);
                }

                [Fact, LogIfTooSlow]
                public void SetsIsLoadingToFalse()
                {
                    var observer = TestScheduler.CreateObserver<bool>();
                    ViewModel.IsLoading.Subscribe(observer);
                    LoginManager.Login(Arg.Any<Email>(), Arg.Any<Password>())
                        .Returns(Observable.Throw<ITogglDataSource>(new Exception()));

                    ViewModel.Login();

                    observer.Messages.Last().Value.Value.Should().BeFalse();
                }

                [Fact, LogIfTooSlow]
                public void DoesNotNavigate()
                {
                    LoginManager.Login(Arg.Any<Email>(), Arg.Any<Password>())
                        .Returns(Observable.Throw<ITogglDataSource>(new Exception()));

                    ViewModel.Login();

                    NavigationService.DidNotReceive().Navigate<MainViewModel>();
                }

                [Fact, LogIfTooSlow]
                public void SetsTheErrorMessageToIncorrectEmailOrPasswordWhenReceivedUnauthorizedException()
                {
                    var observer = TestScheduler.CreateObserver<string>();
                    ViewModel.ErrorMessage.Subscribe(observer);
                    var exception = new UnauthorizedException(
                        Substitute.For<IRequest>(), Substitute.For<IResponse>());
                    LoginManager.Login(Arg.Any<Email>(), Arg.Any<Password>())
                        .Returns(Observable.Throw<ITogglDataSource>(exception));

                    ViewModel.Login();

                    observer.Messages.Last().Value.Value.Should().Be(Resources.IncorrectEmailOrPassword);
                }

                [Fact, LogIfTooSlow]
                public void SetsTheErrorMessageToNothingWhenGoogleLoginWasCanceled()
                {
                    var observer = TestScheduler.CreateObserver<string>();
                    var exception = new GoogleLoginException(true);
                    ViewModel.ErrorMessage.Subscribe(observer);
                    LoginManager.Login(Arg.Any<Email>(), Arg.Any<Password>())
                        .Returns(Observable.Throw<ITogglDataSource>(exception));

                    ViewModel.Login();

                    observer.Messages.Last().Value.Value.Should().BeEmpty();
                }

                [Fact, LogIfTooSlow]
                public void SetsTheErrorMessageToGenericLoginErrorForAnyOtherException()
                {
                    var observer = TestScheduler.CreateObserver<string>();
                    var exception = new Exception();
                    ViewModel.ErrorMessage.Subscribe(observer);
                    LoginManager.Login(Arg.Any<Email>(), Arg.Any<Password>())
                        .Returns(Observable.Throw<ITogglDataSource>(exception));

                    ViewModel.Login();

                    observer.Messages.Last().Value.Value.Should().Be(Resources.GenericLoginError);
                }

                [Fact, LogIfTooSlow]
                public void DoesNothingWhenErrorHandlingServiceHandlesTheException()
                {
                    var observer = TestScheduler.CreateObserver<string>();
                    var exception = new Exception();
                    ViewModel.ErrorMessage.Subscribe(observer);
                    LoginManager.Login(Arg.Any<Email>(), Arg.Any<Password>())
                        .Returns(Observable.Throw<ITogglDataSource>(exception));
                    ErrorHandlingService.TryHandleDeprecationError(Arg.Any<Exception>())
                        .Returns(true);

                    ViewModel.Login();

                    observer.Messages.Last().Value.Value.Should().BeEmpty();
                }
            }
        }

        public sealed class TheGoogleLoginMethod : LoginViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void CallsTheLoginManager()
            {
                ViewModel.GoogleLogin();

                LoginManager.Received().LoginWithGoogle();
            }

            [Fact, LogIfTooSlow]
            public void DoesNothingWhenThePageIsCurrentlyLoading()
            {
                var never = Observable.Never<ITogglDataSource>();
                LoginManager.LoginWithGoogle().Returns(never);
                ViewModel.GoogleLogin();

                ViewModel.GoogleLogin();

                LoginManager.Received(1).LoginWithGoogle();
            }

            [Fact, LogIfTooSlow]
            public void NavigatesToTheTimeEntriesViewModelWhenTheLoginSucceeds()
            {
                LoginManager.LoginWithGoogle()
                    .Returns(Observable.Return(Substitute.For<ITogglDataSource>()));

                ViewModel.GoogleLogin();

                NavigationService.Received().Navigate<MainViewModel>();
            }

            [Fact, LogIfTooSlow]
            public void StopsTheViewModelLoadStateWhenItCompletes()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.IsLoading.Subscribe(observer);
                LoginManager.LoginWithGoogle()
                    .Returns(Observable.Return(Substitute.For<ITogglDataSource>()));

                ViewModel.GoogleLogin();

                observer.Messages.Last().Value.Value.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void TracksGoogleLoginEvent()
            {
                LoginManager.LoginWithGoogle()
                    .Returns(Observable.Return(Substitute.For<ITogglDataSource>()));

                ViewModel.GoogleLogin();

                AnalyticsService.Received().Login.Track(AuthenticationMethod.Google);
            }

            [Fact, LogIfTooSlow]
            public void StopsTheViewModelLoadStateWhenItErrors()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.IsLoading.Subscribe(observer);
                LoginManager.LoginWithGoogle()
                    .Returns(Observable.Throw<ITogglDataSource>(new GoogleLoginException(false)));

                ViewModel.GoogleLogin();

                observer.Messages.Last().Value.Value.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void DoesNotNavigateWhenTheLoginFails()
            {
                LoginManager.LoginWithGoogle()
                    .Returns(Observable.Throw<ITogglDataSource>(new GoogleLoginException(false)));

                ViewModel.GoogleLogin();

                NavigationService.DidNotReceive().Navigate<MainViewModel>();
            }

            [Fact, LogIfTooSlow]
            public void DoesNotDisplayAnErrormessageWhenTheUserCancelsTheRequestOnTheGoogleService()
            {
                var observer = TestScheduler.CreateObserver<string>();
                ViewModel.ErrorMessage.Subscribe(observer);
                LoginManager.LoginWithGoogle()
                    .Returns(Observable.Throw<ITogglDataSource>(new GoogleLoginException(true)));

                ViewModel.GoogleLogin();

                observer.Messages.Last().Value.Value.Should().BeEmpty();
            }

            [Property]
            public void SavesTheTimeOfLastLogin(DateTimeOffset now)
            {
                TimeService.CurrentDateTime.Returns(now);
                LoginManager.LoginWithGoogle()
                    .Returns(Observable.Return(Substitute.For<ITogglDataSource>()));

                ViewModel.GoogleLogin();

                LastTimeUsageStorage.Received().SetLogin(Arg.Is(now));
            }
        }

        public sealed class TheTogglePasswordVisibilityMethod : LoginViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void SetsTheIsPasswordMaskedToFalseWhenItIsTrue()
            {
                var observer = Substitute.For<IObserver<bool>>();
                ViewModel.IsPasswordMasked.Subscribe(observer);
                ViewModel.TogglePasswordVisibility();

                Received.InOrder(() =>
                {
                    observer.OnNext(true);
                    observer.OnNext(false);
                });
            }

            [Fact, LogIfTooSlow]
            public void SetsTheIsPasswordMaskedToTrueWhenItIsFalse()
            {
                var observer = Substitute.For<IObserver<bool>>();
                ViewModel.IsPasswordMasked.Subscribe(observer);
                ViewModel.TogglePasswordVisibility();

                ViewModel.TogglePasswordVisibility();

                Received.InOrder(() =>
                {
                    observer.OnNext(true);
                    observer.OnNext(false);
                    observer.OnNext(true);
                });
            }
        }

        public sealed class TheStartPasswordManagerCommand : LoginViewModelTest
        {
            public TheStartPasswordManagerCommand()
            {
                PasswordManagerService.IsAvailable.Returns(true);
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotCallPasswordManagerWhenThePasswordManagerIsNotAvailable()
            {
                PasswordManagerService.IsAvailable.Returns(false);

                await ViewModel.StartPasswordManager();

                await PasswordManagerService.DidNotReceive().GetLoginInformation();
            }

            [Fact, LogIfTooSlow]
            public async Task CallsThePasswordManagerServiceWhenTheServiceIsAvailable()
            {
                var observable = Observable.Return(new PasswordManagerResult(ValidEmail, ValidPassword));
                PasswordManagerService.GetLoginInformation().Returns(observable);

                await ViewModel.StartPasswordManager();

                await PasswordManagerService.Received().GetLoginInformation();
            }

            [Fact, LogIfTooSlow]
            public void CallsTheLoginCommandWhenValidCredentialsAreProvided()
            {
                var scheduler = new TestScheduler();
                var observable = arrangeCallToPasswordManagerWithValidCredentials();

                scheduler.Schedule(TimeSpan.FromTicks(20), async () => await ViewModel.StartPasswordManager());

                scheduler.Start(
                    () => observable,
                    created: 0,
                    subscribed: 10,
                    disposed: 100
                );

                LoginManager.Received().Login(Arg.Any<Email>(), Arg.Any<Password>());
            }

            [Fact, LogIfTooSlow]
            public void SetsTheEmailFieldWhenValidCredentialsAreProvided()
            {
                var scheduler = new TestScheduler();
                var observable = arrangeCallToPasswordManagerWithValidCredentials();
                var observer = Substitute.For<IObserver<string>>();
                ViewModel.Password.Subscribe(observer);

                scheduler.Schedule(TimeSpan.FromTicks(20), async () => await ViewModel.StartPasswordManager());

                scheduler.Start(
                    () => observable,
                    created: 0,
                    subscribed: 10,
                    disposed: 100
                );

                observer.Received(1).OnNext(ValidPassword.ToString());
            }

            [Fact, LogIfTooSlow]
            public void SetsTheEmailFieldWhenInvalidCredentialsAreProvided()
            {
                var scheduler = new TestScheduler();
                var observable = arrangeCallToPasswordManagerWithInvalidCredentials();
                var observer = Substitute.For<IObserver<string>>();
                ViewModel.Email.Subscribe(observer);

                scheduler.Schedule(TimeSpan.FromTicks(20), async () => await ViewModel.StartPasswordManager());

                scheduler.Start(
                    () => observable,
                    created: 0,
                    subscribed: 10,
                    disposed: 100
                );

                observer.Received(1).OnNext(InvalidEmail.ToString());
            }

            [Fact, LogIfTooSlow]
            public void SetsThePasswordFieldWhenValidCredentialsAreProvided()
            {
                var scheduler = new TestScheduler();
                var observable = arrangeCallToPasswordManagerWithValidCredentials();
                var observer = Substitute.For<IObserver<string>>();
                ViewModel.Password.Subscribe(observer);

                scheduler.Schedule(TimeSpan.FromTicks(20), async () => await ViewModel.StartPasswordManager());

                scheduler.Start(
                    () => observable,
                    created: 0,
                    subscribed: 10,
                    disposed: 100
                );

                observer.Received(1).OnNext(ValidPassword.ToString());
            }

            [Fact, LogIfTooSlow]
            public void DoesNotSetThePasswordFieldWhenInvalidCredentialsAreProvided()
            {
                var scheduler = new TestScheduler();
                var observable = arrangeCallToPasswordManagerWithInvalidCredentials();
                var observer = Substitute.For<IObserver<string>>();
                ViewModel.Password.Subscribe(observer);

                scheduler.Schedule(TimeSpan.FromTicks(20), async () => await ViewModel.StartPasswordManager());

                scheduler.Start(
                    () => observable,
                    created: 0,
                    subscribed: 10,
                    disposed: 100
                );

                observer.Received(1).OnNext(Password.Empty.ToString());
            }

            [Fact, LogIfTooSlow]
            public void DoesNothingWhenValidCredentialsAreNotProvided()
            {
                var scheduler = new TestScheduler();
                var observable = arrangeCallToPasswordManagerWithInvalidCredentials();

                scheduler.Schedule(TimeSpan.FromTicks(20), async () => await ViewModel.StartPasswordManager());

                scheduler.Start(
                    () => observable,
                    created: 0,
                    subscribed: 10,
                    disposed: 100
                );

                LoginManager.DidNotReceive().Login(Arg.Any<Email>(), Arg.Any<Password>());
            }

            [Fact, LogIfTooSlow]
            public async Task TracksThePasswordManagerButtonClicked()
            {
                PasswordManagerService.IsAvailable.Returns(true);
                arrangeCallToPasswordManagerWithInvalidCredentials();

                await ViewModel.StartPasswordManager();

                AnalyticsService.PasswordManagerButtonClicked.Received().Track();
                AnalyticsService.PasswordManagerContainsValidEmail.DidNotReceive().Track();
                AnalyticsService.PasswordManagerContainsValidPassword.DidNotReceive().Track();
            }

            [Fact, LogIfTooSlow]
            public async Task TracksThePasswordManagerContainsValidEmail()
            {
                PasswordManagerService.IsAvailable.Returns(true);
                var loginInfo = new PasswordManagerResult(ValidEmail, InvalidPassword);
                var observable = Observable.Return(loginInfo);
                PasswordManagerService.GetLoginInformation().Returns(observable);

                await ViewModel.StartPasswordManager();

                AnalyticsService.PasswordManagerButtonClicked.Received().Track();
                AnalyticsService.PasswordManagerContainsValidEmail.Received().Track();
                AnalyticsService.PasswordManagerContainsValidPassword.DidNotReceive().Track();
            }

            [Fact, LogIfTooSlow]
            public async Task TracksThePasswordManagerContainsValidPassword()
            {
                PasswordManagerService.IsAvailable.Returns(true);
                arrangeCallToPasswordManagerWithValidCredentials();

                await ViewModel.StartPasswordManager();

                AnalyticsService.PasswordManagerButtonClicked.Received().Track();
                AnalyticsService.PasswordManagerContainsValidEmail.Received().Track();
                AnalyticsService.PasswordManagerContainsValidPassword.Received().Track();
            }

            private IObservable<PasswordManagerResult> arrangeCallToPasswordManagerWithValidCredentials()
            {
                var loginInfo = new PasswordManagerResult(ValidEmail, ValidPassword);
                var observable = Observable.Return(loginInfo);
                PasswordManagerService.GetLoginInformation().Returns(observable);

                return observable;
            }

            private IObservable<PasswordManagerResult> arrangeCallToPasswordManagerWithInvalidCredentials()
            {
                var loginInfo = new PasswordManagerResult(InvalidEmail, InvalidPassword);
                var observable = Observable.Return(loginInfo);
                PasswordManagerService.GetLoginInformation().Returns(observable);

                return observable;
            }
        }

        public sealed class TheSignupCommand : LoginViewModelTest
        {
            [Property]
            public void NavigatesToTheSignupViewModel(
                NonEmptyString emailString, NonEmptyString passwordString)
            {
                var email = Email.From(emailString.Get);
                var password = Password.From(passwordString.Get);
                ViewModel.SetEmail(email);
                ViewModel.SetPassword(password);

                ViewModel.Signup().Wait();

                NavigationService
                    .Received()
                    .Navigate<SignupViewModel, CredentialsParameter>(
                        Arg.Is<CredentialsParameter>(parameter
                            => parameter.Email.Equals(email)
                            && parameter.Password.Equals(password)
                        )
                    );
            }
        }

        public sealed class ThePrepareMethod : LoginViewModelTest
        {
            [Property]
            public void SetsTheEmail(NonEmptyString emailString)
            {
                var email = Email.From(emailString.Get);
                var password = Password.Empty;
                var parameter = CredentialsParameter.With(email, password);
                var observer = Substitute.For<IObserver<string>>();
                ViewModel.Email.Subscribe(observer);

                ViewModel.Prepare(parameter);

                observer.Received(1).OnNext(email.TrimmedEnd().ToString());
            }

            [Property]
            public void SetsThePassword(NonEmptyString passwordString)
            {
                var email = Email.Empty;
                var password = Password.From(passwordString.Get);
                var parameter = CredentialsParameter.With(email, password);
                var observer = Substitute.For<IObserver<string>>();
                ViewModel.Password.Subscribe(observer);

                ViewModel.Prepare(parameter);

                observer.Received(1).OnNext(password.ToString());
            }
        }
    }
}
