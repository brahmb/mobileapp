using System;
using System.Linq.Expressions;
using Realms;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant.Realm.Models;

namespace Toggl.PrimeRadiant.Realm
{
    internal sealed class Repository<TModel> : BaseStorage<TModel>, IRepository<TModel>
    {
        public Repository(IRealmAdapter<TModel> adapter)
            : base(adapter) { }

        public IObservable<TModel> GetById(long id)
            => CreateObservable(() => Adapter.Get(id));

        public IObservable<TModel> ChangeId(long currentId, long newId)
            => CreateObservable(() => Adapter.ChangeId(currentId, newId));

        public static Repository<TModel> For<TRealmEntity>(
            Func<Realms.Realm> getRealmInstance, Func<TModel, Realms.Realm, TRealmEntity> convertToRealm)
            where TRealmEntity : RealmObject, IIdentifiable,  IModifiableId, TModel, IUpdatesFrom<TModel>
            => For(getRealmInstance, convertToRealm, matchById<TRealmEntity>, getId<TRealmEntity>);

        public static Repository<TModel> For<TRealmEntity>(
            Func<Realms.Realm> getRealmInstance,
            Func<TModel, Realms.Realm, TRealmEntity> convertToRealm,
            Func<long, Expression<Func<TRealmEntity, bool>>> matchById,
            Func<TRealmEntity, long> getId)
            where TRealmEntity : RealmObject, TModel, IUpdatesFrom<TModel>
            => new Repository<TModel>(new RealmAdapter<TRealmEntity, TModel>(getRealmInstance, convertToRealm, matchById, getId));

        private static Expression<Func<TRealmEntity, bool>> matchById<TRealmEntity>(long id)
            where TRealmEntity : RealmObject, IIdentifiable, IModifiableId, TModel, IUpdatesFrom<TModel>
            => x => x.Id == id;

        private static long getId<TRealmEntity>(TRealmEntity entity)
            where TRealmEntity : RealmObject, IIdentifiable, TModel
            => entity.Id;
    }
}
