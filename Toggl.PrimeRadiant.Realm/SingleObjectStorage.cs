using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Realms;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Exceptions;

namespace Toggl.PrimeRadiant.Realm
{
    internal sealed class SingleObjectStorage<TModel> : BaseStorage<TModel>, ISingleObjectStorage<TModel>
        where TModel : IDatabaseSyncable
    {
        private const int fakeId = 0;

        public SingleObjectStorage(IRealmAdapter<TModel> adapter)
            : base(adapter) { }

        public IObservable<TModel> GetById(long _)
            => Single();

        public override IObservable<TModel> Create(TModel entity)
        {
            Ensure.Argument.IsNotNull(entity, nameof(entity));

            return Observable.Defer(() =>
            {
                if (Adapter.GetAll().Any())
                    return Observable.Throw<TModel>(new EntityAlreadyExistsException());

                return Adapter.Create(entity)
                              .Apply(Observable.Return)
                              .Catch<TModel, Exception>(ex => Observable.Throw<TModel>(new DatabaseException(ex)));
            });
        }

        public override IObservable<IEnumerable<IConflictResolutionResult<TModel>>> BatchUpdate(
            IEnumerable<(long Id, TModel Entity)> entities,
            Func<TModel, TModel, ConflictResolutionMode> conflictResolution,
            IRivalsResolver<TModel> rivalsResolver = null)
            => CreateObservable(() =>
            {
                var list = entities.ToList();
                if (list.Count > 1)
                    throw new ArgumentException("Too many entities to update.");

                return Adapter.BatchUpdate(list, conflictResolution, rivalsResolver);
            });

        public IObservable<TModel> Single()
            => CreateObservable(() => Adapter.GetAll().Single());

        public static SingleObjectStorage<TModel> For<TRealmEntity>(
            Func<Realms.Realm> getRealmInstance, Func<TModel, Realms.Realm, TRealmEntity> convertToRealm)
            where TRealmEntity : RealmObject, TModel, IUpdatesFrom<TModel>
            => new SingleObjectStorage<TModel>(new RealmAdapter<TRealmEntity, TModel>(
                getRealmInstance,
                convertToRealm, _ => __ => true,
                obj => fakeId));

        public IObservable<TModel> Update(TModel entity)
            => Update(fakeId, entity);

        public IObservable<Unit> Delete()
            => Single().SelectMany(entity => Delete(fakeId));
    }
}
