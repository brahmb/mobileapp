using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public class SendFeedbackViewModel : MvxViewModelResult<bool>
    {
        // Internal States
        private CompositeDisposable disposeBag = new CompositeDisposable();
        private ISubject<bool> errorViewVisible = new BehaviorSubject<bool>(false);
        private ISubject<bool> isLoading = new BehaviorSubject<bool>(false);

        // Inputs
        public ISubject<Unit> CloseButtonTapped = new Subject<Unit>();
        public ISubject<string> FeedbackText = new Subject<string>();
        public ISubject<Unit> ErrorViewTapped = new Subject<Unit>();
        public ISubject<Unit> SendButtonTapped = new Subject<Unit>();

        // Outputs
        public IObservable<bool> IsFeedbackEmpty { get; }
        public IObservable<bool> ErrorViewVisible { get; }
        public IObservable<bool> SendEnabled { get; }
        public IObservable<bool> IsLoading { get; }

        public SendFeedbackViewModel(
            IMvxNavigationService navigationService,
            IInteractorFactory interactorFactory,
            IDialogService dialogService
        )
        {
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(dialogService, nameof(dialogService));

            IsFeedbackEmpty = FeedbackText
                .Select(text => string.IsNullOrEmpty(text))
                .DistinctUntilChanged()
                .StartWith(true);

            SendEnabled = Observable.CombineLatest(
                IsFeedbackEmpty,
                isLoading.AsObservable(),
                (isEmpty, isLoading) => !isEmpty && !isLoading
            );

            CloseButtonTapped
                .WithLatestFrom(IsFeedbackEmpty, (_, isEmpty) => isEmpty)
                .SelectMany(isEmpty =>
                {
                    if (isEmpty)
                    {
                        return Observable.Return(true);
                    }
                    return dialogService.ConfirmDestructiveAction(ActionType.DiscardFeedback);
                })
                .Where(shouldDiscard => shouldDiscard)
                .Do(_ =>
                {
                    navigationService.Close(this, false);
                })
                .Subscribe()
                .DisposedBy(disposeBag);

            ErrorViewTapped
                .Do(e => errorViewVisible.OnNext(false))
                .Subscribe()
                .DisposedBy(disposeBag);

            SendButtonTapped
                .WithLatestFrom(FeedbackText, (_, text) => text)
                .Do(_ =>
                {
                    isLoading.OnNext(true);
                    errorViewVisible.OnNext(false);
                })
                .SelectMany(text => interactorFactory
                    .SendFeedback(text)
                    .Execute()
                    .Materialize()
                )
                .Do(notification =>
                {
                    isLoading.OnNext(false);
                    switch (notification.Kind)
                    {
                        case NotificationKind.OnError:
                            errorViewVisible.OnNext(true);
                            break;
                        default:
                            navigationService.Close(this, true);
                            break;
                    }
                })
                .Subscribe()
                .DisposedBy(disposeBag);

            ErrorViewVisible = errorViewVisible;
            IsLoading = isLoading;
        }
    }
}
