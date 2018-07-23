using System;
using System.Collections.Generic;
using System.Reactive;

namespace Toggl.PrimeRadiant
{
    public interface IBaseStorage<TModel>
    {
        IObservable<TModel> Create(TModel entity);
        IObservable<IEnumerable<TModel>> GetAll();
        IObservable<IEnumerable<TModel>> GetAll(Func<TModel, bool> predicate);
        IObservable<TModel> Update(long id, TModel entity);
        IObservable<IEnumerable<IConflictResolutionResult<TModel>>> BatchUpdate(
            IEnumerable<(long Id, TModel Entity)> entities,
            Func<TModel, TModel, ConflictResolutionMode> conflictResolution,
            IRivalsResolver<TModel> rivalsResolver = null);
        IObservable<Unit> Delete(long id);
    }
}
