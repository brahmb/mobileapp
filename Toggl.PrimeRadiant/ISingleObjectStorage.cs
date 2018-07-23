using System;
using System.Reactive;

namespace Toggl.PrimeRadiant
{
    public interface ISingleObjectStorage<TModel> : IBaseStorage<TModel>
        where TModel : IDatabaseSyncable
    {
        IObservable<TModel> Single();
        IObservable<Unit> Delete();
        IObservable<TModel> Update(TModel entity);
    }
}
