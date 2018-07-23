using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;

namespace Toggl.Foundation.DataSources
{
    public abstract class SingletonDataSource<TThreadsafe, TDatabase>
        : BaseDataSource<TThreadsafe, TDatabase>,
          ISingletonDataSource<TThreadsafe>
        where TDatabase : IDatabaseSyncable
        where TThreadsafe : IThreadSafeModel, IIdentifiable, TDatabase
    {
        private readonly ISingleObjectStorage<TDatabase> storage;

        private readonly ISubject<TThreadsafe> currentSubject;

        public IObservable<TThreadsafe> Current { get; }

        protected SingletonDataSource(ISingleObjectStorage<TDatabase> storage, TThreadsafe defaultCurrentValue)
            : base(storage)
        {
            Ensure.Argument.IsNotNull(storage, nameof(storage));

            this.storage = storage;

            currentSubject = new Subject<TThreadsafe>();

            var initialValueObservable = storage.Single()
                .Select(Convert)
                .Catch((Exception _) => Observable.Return(defaultCurrentValue))
                .FirstAsync();

            var connectableCurrent = initialValueObservable.Concat(currentSubject).Replay(1);
            connectableCurrent.Connect();

            Current = connectableCurrent;
        }

        public virtual IObservable<TThreadsafe> Get()
            => storage.Single().Select(Convert);

        public override IObservable<TThreadsafe> Create(TThreadsafe entity)
            => base.Create(entity).Do(currentSubject.OnNext);

        public override IObservable<TThreadsafe> Update(TThreadsafe entity)
            => base.Update(entity).Do(currentSubject.OnNext);

        public override IObservable<IEnumerable<IConflictResolutionResult<TThreadsafe>>> OverwriteIfOriginalDidNotChange(
            TThreadsafe original, TThreadsafe entity)
            => base.OverwriteIfOriginalDidNotChange(original, entity)
                .Do(results => results.Do(handleConflictResolutionResult));

        public virtual IObservable<IConflictResolutionResult<TThreadsafe>> UpdateWithConflictResolution(
            TThreadsafe entity)
            => storage.UpdateWithConflictResolution(entity.Id, entity, ResolveConflicts, RivalsResolver)
                .ToThreadSafeResult(Convert)
                .SelectMany(CommonFunctions.Identity)
                .SingleAsync()
                .Do(handleConflictResolutionResult);

        private void handleConflictResolutionResult(IConflictResolutionResult<TThreadsafe> result)
        {
            switch (result)
            {
                case CreateResult<TThreadsafe> c:
                    currentSubject.OnNext(c.Entity);
                    break;

                case UpdateResult<TThreadsafe> u:
                    currentSubject.OnNext(u.Entity);
                    break;
            }
        }
    }
}
