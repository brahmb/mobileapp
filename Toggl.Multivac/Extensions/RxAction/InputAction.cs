using System;
using System.Reactive;

namespace Toggl.Multivac.Extensions
{
    public class InputAction<TInput> : RxAction<TInput, Unit>
    {
        public InputAction(Func<TInput, IObservable<Unit>> workFactory) : base(workFactory)
        {
        }

        public InputAction(Func<TInput, IObservable<Unit>> workFactory, IObservable<bool> enabledIf) : base(workFactory, enabledIf)
        {
        }
    }

    public static class CompletableActionExtensions
    {
        public static IObservable<Unit> Execute<TInput>(this InputAction<TInput> action, TInput value)
        {
            return action.Execute(value);
        }
    }
}
