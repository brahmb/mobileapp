using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.DataSources
{
    public abstract class DataSource<TThreadsafe, TDatabase>
        : BaseDataSource<TThreadsafe, TDatabase>,
          IDataSource<TThreadsafe, TDatabase>
        where TDatabase : IDatabaseModel
        where TThreadsafe : TDatabase, IThreadSafeModel, IIdentifiable
    {
        private readonly IRepository<TDatabase> repository;

        protected DataSource(IRepository<TDatabase> repository)
            : base(repository)
        {
            Ensure.Argument.IsNotNull(repository, nameof(repository));

            this.repository = repository;
        }

        public IObservable<TThreadsafe> GetById(long id)
            => repository.GetById(id).Select(Convert);

        public virtual IObservable<TThreadsafe> ChangeId(long currentId, long newId)
            => repository.ChangeId(currentId, newId).Select(Convert);

        public virtual IObservable<IEnumerable<TThreadsafe>> GetAll()
            => repository.GetAll().Select(entities => entities.Select(Convert));

        public virtual IObservable<IEnumerable<TThreadsafe>> GetAll(Func<TDatabase, bool> predicate)
            => repository.GetAll(predicate).Select(entities => entities.Select(Convert));

        public virtual IObservable<IEnumerable<IConflictResolutionResult<TThreadsafe>>> DeleteAll(IEnumerable<TThreadsafe> entities)
            => repository.BatchUpdate(convertEntitiesForBatchUpdate(entities), safeAlwaysDelete)
                         .ToThreadSafeResult(Convert);

        public virtual IObservable<Unit> Delete(long id)
            => repository.Delete(id);

        public virtual IObservable<IEnumerable<IConflictResolutionResult<TThreadsafe>>> BatchUpdate(IEnumerable<TThreadsafe> entities)
            => repository.BatchUpdate(
                    convertEntitiesForBatchUpdate(entities),
                    ResolveConflicts,
                    RivalsResolver)
                .ToThreadSafeResult(Convert);

        private IEnumerable<(long, TDatabase)> convertEntitiesForBatchUpdate(
            IEnumerable<TThreadsafe> entities)
            => entities.Select(entity => (entity.Id, (TDatabase)entity));

        private static ConflictResolutionMode safeAlwaysDelete(TDatabase old, TDatabase now)
            => old == null ? ConflictResolutionMode.Ignore : ConflictResolutionMode.Delete;
    }
}
