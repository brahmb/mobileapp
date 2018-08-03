using System;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class OnboardingViewModelTests
    {
        public abstract class OnboardingViewModelTest : BaseViewModelTests<OnboardingViewModel>
        {
            protected override OnboardingViewModel CreateViewModel()
                => new OnboardingViewModel(NavigationService, OnboardingStorage, AnalyticsService);
        }

        public sealed class TheConstructor : OnboardingViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useNavigationService, bool useOnboardingStorage, bool useAnalyticsService)
            {
                var analyticsService = useAnalyticsService ? AnalyticsService : null;
                var navigationService = useNavigationService ? NavigationService : null;
                var onboardingStorage = useOnboardingStorage ? OnboardingStorage : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new OnboardingViewModel(navigationService, onboardingStorage, analyticsService);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheCurrentPageProperty : OnboardingViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void DoesNotAllowUsersToSetItsValueToAValueGreaterThanTheNumberOfPagesMinusOne()
            {
                ViewModel.CurrentPage = OnboardingViewModel.TrackPage;

                ViewModel.CurrentPage = ViewModel.NumberOfPages;
                ViewModel.CurrentPage = ViewModel.NumberOfPages + 1;

                ViewModel.CurrentPage.Should().Be(OnboardingViewModel.TrackPage);
            }

            [Fact, LogIfTooSlow]
            public void DoesNotAllowUsersToSetItsValueToANegativeNumber()
            {
                ViewModel.CurrentPage = OnboardingViewModel.TrackPage;

                ViewModel.CurrentPage = -1;

                ViewModel.CurrentPage.Should().Be(OnboardingViewModel.TrackPage);
            }
        }

        public sealed class TheIsFirstPageProperty : OnboardingViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(OnboardingViewModel.TrackPage)]
            [InlineData(OnboardingViewModel.MostUsedPage)]
            [InlineData(OnboardingViewModel.ReportsPage)]
            public void OnlyReturnsTrueForTheFirstPage(int page)
            {
                ViewModel.CurrentPage = page;

                var expected = page == 0;
                ViewModel.IsFirstPage.Should().Be(expected);
            }
        }

        public sealed class TheIsLastPageProperty : OnboardingViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(OnboardingViewModel.TrackPage)]
            [InlineData(OnboardingViewModel.MostUsedPage)]
            [InlineData(OnboardingViewModel.ReportsPage)]
            public void OnlyReturnsTrueInThePageWhoseIndexEqualsToTheNumberOfPagesMinusOne(int page)
            {
                ViewModel.CurrentPage = page;

                var expected = page == ViewModel.NumberOfPages - 1;
                ViewModel.IsLastPage.Should().Be(expected);
            }
        }

        public sealed class TheSkipCommand : OnboardingViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task NavigatesToTheLoginPage()
            {
                await ViewModel.SkipCommand.ExecuteAsync();

                await NavigationService.Received().Navigate<LoginViewModel>();
            }

            [Theory, LogIfTooSlow]
            [InlineData(OnboardingViewModel.TrackPage, nameof(OnboardingViewModel.TrackPage))]
            [InlineData(OnboardingViewModel.MostUsedPage, nameof(OnboardingViewModel.MostUsedPage))]
            [InlineData(OnboardingViewModel.ReportsPage, nameof(OnboardingViewModel.ReportsPage))]
            public async Task CallsTheAnalyticsServiceIndicatingTheCurrentPage(int page, string expectedPageName)
            {
                ViewModel.CurrentPage = page;

                await ViewModel.SkipCommand.ExecuteAsync();

                AnalyticsService.Received().OnboardingSkip.Track(expectedPageName);
            }
        }

        public sealed class TheNextCommand : OnboardingViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(OnboardingViewModel.TrackPage, OnboardingViewModel.MostUsedPage)]
            [InlineData(OnboardingViewModel.MostUsedPage, OnboardingViewModel.ReportsPage)]
            public async Task AdvancesToTheNextPage(int from, int to)
            {
                ViewModel.CurrentPage = from;

                await ViewModel.NextCommand.ExecuteAsync();

                ViewModel.CurrentPage.Should().Be(to);
            }

            [Fact, LogIfTooSlow]
            public async Task CompletesOnboardingWhenOnTheLastPage()
            {
                ViewModel.CurrentPage = OnboardingViewModel.ReportsPage;

                await ViewModel.NextCommand.ExecuteAsync();

                await NavigationService.Received().Navigate<LoginViewModel>();
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheCompletedOnboardingFlagWhenUserViewsAllOnboardingPagesAndTapsOnNext()
            {
                await ViewModel.Initialize();
                while (!ViewModel.IsLastPage)
                    await ViewModel.NextCommand.ExecuteAsync();

                await ViewModel.NextCommand.ExecuteAsync();

                OnboardingStorage.Received().SetCompletedOnboarding();
            }
        }

        public sealed class ThePreviousCommand : OnboardingViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(OnboardingViewModel.MostUsedPage, OnboardingViewModel.TrackPage)]
            [InlineData(OnboardingViewModel.ReportsPage, OnboardingViewModel.MostUsedPage)]
            public void ReturnsToThePreviousPage(int from, int to)
            {
                ViewModel.CurrentPage = from;

                ViewModel.PreviousCommand.Execute();

                ViewModel.CurrentPage.Should().Be(to);
            }

            [Fact, LogIfTooSlow]
            public void CannotBeExecutedWhenOnTheFirstPage()
            {
                ViewModel.CurrentPage = OnboardingViewModel.TrackPage;

                ViewModel.PreviousCommand.CanExecute().Should().BeFalse();
            }
        }

        public sealed class TheInitializeMethod : OnboardingViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task InitializesCurrentPageToTrackPageIfUserHasNotCompletedOnboarding()
            {
                await ViewModel.Initialize();

                ViewModel.CurrentPage.Should().Be(OnboardingViewModel.TrackPage);
            }

            [Fact, LogIfTooSlow]
            public async Task NavigatesToTheLoginPageIfUserHasCompletedOnboarding()
            {
                OnboardingStorage.CompletedOnboarding().Returns(true);

                await ViewModel.Initialize();

                await NavigationService.Received().Navigate<LoginViewModel>();
            }
        }
    }
}
