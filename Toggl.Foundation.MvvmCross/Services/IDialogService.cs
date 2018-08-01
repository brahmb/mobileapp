using System;
using System.Reactive;

namespace Toggl.Foundation.MvvmCross.Services
{
    public interface IDialogService
    {
        IObservable<bool> Confirm(
            string title,
            string message,
            string confirmButtonText,
            string dismissButtonText);

        IObservable<Unit> Alert(string title, string message, string buttonTitle);

        IObservable<bool> ConfirmDestructiveAction(ActionType type);
    }

    public enum ActionType
    {
        DiscardNewTimeEntry,
        DiscardEditingChanges,
        DeleteExistingTimeEntry,
        DiscardFeedback
    }
}
