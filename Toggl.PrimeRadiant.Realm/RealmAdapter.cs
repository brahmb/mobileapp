using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using Realms;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Models;
using Toggl.PrimeRadiant.Realm.Models;

namespace Toggl.PrimeRadiant.Realm
{
    internal interface IRealmAdapter<TModel>
    {
        IQueryable<TModel> GetAll();

        TModel Get(long id);

        TModel ChangeId(long currentId, long newId);

        void Delete(long id);

        TModel Create(TModel entity);

        TModel Update(long id, TModel entity);

        IEnumerable<IConflictResolutionResult<TModel>> BatchUpdate(
            IEnumerable<(long Id, TModel Entity)> batch,
            Func<TModel, TModel, ConflictResolutionMode> conflictResolution,
            IRivalsResolver<TModel> rivalsResolver);
    }

    internal sealed class RealmAdapter<TRealmEntity, TModel> : IRealmAdapter<TModel>
        where TRealmEntity : RealmObject, TModel, IUpdatesFrom<TModel>
    {
        private readonly Func<TModel, Realms.Realm, TRealmEntity> clone;

        private readonly Func<long, Expression<Func<TRealmEntity, bool>>> matchEntity;

        private readonly Func<TRealmEntity, long> getId;

        private readonly Func<Realms.Realm> getRealmInstance;

        public RealmAdapter(
            Func<Realms.Realm> getRealmInstance,
            Func<TModel, Realms.Realm, TRealmEntity> clone,
            Func<long, Expression<Func<TRealmEntity, bool>>> matchEntity,
            Func<TRealmEntity, long> getId)
        {
            Ensure.Argument.IsNotNull(getRealmInstance, nameof(getRealmInstance));
            Ensure.Argument.IsNotNull(clone, nameof(clone));
            Ensure.Argument.IsNotNull(matchEntity, nameof(matchEntity));
            Ensure.Argument.IsNotNull(getId, nameof(getId));

            this.getRealmInstance = getRealmInstance;
            this.clone = clone;
            this.matchEntity = matchEntity;
            this.getId = getId;
        }

        public IQueryable<TModel> GetAll()
            => getRealmInstance().All<TRealmEntity>();

        public TModel Get(long id)
            => getRealmInstance().All<TRealmEntity>().Single(matchEntity(id));

        public TModel ChangeId(long currentId, long newId)
        {
            return doModyfingTransaction(currentId, (realm, realmEntity) =>
            {
                if (realmEntity is IModifiableId modifiableEntity)
                {
                    modifiableEntity.ChangeId(newId);
                    return;
                }

                throw new InvalidOperationException($"Cannot change ID of entity of type {typeof(TModel)} which does not implement {nameof(IModifiableId)}.");
            });
        }

        public TModel Create(TModel entity)
        {
            Ensure.Argument.IsNotNull(entity, nameof(entity));

            return doTransaction(realm => addRealmEntity(entity, realm));
        }

        public TModel Update(long id, TModel entity)
        {
            Ensure.Argument.IsNotNull(entity, nameof(entity));

            return doModyfingTransaction(id, (realm, realmEntity) => realmEntity.SetPropertiesFrom(entity, realm));
        }

        public IEnumerable<IConflictResolutionResult<TModel>> BatchUpdate(
            IEnumerable<(long Id, TModel Entity)> batch,
            Func<TModel, TModel, ConflictResolutionMode> conflictResolution,
            IRivalsResolver<TModel> rivalsResolver)
        {
            Ensure.Argument.IsNotNull(batch, nameof(batch));
            Ensure.Argument.IsNotNull(matchEntity, nameof(matchEntity));
            Ensure.Argument.IsNotNull(conflictResolution, nameof(conflictResolution));

            var realm = getRealmInstance();
            using (var transaction = realm.BeginWrite())
            {
                var realmEntities = realm.All<TRealmEntity>();
                var entitiesWithPotentialRival = new List<TRealmEntity>();
                var results = batch.Select(updated =>
                {
                    var oldEntity = realmEntities.SingleOrDefault(matchEntity(updated.Id));
                    var resolveMode = conflictResolution(oldEntity, updated.Entity);
                    var result = resolveEntity(realm, updated.Id, oldEntity, updated.Entity, resolveMode);

                    if (rivalsResolver != null &&
                        (result is CreateResult<TModel> || result is UpdateResult<TModel>))
                    {
                        var resolvedEntity = getEntityFromResult(result);
                        if (rivalsResolver.CanHaveRival(resolvedEntity))
                            entitiesWithPotentialRival.Add(resolvedEntity);
                    }

                    return result;
                }).ToList();

                resolvePotentialRivals(realm, entitiesWithPotentialRival, rivalsResolver, results);

                transaction.Commit();
                return results;
            }
        }

        public void Delete(long id)
        {
            doModyfingTransaction(id, (realm, realmEntity) => realm.Remove(realmEntity));
        }

        private TRealmEntity addRealmEntity(TModel entity, Realms.Realm realm)
        {
            var converted = entity as TRealmEntity ?? clone(entity, realm);
            if (converted is IModifiableId modifiable)
            {
                modifiable.OriginalId = modifiable.Id;
            }

            return realm.Add(converted);
        }

        private TModel doModyfingTransaction(long id, Action<Realms.Realm, TRealmEntity> transact)
            => doTransaction(realm =>
            {
                var realmEntity = realm.All<TRealmEntity>().Single(matchEntity(id));
                transact(realm, realmEntity);
                return realmEntity;
            });

        private TModel doTransaction(Func<Realms.Realm, TModel> transact)
        {
            var realm = getRealmInstance();
            using (var transaction = realm.BeginWrite())
            {
                var returnValue = transact(realm);
                transaction.Commit();
                return returnValue;
            }
        }

        private IConflictResolutionResult<TModel> resolveEntity(Realms.Realm realm, long oldId, TRealmEntity old, TModel entity, ConflictResolutionMode resolveMode)
        {
            switch (resolveMode)
            {
                case ConflictResolutionMode.Create:
                    var realmEntity = addRealmEntity(entity, realm);
                    return new CreateResult<TModel>(realmEntity);

                case ConflictResolutionMode.Delete:
                    realm.Remove(old);
                    return new DeleteResult<TModel>(oldId);

                case ConflictResolutionMode.Update:
                    old.SetPropertiesFrom(entity, realm);
                    return new UpdateResult<TModel>(oldId, old);

                case ConflictResolutionMode.Ignore:
                    return new IgnoreResult<TModel>(oldId);

                default:
                    throw new ArgumentException($"Unknown conflict resolution mode {resolveMode}");
            }
        }

        private TRealmEntity getEntityFromResult(IConflictResolutionResult<TModel> result)
        {
            switch (result)
            {
                case CreateResult<TModel> c:
                    return (TRealmEntity)c.Entity;
                case UpdateResult<TModel> u:
                    return (TRealmEntity)u.Entity;
                default:
                    throw new ArgumentOutOfRangeException(nameof(result));
            }
        }

        private void resolvePotentialRivals(
            Realms.Realm realm,
            IList<TRealmEntity> entitiesWithPotentialRivals,
            IRivalsResolver<TModel> resolver,
            List<IConflictResolutionResult<TModel>> results)
        {
            var entitiesWithRivals = entitiesWithPotentialRivals
                .Select(entity => (entity: entity, rival: findRival(realm, entity, resolver)))
                .Where(tuple => tuple.rival != null)
                .Aggregate(
                    new List<(TRealmEntity entity, TRealmEntity rival)>(),
                    (list, tuple) =>
                    {
                        if (list.None(selectedTuple =>
                                selectedTuple.entity.Equals(tuple.entity) && selectedTuple.rival.Equals(tuple.rival)
                                || selectedTuple.entity.Equals(tuple.rival) && selectedTuple.rival.Equals(tuple.entity)))
                        {
                            list.Add(tuple);
                        }

                        return list;
                    });

            entitiesWithRivals.ForEach(
                tuple => resolveRivals(realm, tuple.entity, tuple.rival, resolver, results));
        }

        private TRealmEntity findRival(
            Realms.Realm realm,
            TRealmEntity entity,
            IRivalsResolver<TModel> resolver)
            => (TRealmEntity)realm.All<TRealmEntity>()
                .SingleOrDefault(resolver.AreRivals(entity));

        private void resolveRivals(
            Realms.Realm realm,
            TRealmEntity entity,
            TRealmEntity rival,
            IRivalsResolver<TModel> resolver,
            List<IConflictResolutionResult<TModel>> results)
        {
            var originalRivalId = getId(rival);

            var (fixedEntity, fixedRival) = resolver.FixRivals(entity, rival, realm.All<TRealmEntity>());
            entity.SetPropertiesFrom(fixedEntity, realm);
            rival.SetPropertiesFrom(fixedRival, realm);

            results.Add(new UpdateResult<TModel>(originalRivalId, rival));
        }
    }
}
