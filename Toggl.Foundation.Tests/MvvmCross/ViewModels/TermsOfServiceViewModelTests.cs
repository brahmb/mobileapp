using System;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class TermsOfServiceViewModelTests
    {
        public abstract class TermsOfServiceViewModelTest
            : BaseViewModelTests<TermsOfServiceViewModel>
        {
            protected override TermsOfServiceViewModel CreateViewModel()
                => new TermsOfServiceViewModel(BrowserService, NavigationService);
        }

        public sealed class TheConstructor : TermsOfServiceViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useBrowserService,
                bool useNavigationService)
            {
                var browserService = useBrowserService ? BrowserService : null;
                var navigationService = useNavigationService ? NavigationService : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new TermsOfServiceViewModel(browserService, navigationService);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheViewTermsOfServiceCommand : TermsOfServiceViewModelTest
        {
            private const string termsOfServiceUrl = "https://toggl.com/legal/terms/";

            [Fact, LogIfTooSlow]
            public void OpensTermsOfService()
            {
                ViewModel.ViewTermsOfServiceCommand.Execute();

                BrowserService.Received().OpenUrl(termsOfServiceUrl);
            }
        }

        public sealed class TheViewPrivacyPolicyCommand : TermsOfServiceViewModelTest
        {
            private const string privacyPolicyUrl = "https://toggl.com/legal/privacy/";

            [Fact, LogIfTooSlow]
            public void OpensPrivacyPolicy()
            {
                ViewModel.ViewPrivacyPolicyCommand.Execute();

                BrowserService.Received().OpenUrl(privacyPolicyUrl);
            }
        }

        public sealed class TheCloseCommand : TermsOfServiceViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModelAndReturnsFalse()
            {
                await ViewModel.CloseCommand.ExecuteAsync();

                await NavigationService.Received().Close(ViewModel, false);
            }
        }

        public sealed class TheAcceptCommand : TermsOfServiceViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModelAndReturnsTrue()
            {
                await ViewModel.AcceptCommand.ExecuteAsync();

                await NavigationService.Received().Close(ViewModel, true);
            }
        }
    }
}
