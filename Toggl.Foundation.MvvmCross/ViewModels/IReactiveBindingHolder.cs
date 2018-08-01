using System;
using System.Reactive.Disposables;
using Toggl.Multivac.Extensions;
using Toggl.Foundation.MvvmCross.Extensions;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Reactive.Linq;
using MvvmCross.Binding.BindingContext;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    public interface IReactiveBindingHolder
    {
        CompositeDisposable DisposeBag { get; }
    }

    public static class ReactiveBindingHolderExtensions
    {
        public static void Bind<T>(this IReactiveBindingHolder holder, IObservable<T> observable, Action<T> onNext)
        {
            observable
                .AsDriver()
                .Subscribe(onNext)
                .DisposedBy(holder.DisposeBag);
        }

        public static void BindVoid(this IReactiveBindingHolder holder, IObservable<Unit> observable, Action onNext)
        {
            observable
                .AsDriver()
                .VoidSubscribe(onNext)
                .DisposedBy(holder.DisposeBag);
        }

        public static void Bind(this IReactiveBindingHolder holder, IObservable<Unit> observable, Func<Task> onNext)
        {
            observable
                .AsDriver()
                .Subscribe(onNext)
                .DisposedBy(holder.DisposeBag);
        }

        public static void Bind<T>(this IReactiveBindingHolder holder, IObservable<T> observable, ISubject<T> subject)
        {
            observable
                .AsDriver()
                .Subscribe(subject.OnNext)
                .DisposedBy(holder.DisposeBag);
        }
      
        public static void Bind<TInput, TOutput>(this IReactiveBindingHolder holder, IObservable<TInput> observable, RxAction<TInput, TOutput> action)
        {
            observable
                .Subscribe(action.Inputs)
                .DisposedBy(holder.DisposeBag);
        }
    }

}
