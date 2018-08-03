using System;
using System.Reactive.Linq;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Toggl.Multivac;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Network;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class ForgotPasswordViewModelTests
    {
        public abstract class ForgotPasswordViewModelTest : BaseViewModelTests<ForgotPasswordViewModel>
        {
            protected Email ValidEmail { get; } = Email.From("person@company.com");
            protected Email InvalidEmail { get; } = Email.From("This is not an email");

            protected override ForgotPasswordViewModel CreateViewModel()
                => new ForgotPasswordViewModel(TimeService, LoginManager, AnalyticsService, NavigationService);
        }

        public sealed class TheConstructor : ForgotPasswordViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useTimeService,
                bool useLoginManager,
                bool useAnalyticsService,
                bool useNavigationService)
            {
                var timeService = useTimeService ? TimeService : null;
                var loginManager = useLoginManager ? LoginManager : null;
                var analyticsSerivce = useAnalyticsService ? AnalyticsService : null;
                var navigationService = useNavigationService ? NavigationService : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new ForgotPasswordViewModel(
                        timeService, loginManager, analyticsSerivce, navigationService);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class ThePrepareMethod : ForgotPasswordViewModelTest
        {
            [Property]
            public void SetsTheEmail(NonEmptyString emailString)
            {
                var email = Email.From(emailString.Get);

                ViewModel.Prepare(EmailParameter.With(email));

                ViewModel.Email.Should().Be(email);
            }
        }

        public sealed class TheResetCommand : ForgotPasswordViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void SetsIsLoadingToTrue()
            {
                ViewModel.Email = ValidEmail;
                LoginManager
                    .ResetPassword(Arg.Any<Email>())
                    .Returns(Observable.Never<string>());

                ViewModel.ResetCommand.Execute();

                ViewModel.IsLoading.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void SetsErrorMessageToEmpty()
            {
                ViewModel.Email = ValidEmail;
                LoginManager
                    .ResetPassword(Arg.Any<Email>())
                    .Returns(Observable.Never<string>());

                ViewModel.ResetCommand.Execute();

                ViewModel.ErrorMessage.Should().BeEmpty();
            }

            [Fact, LogIfTooSlow]
            public void SetsPasswordResetSuccessfulToFalse()
            {
                ViewModel.Email = ValidEmail;
                LoginManager
                    .ResetPassword(Arg.Any<Email>())
                    .Returns(Observable.Never<string>());

                ViewModel.ResetCommand.Execute();

                ViewModel.PasswordResetSuccessful.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void ResetsThePassword()
            {
                ViewModel.Email = ValidEmail;

                ViewModel.ResetCommand.Execute();

                LoginManager.Received().ResetPassword(ViewModel.Email);
            }

            [Fact, LogIfTooSlow]
            public void TracksPasswordReset()
            {
                ViewModel.Email = ValidEmail;

                ViewModel.ResetCommand.Execute();

                AnalyticsService.Received().ResetPassword.Track();
            }

            [Fact, LogIfTooSlow]
            public void CannotExecuteWhenEmailIsNotValid()
            {
                ViewModel.Email = InvalidEmail;

                ViewModel.ResetCommand.CanExecute().Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void CannotExecuteWhenEmailIsEmpty()
            {
                ViewModel.ResetCommand.CanExecute().Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void CannotExecuteIfIsLoading()
            {
                ViewModel.Email = ValidEmail;
                LoginManager
                    .ResetPassword(Arg.Any<Email>())
                    .Returns(Observable.Never<string>());

                ViewModel.ResetCommand.Execute();

                ViewModel.ResetCommand.CanExecute().Should().BeFalse();
            }

            public sealed class WhenPasswordResetSucceeds : ForgotPasswordViewModelTest
            {
                [Fact, LogIfTooSlow]
                public void SetsIsLoadingToFalse()
                {
                    ViewModel.Email = ValidEmail;
                    LoginManager
                        .ResetPassword(Arg.Any<Email>())
                        .Returns(Observable.Return("Great success"));

                    ViewModel.ResetCommand.Execute();

                    ViewModel.IsLoading.Should().BeFalse();
                }

                [Fact, LogIfTooSlow]
                public void SetsPasswordResetSuccessfulToTrue()
                {
                    ViewModel.Email = ValidEmail;
                    LoginManager
                        .ResetPassword(Arg.Any<Email>())
                        .Returns(Observable.Return("Great success"));

                    ViewModel.ResetCommand.Execute();

                    ViewModel.PasswordResetSuccessful.Should().BeTrue();
                }

                [Fact, LogIfTooSlow]
                public void CallsTimeServiceToCloseViewModelAfterFourSeconds()
                {
                    ViewModel.Email = ValidEmail;
                    LoginManager
                        .ResetPassword(Arg.Any<Email>())
                        .Returns(Observable.Return("Great success"));

                    ViewModel.ResetCommand.Execute();

                    TimeService.Received().RunAfterDelay(TimeSpan.FromSeconds(4), Arg.Any<Action>());
                }

                [Fact, LogIfTooSlow]
                public void ClosesTheViewModelAfterFourSecondDelay()
                {
                    var scheduler = new TestScheduler();
                    var timeService = new TimeService(scheduler);
                    var viewModel = new ForgotPasswordViewModel(
                        timeService, LoginManager, AnalyticsService, NavigationService);
                    viewModel.Email = ValidEmail;
                    LoginManager
                        .ResetPassword(Arg.Any<Email>())
                        .Returns(Observable.Return("Great success"));

                    viewModel.ResetCommand.Execute();
                    scheduler.AdvanceBy(TimeSpan.FromSeconds(4).Ticks);

                    NavigationService
                        .Received()
                        .Close(
                            viewModel,
                            Arg.Is<EmailParameter>(
                                parameter => parameter.Email.Equals(viewModel.Email)));
                }
            }

            public sealed class WhenPasswordResetFails : ForgotPasswordViewModelTest
            {
                [Fact, LogIfTooSlow]
                public void SetsIsLoadingToFalse()
                {
                    ViewModel.Email = ValidEmail;
                    LoginManager
                        .ResetPassword(Arg.Any<Email>())
                        .Returns(Observable.Throw<string>(new Exception()));

                    ViewModel.ResetCommand.Execute();

                    ViewModel.IsLoading.Should().BeFalse();
                }

                [Fact, LogIfTooSlow]
                public void SetsNoEmailErrorWhenReceivesBadRequestException()
                {
                    ViewModel.Email = ValidEmail;
                    var exception = new BadRequestException(
                        Substitute.For<IRequest>(), Substitute.For<IResponse>());
                    LoginManager
                        .ResetPassword(Arg.Any<Email>())
                        .Returns(Observable.Throw<string>(exception));

                    ViewModel.ResetCommand.Execute();

                    ViewModel.ErrorMessage.Should().Be(Resources.PasswordResetEmailDoesNotExistError);
                }

                [Fact, LogIfTooSlow]
                public void SetsOfflineErrorWhenReceivesOfflineException()
                {
                    ViewModel.Email = ValidEmail;
                    LoginManager
                        .ResetPassword(Arg.Any<Email>())
                        .Returns(Observable.Throw<string>(new OfflineException()));

                    ViewModel.ResetCommand.Execute();

                    ViewModel.ErrorMessage.Should().Be(Resources.PasswordResetOfflineError);
                }

                [Property]
                public void SetsApiErrorWhenReceivesApiException(NonEmptyString errorString)
                {
                    ViewModel.Email = ValidEmail;
                    var exception = new ApiException(
                        Substitute.For<IRequest>(),
                        Substitute.For<IResponse>(),
                        errorString.Get);
                    LoginManager
                        .ResetPassword(Arg.Any<Email>())
                        .Returns(Observable.Throw<string>(exception));

                    ViewModel.ResetCommand.Execute();

                    ViewModel.ErrorMessage.Should().Be(exception.LocalizedApiErrorMessage);
                }

                [Fact, LogIfTooSlow]
                public void SetsGeneralErrorForAnyOtherException()
                {
                    ViewModel.Email = ValidEmail;
                    LoginManager
                        .ResetPassword(Arg.Any<Email>())
                        .Returns(Observable.Throw<string>(new Exception()));

                    ViewModel.ResetCommand.Execute();

                    ViewModel.ErrorMessage.Should().Be(Resources.PasswordResetGeneralError);
                }
            }
        }

        public sealed class TheCloseCommand : ForgotPasswordViewModelTest
        {
            [Property]
            public void ClosesTheViewModelReturningTheEmail(NonEmptyString emailString)
            {
                var email = Email.From(emailString.Get);
                ViewModel.Email = email;

                ViewModel.CloseCommand.Execute();

                NavigationService
                    .Received()
                    .Close(
                        ViewModel,
                        Arg.Is<EmailParameter>(
                            parameter => parameter.Email.Equals(ViewModel.Email)));
            }
        }
    }
}
