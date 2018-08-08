using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Ultrawave.Network;

namespace Toggl.Foundation.MvvmCross.Services
{
    public sealed class FeedbackService : IFeedbackService
    {
        private const string feedbackRecipient = "support@toggl.com";

        private readonly UserAgent userAgent;
        private readonly IMailService mailService;
        private readonly IDialogService dialogService;
        private readonly IPlatformConstants platformConstants;

        public FeedbackService(
            UserAgent userAgent,
            IMailService mailService,
            IDialogService dialogService,
            IPlatformConstants platformConstants)
        {
            Ensure.Argument.IsNotNull(userAgent, nameof(userAgent));
            Ensure.Argument.IsNotNull(mailService, nameof(mailService));
            Ensure.Argument.IsNotNull(dialogService, nameof(dialogService));
            Ensure.Argument.IsNotNull(platformConstants, nameof(platformConstants));

            this.userAgent = userAgent;
            this.mailService = mailService;
            this.dialogService = dialogService;
            this.platformConstants = platformConstants;
        }

        public async Task SubmitFeedback()
        {
            var version = userAgent.ToString();
            var phone = platformConstants.PhoneModel;
            var os = platformConstants.OperatingSystem;

            var messageBuilder = new StringBuilder();
            messageBuilder.Append("\n\n"); // 2 leading newlines, so user user can type something above this info
            messageBuilder.Append($"Version: {version}\n");
            if (phone != null)
            {
                messageBuilder.Append($"Phone: {phone}\n");
            }
            messageBuilder.Append($"OS: {os}");

            var mailResult = await mailService.Send(
                feedbackRecipient,
                platformConstants.FeedbackEmailSubject,
                messageBuilder.ToString()
            );

            if (mailResult.Success || string.IsNullOrEmpty(mailResult.ErrorTitle))
                return;

            await dialogService.Alert(
                mailResult.ErrorTitle,
                mailResult.ErrorMessage,
                Resources.Ok
            );
        }
    }
}
