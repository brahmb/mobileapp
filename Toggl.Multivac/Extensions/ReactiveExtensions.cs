using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace Toggl.Multivac.Extensions
{
    public static class ReactiveExtensions
    {
        private class Observer<T> : IObserver<T>
        {
            private readonly Action<Exception> onError;
            private readonly Action onCompleted;

            public Observer(Action<Exception> onError, Action onCompleted)
            {
                this.onError = onError;
                this.onCompleted = onCompleted;
            }

            public void OnCompleted()
                => onCompleted();

            public void OnError(Exception error)
                => onError(error);

            public void OnNext(T value) { }
        }

        public static IDisposable Subscribe<T>(this IObservable<T> observable, Action<Exception> onError, Action onCompleted)
        {
            var observer = new Observer<T>(onError, onCompleted);
            return observable.Subscribe(observer);
        }

        public static IDisposable Subscribe<T>(this IObservable<T> observable, Action<Exception> onError)
        {
            var observer = new Observer<T>(onError, () => { });
            return observable.Subscribe(observer);
        }

        public static IObservable<T> ConnectedReplay<T>(this IObservable<T> observable)
        {
            var replayed = observable.Replay();
            replayed.Connect();
            return replayed;
        }

        public static IObservable<T> DelayIf<T>(this IObservable<T> observable, Predicate<T> predicate, TimeSpan delay)
            => observable.SelectMany(value => predicate(value)
                ? Observable.Return(value).Delay(delay)
                : Observable.Return(value));

        public static IObservable<T> RetryWhen<T, U>(this IObservable<T> source, Func<IObservable<Exception>, IObservable<U>> handler)
        {
            return Observable.Defer(() =>
            {
                var errorSignal = new Subject<Exception>();
                var retrySignal = handler(errorSignal);
                var sources = new BehaviorSubject<IObservable<T>>(source);

                return Observable.Using(
                        () => retrySignal.Select(s => source).Subscribe(sources),
                        r => sources
                            .Select(src =>
                                src.Do(v => { }, e => errorSignal.OnNext(e), () => errorSignal.OnCompleted())
                                   .OnErrorResumeNext(Observable.Empty<T>())
                            )
                            .Concat()
                    );
            });
        }

        public static IObservable<T> Share<T>(this IObservable<T> observable)
            => observable.Publish().RefCount();

        public static IObservable<T> AsDriver<T>(this IObservable<T> observable, T onErrorJustReturn)
            => observable
                .Replay(1).RefCount()
                .Catch(Observable.Return(onErrorJustReturn))
                .ObserveOn(SynchronizationContext.Current);

        public static IObservable<TValue> NotNullable<TValue>(this IObservable<TValue?> observable)
            where TValue : struct
            => observable.Where(x => x != null).Select(x => x.Value);

        public static IObservable<U> Select<T, U>(this IObservable<T> observable, U u)
            => observable.Select(_ => u);

        public static IObservable<Unit> SelectUnit<T>(this IObservable<T> observable)
            => observable.Select(Unit.Default);

        public static IObservable<T> Debug<T>(this IObservable<T> observable, string tag = "")
            => observable.Do(
                x => Console.WriteLine($"OnNext {tag}: {x}"),
                ex => Console.WriteLine($"OnError {tag}: {ex}"),
                () => Console.WriteLine($"OnCompleted {tag}")
        );

        public static IObservable<bool> Invert(this IObservable<bool> observable) => observable.Select(b => !b);

        public static IObservable<T> WhereAsync<T>(this IObservable<T> observable, Func<T, IObservable<bool>> asyncPredicate)
            => observable.SelectMany(item =>
                asyncPredicate(item)
                    .SingleAsync()
                    .SelectMany(include => include ? Observable.Return(item) : Observable.Empty<T>()));

        public static void DisposedBy(this IDisposable disposable, CompositeDisposable disposeBag)
        {
            disposeBag.Add(disposable);
        }

        public static IObservable<T> ConditionalRetryWithBackoffStrategy<T>(
            this IObservable<T> source,
            int maxRetries,
            Func<int, TimeSpan> backOffStrategy,
            Func<Exception, bool> shouldRetryOn,
            IScheduler scheduler)
        {
            return source.RetryWhen(errorSignal =>
            {
                return errorSignal.SelectMany((error, retryCount) =>
                {
                    var currentTry = retryCount + 1;
                    if (!shouldRetryOn(error) || currentTry > maxRetries)
                    {
                        throw error;
                    }

                    return Observable.Return(Unit.Default).Delay(backOffStrategy(currentTry), scheduler);
                });
            });
        }
    }
}
