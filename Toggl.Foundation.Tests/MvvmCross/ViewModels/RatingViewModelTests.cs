using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck.Xunit;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels.Hints;
using Toggl.Foundation.Tests.Generators;
using Toggl.PrimeRadiant;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class RatingViewModelTests
    {
        public abstract class RatingViewModelTest : BaseViewModelTests<RatingViewModel>
        {
            protected DateTimeOffset CurrentDateTime { get; } = new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero);
            protected TestScheduler Scheduler { get; } = new TestScheduler();

            protected override void AdditionalSetup()
            {
                base.AdditionalSetup();

                TimeService.CurrentDateTime.Returns(CurrentDateTime);
            }

            protected override RatingViewModel CreateViewModel()
                => new RatingViewModel(
                    TimeService,
                    DataSource,
                    RatingService,
                    AnalyticsService,
                    OnboardingStorage,
                    NavigationService);
        }

        public sealed class TheConstructor : RatingViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useDataSource,
                bool useTimeService,
                bool useRatingService,
                bool useAnalyticsService,
                bool useOnboardingStorage,
                bool useNavigationService)
            {
                var dataSource = useDataSource ? DataSource : null;
                var timeService = useTimeService ? TimeService : null;
                var ratingService = useRatingService ? RatingService : null;
                var analyticsService = useAnalyticsService ? AnalyticsService : null;
                var onboardingStorage = useOnboardingStorage ? OnboardingStorage : null;
                var navigationService = useNavigationService ? NavigationService : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new RatingViewModel(
                        timeService,
                        dataSource,
                        ratingService,
                        analyticsService,
                        onboardingStorage,
                        navigationService);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheRegisterImpressionMethod : RatingViewModelTest
        {
            [Property]
            public void EmitsNewImpression(bool impressionIsPositive)
            {
                var observer = Substitute.For<IObserver<bool?>>();
                ViewModel.Impression.Subscribe(observer);

                ViewModel.RegisterImpression(impressionIsPositive);

                observer.Received().OnNext(impressionIsPositive);
            }

            [Property]
            public void TracksTheUserFinishedRatingViewFirstStepEvent(bool impressionIsPositive)
            {
                ViewModel.RegisterImpression(impressionIsPositive);

                AnalyticsService.UserFinishedRatingViewFirstStep.Received().Track(impressionIsPositive);
            }

            public abstract class RegisterImpressionMethodTest : RatingViewModelTest
            {
                protected abstract bool ImpressionIsPositive { get; }
                protected abstract string ExpectedCtaTitle { get; }
                protected abstract string ExpectedCtaDescription { get; }
                protected abstract string ExpectedCtaButtonTitle { get; }
                protected abstract RatingViewOutcome ExpectedStorageOucome { get; }

                [Fact, LogIfTooSlow]
                public void SetsTheAppropriateCtaTitle()
                {
                    var observer = Substitute.For<IObserver<string>>();
                    ViewModel.CtaTitle.Subscribe(observer);
                    ViewModel.RegisterImpression(ImpressionIsPositive);

                    observer.Received().OnNext(ExpectedCtaTitle);
                }

                [Fact, LogIfTooSlow]
                public void SetsTheAppropriateCtaDescription()
                {
                    var observer = Substitute.For<IObserver<string>>();
                    ViewModel.CtaDescription.Subscribe(observer);
                    ViewModel.RegisterImpression(ImpressionIsPositive);

                    observer.Received().OnNext(ExpectedCtaDescription);
                }

                [Fact, LogIfTooSlow]
                public void SetsTheAppropriateCtaButtonTitle()
                {
                    var observer = Substitute.For<IObserver<string>>();
                    ViewModel.CtaButtonTitle.Subscribe(observer);
                    ViewModel.RegisterImpression(ImpressionIsPositive);

                    observer.Received().OnNext(ExpectedCtaButtonTitle);
                }

                [Fact, LogIfTooSlow]
                public void StoresTheAppropriateRatingViewOutcomeAndTime()
                {
                    ViewModel.RegisterImpression(ImpressionIsPositive);

                    OnboardingStorage.Received().SetRatingViewOutcome(ExpectedStorageOucome, CurrentDateTime);
                }
            }

            public sealed class WhenImpressionIsPositive : RegisterImpressionMethodTest
            {
                protected override bool ImpressionIsPositive => true;
                protected override string ExpectedCtaTitle => Resources.RatingViewPositiveCTATitle;
                protected override string ExpectedCtaDescription => Resources.RatingViewPositiveCTADescription;
                protected override string ExpectedCtaButtonTitle => Resources.RatingViewPositiveCTAButtonTitle;
                protected override RatingViewOutcome ExpectedStorageOucome => RatingViewOutcome.PositiveImpression;
            }

            public sealed class WhenImpressionIsNegative : RegisterImpressionMethodTest
            {
                protected override bool ImpressionIsPositive => false;
                protected override string ExpectedCtaTitle => Resources.RatingViewNegativeCTATitle;
                protected override string ExpectedCtaDescription => Resources.RatingViewNegativeCTADescription;
                protected override string ExpectedCtaButtonTitle => Resources.RatingViewNegativeCTAButtonTitle;
                protected override RatingViewOutcome ExpectedStorageOucome => RatingViewOutcome.NegativeImpression;
            }
        }

        public sealed class ThePerformMainActionMethod
        {
            public abstract class PerformMainActionMethodTest : RatingViewModelTest
            {
                protected abstract void EnsureCorrectActionWasPerformed();
                protected abstract RatingViewSecondStepOutcome ExpectedEventParameterToTrack { get; }
                protected abstract RatingViewOutcome ExpectedStoragetOutcome { get; }

                [Fact, LogIfTooSlow]
                public async Task PerformsTheCorrectAction()
                {
                    await ViewModel.PerformMainAction();

                    EnsureCorrectActionWasPerformed();
                }

                [Fact, LogIfTooSlow]
                public async Task TracksTheAppropriateEventWithTheExpectedParameter()
                {
                    await ViewModel.PerformMainAction();

                    AnalyticsService
                        .UserFinishedRatingViewSecondStep
                        .Received()
                        .Track(ExpectedEventParameterToTrack);
                }

                [Fact, LogIfTooSlow]
                public async Task StoresTheAppropriateRatingViewOutcomeAndTime()
                {
                    await ViewModel.PerformMainAction();

                    OnboardingStorage
                        .Received()
                        .SetRatingViewOutcome(ExpectedStoragetOutcome, CurrentDateTime);
                }
            }

            public sealed class WhenImpressionIsPositive : PerformMainActionMethodTest
            {
                protected override RatingViewOutcome ExpectedStoragetOutcome => RatingViewOutcome.AppWasRated;
                protected override RatingViewSecondStepOutcome ExpectedEventParameterToTrack => RatingViewSecondStepOutcome.AppWasRated;

                protected override void AdditionalViewModelSetup()
                {
                    ViewModel.RegisterImpression(true);
                }

                protected override void EnsureCorrectActionWasPerformed()
                {
                    RatingService.Received().AskForRating();
                }
            }

            public sealed class WhenImpressionWasntLeft : RatingViewModelTest
            {
                [Fact, LogIfTooSlow]
                public async Task DoesNothing()
                {
                    await ViewModel.PerformMainAction();

                    RatingService.DidNotReceive().AskForRating();
                }
            }
        }

        public sealed class TheDismissMethod : RatingViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void HidesTheViewModel()
            {
                ViewModel.Dismiss();

                NavigationService.Received().ChangePresentation(
                    Arg.Is<ToggleRatingViewVisibilityHint>(hint => hint.ShouldHide == true)
                );
            }

            [Fact, LogIfTooSlow]
            public void DoesNotTrackAnythingIfImpressionWasNotLeft()
            {
                ViewModel.Dismiss();

                AnalyticsService.UserFinishedRatingViewFirstStep.DidNotReceive().Track(Arg.Any<bool>());
                AnalyticsService.UserFinishedRatingViewSecondStep.DidNotReceive().Track(Arg.Any<RatingViewSecondStepOutcome>());
            }

            [Fact, LogIfTooSlow]
            public void DoesNotSotreAnythingIfImpressionWasNotLeft()
            {
                ViewModel.Dismiss();

                OnboardingStorage.DidNotReceive().SetRatingViewOutcome(Arg.Any<RatingViewOutcome>(), Arg.Any<DateTimeOffset>());
            }

            public abstract class DismissMethodTest : RatingViewModelTest
            {
                protected abstract bool ImpressionIsPositive { get; }
                protected abstract RatingViewOutcome ExpectedStorageOutcome { get; }
                protected abstract RatingViewSecondStepOutcome ExpectedEventParameterToTrack { get; }

                protected override void AdditionalViewModelSetup()
                {
                    ViewModel.RegisterImpression(ImpressionIsPositive);
                }

                [Fact, LogIfTooSlow]
                public void StoresTheExpectedRatingViewOutcomeAndTime()
                {
                    ViewModel.Dismiss();

                    OnboardingStorage.Received().SetRatingViewOutcome(ExpectedStorageOutcome, CurrentDateTime);
                }

                public void TracksTheAppropriateEventWithTheExpectedParameter()
                {
                    ViewModel.Dismiss();

                    AnalyticsService.UserFinishedRatingViewSecondStep.Received().Track(ExpectedEventParameterToTrack);
                }
            }

            public sealed class WhenImpressionIsPositive : DismissMethodTest
            {
                protected override bool ImpressionIsPositive => true;
                protected override RatingViewOutcome ExpectedStorageOutcome => RatingViewOutcome.AppWasNotRated;
                protected override RatingViewSecondStepOutcome ExpectedEventParameterToTrack => RatingViewSecondStepOutcome.AppWasNotRated;
            }

            public sealed class WhenImpressionIsNegative : DismissMethodTest
            {
                protected override bool ImpressionIsPositive => false;
                protected override RatingViewOutcome ExpectedStorageOutcome => RatingViewOutcome.FeedbackWasNotLeft;
                protected override RatingViewSecondStepOutcome ExpectedEventParameterToTrack => RatingViewSecondStepOutcome.FeedbackWasNotLeft;
            }

            public sealed class TheIsFeedBackSuccessViewShowingProperty : RatingViewModelTest
            {
                [Fact, LogIfTooSlow]
                public void EmitsTrueWhenTapOnTheView()
                {
                    var observer = TestScheduler.CreateObserver<bool>();
                    var viewModel = CreateViewModel();

                    viewModel.IsFeedbackSuccessViewShowing.StartWith(true).Subscribe(observer);
                    viewModel.CloseFeedbackSuccessView();
                    observer.Messages.Last().Value.Value.Should().BeFalse();
                }
            }
        }
    }
}
