using System.Reactive.Linq;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.Xunit;
using NSubstitute;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Services;
using Toggl.Ultrawave.Network;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.Services
{
    public sealed class FeedbackServiceTests
    {
        public sealed class TheSubmitFeedbackCommand : BaseMvvmCrossTests
        {
            private readonly IPlatformConstants platformConstants = Substitute.For<IPlatformConstants>();
            private readonly UserAgent userAgent = new UserAgent("Test", "0.1");
            private readonly IMailService mailService = Substitute.For<IMailService>();
            private readonly IDialogService dialogService = Substitute.For<IDialogService>();

            private readonly IFeedbackService feedbackService;

            public TheSubmitFeedbackCommand()
            {
                feedbackService = new FeedbackService(
                    userAgent,
                    mailService,
                    dialogService,
                    platformConstants);
            }

            [Property]
            public void SendsAnEmailToTogglSupport(
                NonEmptyString nonEmptyString0, NonEmptyString nonEmptyString1)
            {
                var phoneModel = nonEmptyString0.Get;
                var os = nonEmptyString1.Get;
                platformConstants.PhoneModel.Returns(phoneModel);
                platformConstants.OperatingSystem.Returns(os);

                feedbackService.SubmitFeedback().Wait();

                mailService
                    .Received()
                    .Send(
                        "support@toggl.com",
                        Arg.Any<string>(),
                        Arg.Any<string>())
                    .Wait();
            }

            [Property]
            public void SendsAnEmailWithTheProperSubject(
                NonEmptyString nonEmptyString)
            {
                var subject = nonEmptyString.Get;
                platformConstants.FeedbackEmailSubject.Returns(subject);

                feedbackService.SubmitFeedback().Wait();

                mailService.Received()
                    .Send(
                        Arg.Any<string>(),
                        subject,
                        Arg.Any<string>())
                   .Wait();
            }

            [Fact, LogIfTooSlow]
            public async Task SendsAnEmailWithAppVersionPhoneModelAndOsVersion()
            {
                platformConstants.PhoneModel.Returns("iPhone Y");
                platformConstants.OperatingSystem.Returns("iOS 4.2.0");
                var expectedMessage = $"\n\nVersion: {userAgent.ToString()}\nPhone: {platformConstants.PhoneModel}\nOS: {platformConstants.OperatingSystem}";

                await feedbackService.SubmitFeedback();

                await mailService.Received().Send(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    expectedMessage);
            }

            [Property]
            public void AlertsUserWhenMailServiceReturnsAnError(
                NonEmptyString nonEmptyString0, NonEmptyString nonEmptyString1)
            {
                var errorTitle = nonEmptyString0.Get;
                var errorMessage = nonEmptyString1.Get;
                var result = new MailResult(false, errorTitle, errorMessage);
                mailService
                    .Send(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                    .Returns(Task.FromResult(result));

                feedbackService.SubmitFeedback().Wait();

                dialogService
                    .Received()
                    .Alert(errorTitle, errorMessage, Resources.Ok)
                    .Wait();
            }

            [Theory, LogIfTooSlow]
            [InlineData(true, "")]
            [InlineData(true, "Error")]
            [InlineData(true, null)]
            [InlineData(false, "")]
            [InlineData(false, null)]
            public async Task DoesNotAlertUserWhenMailServiceReturnsSuccessOrDoesNotHaveErrorTitle(
                bool success, string errorTitle)
            {
                var result = new MailResult(success, errorTitle, "");
                mailService
                    .Send(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                    .Returns(Task.FromResult(result));

                await feedbackService.SubmitFeedback();

                await dialogService
                    .DidNotReceive()
                    .Alert(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
            }
        }
    }
}
