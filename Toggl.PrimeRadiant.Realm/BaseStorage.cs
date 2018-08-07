using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Toggl.Multivac;
using Toggl.PrimeRadiant.Exceptions;

namespace Toggl.PrimeRadiant.Realm
{
    internal abstract class BaseStorage<TModel> : IBaseStorage<TModel>
    {
        protected IRealmAdapter<TModel> Adapter { get; }

        protected BaseStorage(IRealmAdapter<TModel> adapter)
        {
            Adapter = adapter;
        }

        public virtual IObservable<TModel> Create(TModel entity)
        {
            Ensure.Argument.IsNotNull(entity, nameof(entity));

            return Observable
                .Start(() => Adapter.Create(entity))
                .Catch<TModel, Exception>(ex => Observable.Throw<TModel>(new DatabaseException(ex)));
        }

        public virtual IObservable<IEnumerable<IConflictResolutionResult<TModel>>> BatchUpdate(
            IEnumerable<(long Id, TModel Entity)> entities,
            Func<TModel, TModel, ConflictResolutionMode> conflictResolution,
            IRivalsResolver<TModel> rivalsResolver = null)
        {
            Ensure.Argument.IsNotNull(entities, nameof(entities));
            Ensure.Argument.IsNotNull(conflictResolution, nameof(conflictResolution));

            return CreateObservable(() => Adapter.BatchUpdate(entities, conflictResolution, rivalsResolver));
        }

        public virtual IObservable<TModel> Update(long id, TModel entity)
        {
            Ensure.Argument.IsNotNull(entity, nameof(entity));

            return CreateObservable(() => Adapter.Update(id, entity));
        }

        public virtual IObservable<Unit> Delete(long id)
            => CreateObservable(() =>
            {
                Adapter.Delete(id);
                return Unit.Default;
            });

        public virtual IObservable<IEnumerable<TModel>> GetAll(Func<TModel, bool> predicate)
        {
            Ensure.Argument.IsNotNull(predicate, nameof(predicate));

            return CreateObservable(() => Adapter.GetAll().Where(predicate));
        }

        public virtual IObservable<IEnumerable<TModel>> GetAll()
            => CreateObservable(() => Adapter.GetAll());

        protected static IObservable<T> CreateObservable<T>(Func<T> getFunction)
        {
            return Observable.Create<T>(observer =>
            {
                try
                {
                    var data = getFunction();
                    observer.OnNext(data);
                    observer.OnCompleted();
                }
                catch (InvalidOperationException ex)
                {
                    observer.OnError(new DatabaseOperationException<TModel>(ex));
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }

                return Disposable.Empty;
            });
        }
    }
}
