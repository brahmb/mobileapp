using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class SendFeedbackViewModelTests
    {
        public abstract class SendFeedbackViewModelTest : BaseViewModelTests<SendFeedbackViewModel>
        {
            protected override SendFeedbackViewModel CreateViewModel()
                => new SendFeedbackViewModel(NavigationService, InteractorFactory, DialogService);
        }

        public sealed class TheConstructor : SendFeedbackViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useNavigationService,
                bool useInteractorFactory,
                bool useDialogService
            )
            {
                var navigationService = useNavigationService ? NavigationService : null;
                var interactorFactory = useInteractorFactory ? InteractorFactory : null;
                var dialogService = useDialogService ? DialogService : null;

                Action tryingToConstructWithEmptyParameters = ()
                    => new SendFeedbackViewModel(navigationService, interactorFactory, dialogService);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheIsFeedbackEmptyObservable : SendFeedbackViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void EmitsValueBasedOnFeedbackText()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                var viewModel = CreateViewModel();

                viewModel.IsFeedbackEmpty.Subscribe(observer);
                viewModel.FeedbackText.OnNext(string.Empty);
                viewModel.FeedbackText.OnNext("some value");
                viewModel.FeedbackText.OnNext(string.Empty);

                observer.Messages.Select(r => r.Value.Value).TakeLast(3).AssertEqual(true, false, true);
            }

            [Fact, LogIfTooSlow]
            public void StartWithTrue()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                var viewModel = CreateViewModel();

                viewModel.IsFeedbackEmpty.Subscribe(observer);
                observer.Messages.First().Value.Value.Should().BeTrue();
            }
        }

        public sealed class TheSendEnabledProperty : SendFeedbackViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void StartsWithFalse()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                var viewModel = CreateViewModel();

                viewModel.SendEnabled.Subscribe(observer);
                observer.Messages.Single().Value.Value.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void EmitsValueBasedOnFeedbackText()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                var viewModel = CreateViewModel();

                viewModel.SendEnabled.Subscribe(observer);

                viewModel.FeedbackText.OnNext(string.Empty);
                viewModel.FeedbackText.OnNext("some value");
                viewModel.FeedbackText.OnNext(string.Empty);

                observer.Messages.TakeLast(3).Select(r => r.Value.Value).AssertEqual(false, true, false);
            }

            [Fact, LogIfTooSlow]
            public void EmitsFalseWhenFeedbackBeingSent()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                var viewModel = CreateViewModel();

                viewModel.SendEnabled.Subscribe(observer);

                viewModel.FeedbackText.OnNext("some value");
                viewModel.SendButtonTapped.OnNext(Unit.Default);

                observer.Messages.Last().Value.Value.Should().BeTrue();
            }
        }

        public sealed class TheIsLoadingProperty : SendFeedbackViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void EmitsTrueWhenFeedbackBeingSent()
            {

                SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

                var mockedFeedbackInteractor = Substitute.For<IInteractor<IObservable<Unit>>>();
                InteractorFactory.SendFeedback(Arg.Any<string>()).Returns(mockedFeedbackInteractor);
                mockedFeedbackInteractor.Execute().Returns(Observable.Never<Unit>());
                var observer = TestScheduler.CreateObserver<bool>();
                var viewModel = CreateViewModel();

                viewModel.IsLoading.StartWith(false).Subscribe(observer);
                viewModel.FeedbackText.OnNext("some value");
                viewModel.SendButtonTapped.OnNext(Unit.Default);

                observer.Messages.Last().Value.Value.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void EmitsFalseAfterNetworkRequestFailed()
            {
                var mockedFeedbackInteractor = Substitute.For<IInteractor<IObservable<Unit>>>();
                InteractorFactory.SendFeedback(Arg.Any<string>()).Returns(mockedFeedbackInteractor);
                mockedFeedbackInteractor.Execute().Returns(Observable.Throw<Unit>(new Exception()));
                var observer = TestScheduler.CreateObserver<bool>();
                var viewModel = CreateViewModel();

                viewModel.IsLoading.StartWith(true).Subscribe(observer);
                viewModel.FeedbackText.OnNext("some value");
                viewModel.SendButtonTapped.OnNext(Unit.Default);

                observer.Messages.Last().Value.Value.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void EmitsFalseAfterNetworkRequestSucceed()
            {
                var mockedFeedbackInteractor = Substitute.For<IInteractor<IObservable<Unit>>>();
                mockedFeedbackInteractor.Execute().Returns(Observable.Return(Unit.Default));
                InteractorFactory.SendFeedback(Arg.Any<string>()).Returns(mockedFeedbackInteractor);
                var observer = TestScheduler.CreateObserver<bool>();
                var viewModel = CreateViewModel();

                viewModel.IsLoading.StartWith(true).Subscribe(observer);
                viewModel.FeedbackText.OnNext("some value");
                viewModel.SendButtonTapped.OnNext(Unit.Default);

                observer.Messages.Last().Value.Value.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void EmitsFalseWhenUserDismissTheView()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                var viewModel = CreateViewModel();

                viewModel.IsLoading.StartWith(true).Subscribe(observer);
                viewModel.CloseButtonTapped.OnNext(Unit.Default);

                observer.Messages.Last().Value.Value.Should().BeFalse();
            }
        }

        public sealed class TheErrorViewVisibleProperty : SendFeedbackViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void EmitsFalseWhenUserTapOnErrorView()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                var viewModel = CreateViewModel();

                viewModel.ErrorViewVisible.StartWith(true).Subscribe(observer);

                viewModel.ErrorViewTapped.OnNext(Unit.Default);

                observer.Messages.Last().Value.Value.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void EmitsTrueWhenNetworkFails()
            {

                var mockedFeedbackInteractor = Substitute.For<IInteractor<IObservable<Unit>>>();
                InteractorFactory.SendFeedback(Arg.Any<string>()).Returns(mockedFeedbackInteractor);

                var observer = TestScheduler.CreateObserver<bool>();
                var viewModel = CreateViewModel();

                viewModel.ErrorViewVisible.StartWith(false).Subscribe(observer);
                mockedFeedbackInteractor.Execute().Returns(Observable.Throw<Unit>(new Exception()));

                viewModel.FeedbackText.OnNext("some value");
                viewModel.SendButtonTapped.OnNext(Unit.Default);

                observer.Messages.Last().Value.Value.Should().BeTrue();
            }
        }
    }
}
