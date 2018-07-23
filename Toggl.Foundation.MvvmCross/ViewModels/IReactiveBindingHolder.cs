using System;
using System.Reactive.Disposables;
using Toggl.Multivac.Extensions;
using Toggl.Foundation.MvvmCross.Extensions;
using System.Reactive;
using System.Threading.Tasks;
using System.Reactive.Linq;

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
    }
    
}
