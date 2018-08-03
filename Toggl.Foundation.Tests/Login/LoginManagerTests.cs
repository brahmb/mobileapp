using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Login;
using Toggl.Foundation.Shortcuts;
using Toggl.Foundation.Tests.Generators;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Network;
using Xunit;
using Xunit.Sdk;
using FoundationUser = Toggl.Foundation.Models.User;
using User = Toggl.Ultrawave.Models.User;

namespace Toggl.Foundation.Tests.Login
{
    public sealed class LoginManagerTests
    {
        public abstract class LoginManagerTest
        {
            protected static readonly Password Password = "theirobotmoviesucked123".ToPassword();
            protected static readonly Email Email = "susancalvin@psychohistorian.museum".ToEmail();
            protected static readonly bool TermsAccepted = true;
            protected static readonly int CountryId = 237;

            protected IUser User { get; } = new User { Id = 10, ApiToken = "ABCDEFG" };
            protected ITogglApi Api { get; } = Substitute.For<ITogglApi>();
            protected IApiFactory ApiFactory { get; } = Substitute.For<IApiFactory>();
            protected ITogglDatabase Database { get; } = Substitute.For<ITogglDatabase>();
            protected IGoogleService GoogleService { get; } = Substitute.For<IGoogleService>();
            protected IAccessRestrictionStorage AccessRestrictionStorage { get; } = Substitute.For<IAccessRestrictionStorage>();
            protected ITogglDataSource DataSource { get; } = Substitute.For<ITogglDataSource>();
            protected IApplicationShortcutCreator ApplicationShortcutCreator { get; } = Substitute.For<IApplicationShortcutCreator>();
            protected readonly ILoginManager LoginManager;
            protected IScheduler Scheduler { get; } = System.Reactive.Concurrency.Scheduler.Default;
            protected ITogglDataSource CreateDataSource(ITogglApi api) => DataSource;
            protected virtual IScheduler CreateScheduler => Scheduler;
            protected IAnalyticsService AnalyticsService { get; } = Substitute.For<IAnalyticsService>();

            protected LoginManagerTest()
            {
                LoginManager = new LoginManager(ApiFactory, Database, GoogleService, ApplicationShortcutCreator, AccessRestrictionStorage, AnalyticsService, CreateDataSource, CreateScheduler);

                Api.User.Get().Returns(Observable.Return(User));
                Api.User.SignUp(Email, Password, TermsAccepted, CountryId).Returns(Observable.Return(User));
                Api.User.GetWithGoogle().Returns(Observable.Return(User));
                ApiFactory.CreateApiWith(Arg.Any<Credentials>()).Returns(Api);
                Database.Clear().Returns(Observable.Return(Unit.Default));
            }
        }

        public abstract class LoginManagerWithTestSchedulerTest : LoginManagerTest
        {
            protected readonly TestScheduler TestScheduler = new TestScheduler();
            protected override IScheduler CreateScheduler => TestScheduler;
        }

        public sealed class Constructor : LoginManagerTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useApiFactory,
                bool useDatabase,
                bool useGoogleService,
                bool useAccessRestrictionStorage,
                bool useApplicationShortcutCreator,
                bool useCreateDataSource,
                bool useScheduler,
                bool useAnalyticsService)
            {
                var database = useDatabase ? Database : null;
                var apiFactory = useApiFactory ? ApiFactory : null;
                var googleService = useGoogleService ? GoogleService : null;
                var accessRestrictionStorage = useAccessRestrictionStorage ? AccessRestrictionStorage : null;
                var createDataSource = useCreateDataSource ? CreateDataSource : (Func<ITogglApi, ITogglDataSource>)null;
                var shortcutCreator = useApplicationShortcutCreator ? ApplicationShortcutCreator : null;
                var testScheduler = useScheduler ? Scheduler : null;
                var analyticsService = useAnalyticsService ? AnalyticsService : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new LoginManager(apiFactory, database, googleService, shortcutCreator, accessRestrictionStorage, analyticsService, createDataSource, testScheduler);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheLoginMethod : LoginManagerTest
        {
            [Theory, LogIfTooSlow]
            [InlineData("susancalvin@psychohistorian.museum", null)]
            [InlineData("susancalvin@psychohistorian.museum", "")]
            [InlineData("susancalvin@psychohistorian.museum", " ")]
            [InlineData("susancalvin@", null)]
            [InlineData("susancalvin@", "")]
            [InlineData("susancalvin@", " ")]
            [InlineData("susancalvin@", "123456")]
            [InlineData("", null)]
            [InlineData("", "")]
            [InlineData("", " ")]
            [InlineData("", "123456")]
            [InlineData(null, null)]
            [InlineData(null, "")]
            [InlineData(null, " ")]
            [InlineData(null, "123456")]
            public void ThrowsIfYouPassInvalidParameters(string email, string password)
            {
                var actualEmail = email.ToEmail();
                var actualPassword = password.ToPassword();

                Action tryingToConstructWithEmptyParameters =
                    () => LoginManager.Login(actualEmail, actualPassword).Wait();

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentException>();
            }

            [Fact, LogIfTooSlow]
            public async Task EmptiesTheDatabaseBeforeTryingToLogin()
            {
                await LoginManager.Login(Email, Password);

                Received.InOrder(async () =>
                {
                    await Database.Clear();
                    await Api.User.Get();
                });
            }

            [Fact, LogIfTooSlow]
            public async Task CallsTheGetMethodOfTheUserApi()
            {
                await LoginManager.Login(Email, Password);

                await Api.User.Received().Get();
            }

            [Fact, LogIfTooSlow]
            public async Task PersistsTheUserToTheDatabase()
            {
                await LoginManager.Login(Email, Password);

                await Database.User.Received().Create(Arg.Is<IDatabaseUser>(receivedUser => receivedUser.Id == User.Id));
            }

            [Fact, LogIfTooSlow]
            public async Task PersistsTheUserWithTheSyncStatusSetToInSync()
            {
                await LoginManager.Login(Email, Password);

                await Database.User.Received().Create(Arg.Is<IDatabaseUser>(receivedUser => receivedUser.SyncStatus == SyncStatus.InSync));
            }

            [Fact, LogIfTooSlow]
            public async Task AlwaysReturnsASingleResult()
            {
                await LoginManager
                        .Login(Email, Password)
                        .SingleAsync();
            }

            [Fact, LogIfTooSlow]
            public async Task NotifiesShortcutCreatorAboutLogin()
            {
                await LoginManager.Login(Email, Password);

                ApplicationShortcutCreator.Received().OnLogin(Arg.Any<ITogglDataSource>());
            }

            [Fact, LogIfTooSlow]
            public void DoesNotRetryWhenTheApiThrowsSomethingOtherThanUserIsMissingApiTokenException()
            {
                var serverErrorException = Substitute.For<ServerErrorException>(Substitute.For<IRequest>(), Substitute.For<IResponse>(), "Some Exception");
                Api.User.Get().Returns(Observable.Throw<IUser>(serverErrorException));

                Action tryingToLoginWhenTheApiIsThrowingSomeRandomServerErrorException =
                    () => LoginManager.Login(Email, Password).Wait();

                tryingToLoginWhenTheApiIsThrowingSomeRandomServerErrorException
                    .Should().Throw<ServerErrorException>();
            }

            [Fact, LogIfTooSlow]
            public async Task TracksMissingApiToken()
            {
                var userIsMissingApiTokenException = new UserIsMissingApiTokenException(Substitute.For<IRequest>(), Substitute.For<IResponse>());
                Api.User.Get().Returns(Observable.Throw<IUser>(userIsMissingApiTokenException));

                try
                {
                    await LoginManager.Login(Email, Password);
                }
                catch
                {
                }

                AnalyticsService.UserIsMissingApiToken.Received().Track(Arg.Is(LoginSignupAuthenticationMethod.Login));
            }
        }

        public sealed class TheResetPasswordMethod : LoginManagerTest
        {
            [Theory, LogIfTooSlow]
            [InlineData("foo")]
            public void ThrowsWhenEmailIsInvalid(string emailString)
            {
                Action tryingToResetWithInvalidEmail = () => LoginManager
                    .ResetPassword(Email.From(emailString))
                    .Wait();

                tryingToResetWithInvalidEmail.Should().Throw<ArgumentException>();
            }

            [Fact, LogIfTooSlow]
            public async Task UsesApiWithoutCredentials()
            {
                await LoginManager.ResetPassword(Email.From("some@email.com"));

                ApiFactory.Received().CreateApiWith(Arg.Is<Credentials>(
                    arg => arg.Header.Name == null
                        && arg.Header.Value == null
                        && arg.Header.Type == HttpHeader.HeaderType.None));
            }

            [Theory, LogIfTooSlow]
            [InlineData("example@email.com")]
            [InlineData("john.smith@gmail.com")]
            [InlineData("h4cker123@domain.ru")]
            public async Task CallsApiClientWithThePassedEmailAddress(string address)
            {
                var email = address.ToEmail();

                await LoginManager.ResetPassword(email);

                await Api.User.Received().ResetPassword(Arg.Is(email));
            }
        }

        public sealed class TheGetDataSourceIfLoggedInInMethod : LoginManagerTest
        {
            [Fact, LogIfTooSlow]
            public void ReturnsNullIfTheDatabaseHasNoUsers()
            {
                var observable = Observable.Throw<IDatabaseUser>(new InvalidOperationException());
                Database.User.Single().Returns(observable);

                var result = LoginManager.GetDataSourceIfLoggedIn();

                result.Should().BeNull();
            }

            [Fact, LogIfTooSlow]
            public void ReturnsADataSourceIfTheUserExistsInTheDatabase()
            {
                var observable = Observable.Return<IDatabaseUser>(FoundationUser.Clean(User));
                Database.User.Single().Returns(observable);

                var result = LoginManager.GetDataSourceIfLoggedIn();

                result.Should().NotBeNull();
            }
        }

        public sealed class TheSignUpMethod : LoginManagerTest
        {
            [Theory, LogIfTooSlow]
            [InlineData("susancalvin@psychohistorian.museum", null)]
            [InlineData("susancalvin@psychohistorian.museum", "")]
            [InlineData("susancalvin@psychohistorian.museum", " ")]
            [InlineData("susancalvin@", null)]
            [InlineData("susancalvin@", "")]
            [InlineData("susancalvin@", " ")]
            [InlineData("susancalvin@", "123456")]
            [InlineData("", null)]
            [InlineData("", "")]
            [InlineData("", " ")]
            [InlineData("", "123456")]
            [InlineData(null, null)]
            [InlineData(null, "")]
            [InlineData(null, " ")]
            [InlineData(null, "123456")]
            public void ThrowsIfYouPassInvalidParameters(string email, string password)
            {
                var actualEmail = email.ToEmail();
                var actualPassword = password.ToPassword();

                Action tryingToConstructWithEmptyParameters =
                    () => LoginManager.SignUp(actualEmail, actualPassword, true, 0).Wait();

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentException>();
            }

            [Fact, LogIfTooSlow]
            public async Task EmptiesTheDatabaseBeforeTryingToCreateTheUser()
            {
                await LoginManager.SignUp(Email, Password, TermsAccepted, CountryId);

                Received.InOrder(async () =>
                {
                    await Database.Clear();
                    await Api.User.SignUp(Email, Password, TermsAccepted, CountryId);
                });
            }

            [Fact, LogIfTooSlow]
            public async Task CallsTheSignUpMethodOfTheUserApi()
            {
                await LoginManager.SignUp(Email, Password, TermsAccepted, CountryId);

                await Api.User.Received().SignUp(Email, Password, TermsAccepted, CountryId);
            }

            [Fact, LogIfTooSlow]
            public async Task PersistsTheUserToTheDatabase()
            {
                await LoginManager.SignUp(Email, Password, TermsAccepted, CountryId);

                await Database.User.Received().Create(Arg.Is<IDatabaseUser>(receivedUser => receivedUser.Id == User.Id));
            }

            [Fact, LogIfTooSlow]
            public async Task PersistsTheUserWithTheSyncStatusSetToInSync()
            {
                await LoginManager.SignUp(Email, Password, TermsAccepted, CountryId);

                await Database.User.Received().Create(Arg.Is<IDatabaseUser>(receivedUser => receivedUser.SyncStatus == SyncStatus.InSync));
            }

            [Fact, LogIfTooSlow]
            public async Task AlwaysReturnsASingleResult()
            {
                await LoginManager
                        .SignUp(Email, Password, TermsAccepted, CountryId)
                        .SingleAsync();
            }

            [Fact, LogIfTooSlow]
            public async Task NotifiesShortcutCreatorAboutLogin()
            {
                await LoginManager.SignUp(Email, Password, TermsAccepted, CountryId);

                ApplicationShortcutCreator.Received().OnLogin(Arg.Any<ITogglDataSource>());
            }

            [Fact, LogIfTooSlow]
            public void DoesNotRetryWhenTheApiThrowsSomethingOtherThanUserIsMissingApiTokenException()
            {
                var serverErrorException = Substitute.For<ServerErrorException>(Substitute.For<IRequest>(), Substitute.For<IResponse>(), "Some Exception");
                Api.User.SignUp(Email, Password, TermsAccepted, CountryId).Returns(Observable.Throw<IUser>(serverErrorException));

                Action tryingToSignUpWhenTheApiIsThrowingSomeRandomServerErrorException =
                    () => LoginManager.SignUp(Email, Password, TermsAccepted, CountryId).Wait();

                tryingToSignUpWhenTheApiIsThrowingSomeRandomServerErrorException
                    .Should().Throw<ServerErrorException>();
            }

            [Fact, LogIfTooSlow]
            public async Task TracksMissingApiToken()
            {
                var userIsMissingApiTokenException = new UserIsMissingApiTokenException(Substitute.For<IRequest>(), Substitute.For<IResponse>());
                Api.User.SignUp(Email, Password, TermsAccepted, CountryId).Returns(Observable.Throw<IUser>(userIsMissingApiTokenException));

                try
                {
                    await LoginManager.SignUp(Email, Password, TermsAccepted, CountryId);
                }
                catch
                {
                }

                AnalyticsService.UserIsMissingApiToken.Received().Track(Arg.Is(LoginSignupAuthenticationMethod.SignUp));
            }
        }

        public sealed class TheRefreshTokenMethod : LoginManagerTest
        {
            public TheRefreshTokenMethod()
            {
                var user = Substitute.For<IDatabaseUser>();
                user.Email.Returns(Email);
                Database.User.Single().Returns(Observable.Return(user));
            }

            [Theory, LogIfTooSlow]
            [InlineData(null)]
            [InlineData("")]
            [InlineData(" ")]
            public void ThrowsIfYouPassInvalidParameters(string password)
            {
                Action tryingToRefreshWithInvalidParameters =
                    () => LoginManager.RefreshToken(password.ToPassword()).Wait();

                tryingToRefreshWithInvalidParameters
                    .Should().Throw<ArgumentException>();
            }

            [Fact, LogIfTooSlow]
            public async Task CallsTheGetMethodOfTheUserApi()
            {
                await LoginManager.RefreshToken(Password);

                 await Api.User.Received().Get();
            }

            [Fact, LogIfTooSlow]
            public async Task PersistsTheUserToTheDatabase()
            {
                await LoginManager.RefreshToken(Password);

                await Database.User.Received().Update(Arg.Is<IDatabaseUser>(receivedUser => receivedUser.Id == User.Id));
            }

            [Fact, LogIfTooSlow]
            public async Task PersistsTheUserWithTheSyncStatusSetToInSync()
            {
                await LoginManager.RefreshToken(Password);

                await Database.User.Received().Update(Arg.Is<IDatabaseUser>(receivedUser => receivedUser.SyncStatus == SyncStatus.InSync));
            }

            [Fact, LogIfTooSlow]
            public async Task AlwaysReturnsASingleResult()
            {
                await LoginManager
                        .RefreshToken(Password)
                        .SingleAsync();
            }
        }

        public sealed class TheLoginUsingGoogleMethod : LoginManagerTest
        {
            public TheLoginUsingGoogleMethod()
            {
                GoogleService.GetAuthToken().Returns(Observable.Return("sometoken"));
            }

            [Fact, LogIfTooSlow]
            public async Task EmptiesTheDatabaseBeforeTryingToCreateTheUser()
            {
                await LoginManager.LoginWithGoogle();

                Received.InOrder(async () =>
                {
                    await Database.Clear();
                    await Api.User.GetWithGoogle();
                });
            }

            [Fact, LogIfTooSlow]
            public async Task UsesTheGoogleServiceToGetTheToken()
            {
                await LoginManager.LoginWithGoogle();

                await GoogleService.Received().GetAuthToken();
            }

            [Fact, LogIfTooSlow]
            public async Task CallsTheGetWithGoogleOfTheUserApi()
            {
                await LoginManager.LoginWithGoogle();

                await Api.User.Received().GetWithGoogle();
            }

            [Fact, LogIfTooSlow]
            public async Task PersistsTheUserToTheDatabase()
            {
                await LoginManager.LoginWithGoogle();

                await Database.User.Received().Create(Arg.Is<IDatabaseUser>(receivedUser => receivedUser.Id == User.Id));
            }

            [Fact, LogIfTooSlow]
            public async Task PersistsTheUserWithTheSyncStatusSetToInSync()
            {
                await LoginManager.LoginWithGoogle();

                await Database.User.Received().Create(Arg.Is<IDatabaseUser>(receivedUser => receivedUser.SyncStatus == SyncStatus.InSync));
            }

            [Fact, LogIfTooSlow]
            public async Task AlwaysReturnsASingleResult()
            {
                await LoginManager
                        .LoginWithGoogle()
                        .SingleAsync();
            }

            [Fact, LogIfTooSlow]
            public async Task NotifiesShortcutCreatorAboutLogin()
            {
                await LoginManager.LoginWithGoogle();

                ApplicationShortcutCreator.Received().OnLogin(Arg.Any<ITogglDataSource>());
            }

            [Fact, LogIfTooSlow]
            public async Task TracksMissingApiToken()
            {
                var userIsMissingApiTokenException = new UserIsMissingApiTokenException(Substitute.For<IRequest>(), Substitute.For<IResponse>());
                Api.User.GetWithGoogle().Returns(Observable.Throw<IUser>(userIsMissingApiTokenException));

                try
                {
                    await LoginManager.LoginWithGoogle();
                }
                catch
                {
                }

                AnalyticsService.UserIsMissingApiToken.Received().Track(Arg.Is(LoginSignupAuthenticationMethod.LoginGoogle));
            }
        }

        public sealed class TheLoginMethodRetries : LoginManagerWithTestSchedulerTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(0, 1)]
            [InlineData(1, 1)]
            [InlineData(3, 2)]
            [InlineData(5, 2)]
            [InlineData(13, 3)]
            [InlineData(100, 3)]
            public void AfterAWhileWhenTheApiThrowsUserIsMissingApiTokenException(int seconds, int apiCalls)
            {
                var userIsMissingApiTokenException = new UserIsMissingApiTokenException(Substitute.For<IRequest>(),  Substitute.For<IResponse>());
                Api.User.Get().Returns(Observable.Throw<IUser>(userIsMissingApiTokenException));
                var observer = TestScheduler.CreateObserver<ITogglDataSource>();
                TestScheduler.Start();

                LoginManager.Login(Email, Password).Subscribe(observer);
                TestScheduler.AdvanceBy(TimeSpan.FromSeconds(seconds).Ticks);

                Api.User.Received(apiCalls).Get();
            }

            [Fact, LogIfTooSlow]
            public void LoggingInWhenReceivingUserIsMissingApiTokenExceptionAndThenForwardTheErrorOnTheThirdFailure()
            {
                var userIsMissingApiTokenException = new UserIsMissingApiTokenException(Substitute.For<IRequest>(), Substitute.For<IResponse>());
                Api.User.Get().Returns(Observable.Throw<IUser>(userIsMissingApiTokenException));
                var observer = TestScheduler.CreateObserver<ITogglDataSource>();
                TestScheduler.Start();

                LoginManager.Login(Email, Password).Subscribe(observer);
                TestScheduler.AdvanceBy(TimeSpan.FromSeconds(20).Ticks);

                Api.User.Received(3).Get();
                observer.Messages.Single().Value.Exception.Should().BeOfType<UserIsMissingApiTokenException>();
            }

            [Fact, LogIfTooSlow]
            public void AndStopRetryingAfterASuccessFullLoginApiCall()
            {
                var observer = TestScheduler.CreateObserver<ITogglDataSource>();
                var userIsMissingApiTokenException = new UserIsMissingApiTokenException(Substitute.For<IRequest>(), Substitute.For<IResponse>());
                Api.User.Get().Returns(Observable.Throw<IUser>(userIsMissingApiTokenException), Observable.Return(User));
                TestScheduler.Start();

                LoginManager.Login(Email, Password).Subscribe(observer);
                TestScheduler.AdvanceBy(TimeSpan.FromDays(1).Ticks);

                Api.User.Received(2).Get();
            }

            [Fact, LogIfTooSlow]
            public void WhenReceivingUserIsMissingApiTokenExceptionOrThrowOtherExceptions()
            {
                var userIsMissingApiTokenException = new UserIsMissingApiTokenException(Substitute.For<IRequest>(), Substitute.For<IResponse>());
                var serverErrorException = Substitute.For<ServerErrorException>(Substitute.For<IRequest>(), Substitute.For<IResponse>(), "Some Exception");
                Api.User.Get().Returns(Observable.Throw<IUser>(userIsMissingApiTokenException), Observable.Throw<IUser>(serverErrorException));
                var observer = TestScheduler.CreateObserver<ITogglDataSource>();
                TestScheduler.Start();

                LoginManager.Login(Email, Password).Subscribe(observer);
                TestScheduler.AdvanceBy(TimeSpan.FromSeconds(20).Ticks);

                observer.Messages.Single().Value.Exception.Should().Be(serverErrorException);
                Api.User.Received(2).Get();
            }
        }

        public sealed class TheSignUpMethodRuns : LoginManagerWithTestSchedulerTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(0, 0)]
            [InlineData(1, 0)]
            [InlineData(3, 1)]
            [InlineData(4, 2)]
            [InlineData(5, 2)]
            [InlineData(13, 3)]
            [InlineData(100, 3)]
            public void OnceAndResumesTryingToLoginAfterAWhileWhenTheApiThrowsUserIsMissingApiTokenException(int seconds, int loginApiCalls)
            {
                var userIsMissingApiTokenException = new UserIsMissingApiTokenException(Substitute.For<IRequest>(), Substitute.For<IResponse>());
                Api.User.SignUp(Email, Password, TermsAccepted, CountryId).Returns(Observable.Throw<IUser>(userIsMissingApiTokenException));
                Api.User.Get().Returns(Observable.Throw<IUser>(userIsMissingApiTokenException));
                var observer = TestScheduler.CreateObserver<ITogglDataSource>();
                TestScheduler.Start();

                LoginManager.SignUp(Email, Password, TermsAccepted, CountryId).Subscribe(observer);
                TestScheduler.AdvanceBy(TimeSpan.FromSeconds(seconds).Ticks);

                Api.User.Received(1).SignUp(Email, Password, TermsAccepted, CountryId);
                Api.User.Received(loginApiCalls).Get();
            }

            [Fact, LogIfTooSlow]
            public void AndWhenReceivingUserIsMissingApiTokenExceptionWillResumeTryingToLoginThreeTimesBeforeThrowingIt()
            {
                var userIsMissingApiTokenException = new UserIsMissingApiTokenException(Substitute.For<IRequest>(), Substitute.For<IResponse>());
                Api.User.SignUp(Email, Password, TermsAccepted, CountryId).Returns(Observable.Throw<IUser>(userIsMissingApiTokenException));
                Api.User.Get().Returns(Observable.Throw<IUser>(userIsMissingApiTokenException));
                var observer = TestScheduler.CreateObserver<ITogglDataSource>();
                TestScheduler.Start();

                LoginManager.SignUp(Email, Password, TermsAccepted, CountryId).Subscribe(observer);
                TestScheduler.AdvanceBy(TimeSpan.FromSeconds(20).Ticks);

                Api.User.Received(1).SignUp(Email, Password, TermsAccepted, CountryId);
                Api.User.Received(3).Get();
                observer.Messages.Single().Value.Exception.Should().BeOfType<UserIsMissingApiTokenException>();
            }

            [Fact, LogIfTooSlow]
            public void AndWhenReceivingUserIsMissingApiTokenExceptionWillResumeWithTheLoginFlow()
            {
                var userIsMissingApiTokenException = new UserIsMissingApiTokenException(Substitute.For<IRequest>(), Substitute.For<IResponse>());
                var serverErrorException = Substitute.For<ServerErrorException>(Substitute.For<IRequest>(), Substitute.For<IResponse>(), "Some Exception");
                Api.User.SignUp(Email, Password, TermsAccepted, CountryId).Returns(Observable.Throw<IUser>(userIsMissingApiTokenException));
                Api.User.Get().Returns(Observable.Throw<IUser>(serverErrorException));
                var observer = TestScheduler.CreateObserver<ITogglDataSource>();
                TestScheduler.Start();

                LoginManager.SignUp(Email, Password, TermsAccepted, CountryId).Subscribe(observer);
                TestScheduler.AdvanceBy(TimeSpan.FromSeconds(20).Ticks);

                observer.Messages.Single().Value.Exception.Should().Be(serverErrorException);
                Api.User.Received(1).SignUp(Email, Password, TermsAccepted, CountryId);
                Api.User.Received(1).Get();
            }
        }

        public sealed class TheSignUpWithGoogleMethod : LoginManagerTest
        {
            [Fact, LogIfTooSlow]
            public async Task TracksMissingApiToken()
            {
                var userIsMissingApiTokenException = new UserIsMissingApiTokenException(Substitute.For<IRequest>(), Substitute.For<IResponse>());
                Api.User.SignUpWithGoogle(Arg.Any<string>()).Returns(Observable.Throw<IUser>(userIsMissingApiTokenException));
                GoogleService.GetAuthToken().Returns(Observable.Return("abc"));

                try
                {
                    await LoginManager.SignUpWithGoogle();
                }
                catch
                {
                }

                AnalyticsService.UserIsMissingApiToken.Received().Track(Arg.Is(LoginSignupAuthenticationMethod.SignUpWithGoogle));
            }
        }

        public sealed class TheSignUpWithGoogleMethodRetries : LoginManagerWithTestSchedulerTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(0, 1)]
            [InlineData(1, 1)]
            [InlineData(3, 2)]
            [InlineData(5, 2)]
            [InlineData(13, 3)]
            [InlineData(100, 3)]
            public void AfterAWhileWhenTheApiThrowsUserIsMissingApiTokenException(int seconds, int apiCalls)
            {
                var userIsMissingApiTokenException = new UserIsMissingApiTokenException(Substitute.For<IRequest>(),  Substitute.For<IResponse>());
                GoogleService.GetAuthToken().Returns(Observable.Return("sometoken"));
                Api.User.GetWithGoogle().Returns(Observable.Throw<IUser>(userIsMissingApiTokenException));
                var observer = TestScheduler.CreateObserver<ITogglDataSource>();
                TestScheduler.Start();

                LoginManager.LoginWithGoogle().Subscribe(observer);
                TestScheduler.AdvanceBy(TimeSpan.FromSeconds(seconds).Ticks);

                Api.User.Received(apiCalls).GetWithGoogle();
            }

            [Fact, LogIfTooSlow]
            public void LoggingInWithGoogleWhenReceivingUserIsMissingApiTokenExceptionAndThenForwardTheErrorOnTheThirdFailure()
            {
                var userIsMissingApiTokenException = new UserIsMissingApiTokenException(Substitute.For<IRequest>(), Substitute.For<IResponse>());
                GoogleService.GetAuthToken().Returns(Observable.Return("sometoken"));
                Api.User.GetWithGoogle().Returns(Observable.Throw<IUser>(userIsMissingApiTokenException));
                var observer = TestScheduler.CreateObserver<ITogglDataSource>();
                TestScheduler.Start();

                LoginManager.LoginWithGoogle().Subscribe(observer);
                TestScheduler.AdvanceBy(TimeSpan.FromSeconds(20).Ticks);

                Api.User.Received(3).GetWithGoogle();
                observer.Messages.Single().Value.Exception.Should().BeOfType<UserIsMissingApiTokenException>();
            }

            [Fact, LogIfTooSlow]
            public void AndStopRetryingAfterASuccessfullLoginWithGoogleApiCall()
            {
                var observer = TestScheduler.CreateObserver<ITogglDataSource>();
                var userIsMissingApiTokenException = new UserIsMissingApiTokenException(Substitute.For<IRequest>(), Substitute.For<IResponse>());
                GoogleService.GetAuthToken().Returns(Observable.Return("sometoken"));
                Api.User.GetWithGoogle().Returns(Observable.Throw<IUser>(userIsMissingApiTokenException), Observable.Return(User));
                TestScheduler.Start();

                LoginManager.LoginWithGoogle().Subscribe(observer);
                TestScheduler.AdvanceBy(TimeSpan.FromDays(1).Ticks);

                Api.User.Received(2).GetWithGoogle();
            }

            [Fact, LogIfTooSlow]
            public void WhenReceivingUserIsMissingApiTokenExceptionOrThrowOtherExceptions()
            {
                var userIsMissingApiTokenException = new UserIsMissingApiTokenException(Substitute.For<IRequest>(), Substitute.For<IResponse>());
                var serverErrorException = Substitute.For<ServerErrorException>(Substitute.For<IRequest>(), Substitute.For<IResponse>(), "Some Exception");
                GoogleService.GetAuthToken().Returns(Observable.Return("sometoken"));
                Api.User.GetWithGoogle().Returns(Observable.Throw<IUser>(userIsMissingApiTokenException), Observable.Throw<IUser>(serverErrorException));
                var observer = TestScheduler.CreateObserver<ITogglDataSource>();
                TestScheduler.Start();

                LoginManager.LoginWithGoogle().Subscribe(observer);
                TestScheduler.AdvanceBy(TimeSpan.FromSeconds(20).Ticks);

                observer.Messages.Single().Value.Exception.Should().Be(serverErrorException);
                Api.User.Received(2).GetWithGoogle();
            }
        }

        public sealed class TheSignUpWithGoogleMethodRuns : LoginManagerWithTestSchedulerTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(0, 0)]
            [InlineData(1, 0)]
            [InlineData(3, 1)]
            [InlineData(4, 2)]
            [InlineData(5, 2)]
            [InlineData(13, 3)]
            [InlineData(100, 3)]
            public void OnceAndResumesTryingToLoginAfterAWhileWhenTheApiThrowsUserIsMissingApiTokenException(int seconds, int loginApiCalls)
            {
                var userIsMissingApiTokenException = new UserIsMissingApiTokenException(Substitute.For<IRequest>(), Substitute.For<IResponse>());
                GoogleService.GetAuthToken().Returns(Observable.Return("sometoken"));
                Api.User.SignUpWithGoogle(Arg.Any<string>()).Returns(Observable.Throw<IUser>(userIsMissingApiTokenException));
                Api.User.GetWithGoogle().Returns(Observable.Throw<IUser>(userIsMissingApiTokenException));
                var observer = TestScheduler.CreateObserver<ITogglDataSource>();
                TestScheduler.Start();

                LoginManager.SignUpWithGoogle().Subscribe(observer);
                TestScheduler.AdvanceBy(TimeSpan.FromSeconds(seconds).Ticks);

                Api.User.Received(1).SignUpWithGoogle(Arg.Any<string>());
                Api.User.Received(loginApiCalls).GetWithGoogle();
            }

            [Fact, LogIfTooSlow]
            public void AndWhenReceivingUserIsMissingApiTokenExceptionWillResumeTryingToLoginWithGoogleThreeTimesBeforeThrowingIt()
            {
                var userIsMissingApiTokenException = new UserIsMissingApiTokenException(Substitute.For<IRequest>(), Substitute.For<IResponse>());
                GoogleService.GetAuthToken().Returns(Observable.Return("sometoken"));
                Api.User.SignUpWithGoogle(Arg.Any<string>()).Returns(Observable.Throw<IUser>(userIsMissingApiTokenException));
                Api.User.GetWithGoogle().Returns(Observable.Throw<IUser>(userIsMissingApiTokenException));
                var observer = TestScheduler.CreateObserver<ITogglDataSource>();
                TestScheduler.Start();

                LoginManager.SignUpWithGoogle().Subscribe(observer);
                TestScheduler.AdvanceBy(TimeSpan.FromSeconds(20).Ticks);

                Api.User.Received(1).SignUpWithGoogle(Arg.Any<string>());
                Api.User.Received(3).GetWithGoogle();
                observer.Messages.Single().Value.Exception.Should().BeOfType<UserIsMissingApiTokenException>();
            }

            [Fact, LogIfTooSlow]
            public void AndWhenReceivingUserIsMissingApiTokenExceptionWillResumeWithTheLoginWithGoogleFlow()
            {
                var userIsMissingApiTokenException = new UserIsMissingApiTokenException(Substitute.For<IRequest>(), Substitute.For<IResponse>());
                var serverErrorException = Substitute.For<ServerErrorException>(Substitute.For<IRequest>(), Substitute.For<IResponse>(), "Some Exception");
                GoogleService.GetAuthToken().Returns(Observable.Return("sometoken"));
                Api.User.SignUpWithGoogle(Arg.Any<string>()).Returns(Observable.Throw<IUser>(userIsMissingApiTokenException));
                Api.User.GetWithGoogle().Returns(Observable.Throw<IUser>(serverErrorException));
                var observer = TestScheduler.CreateObserver<ITogglDataSource>();
                TestScheduler.Start();

                LoginManager.SignUpWithGoogle().Subscribe(observer);
                TestScheduler.AdvanceBy(TimeSpan.FromSeconds(20).Ticks);

                observer.Messages.Single().Value.Exception.Should().Be(serverErrorException);
                Api.User.Received(1).SignUpWithGoogle(Arg.Any<string>());
                Api.User.Received(1).GetWithGoogle();
            }
        }
    }
}
