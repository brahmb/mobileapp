using System;
using System.Reactive.Linq;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Toggl.Ultrawave.ApiClients;
using static Toggl.Foundation.Sync.PushSyncOperation;
using System.Collections;
using System.Collections.Generic;

namespace Toggl.Foundation.Sync.States.Push
{
    internal sealed class CreateEntityState<TModel, TDatabaseModel, TThreadsafeModel>
        : BasePushEntityState<TThreadsafeModel>
        where TModel : IIdentifiable
        where TDatabaseModel : TModel, IDatabaseModel
        where TThreadsafeModel : class, TDatabaseModel, IThreadSafeModel
    {
        private readonly ICreatingApiClient<TModel> api;

        private readonly IDataSource<TThreadsafeModel, TDatabaseModel> dataSource;

        private readonly Func<TModel, TThreadsafeModel> convertToThreadsafeModel;

        public StateResult<TThreadsafeModel> EntityChanged { get; } = new StateResult<TThreadsafeModel>();

        public StateResult<TThreadsafeModel> Finished { get; } = new StateResult<TThreadsafeModel>();

        public CreateEntityState(
            ICreatingApiClient<TModel> api,
            IDataSource<TThreadsafeModel, TDatabaseModel> dataSource,
            IAnalyticsService analyticsService,
            Func<TModel, TThreadsafeModel> convertToThreadsafeModel)
            : base(analyticsService)
        {
            Ensure.Argument.IsNotNull(api, nameof(api));
            Ensure.Argument.IsNotNull(convertToThreadsafeModel, nameof(convertToThreadsafeModel));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));

            this.api = api;
            this.convertToThreadsafeModel = convertToThreadsafeModel;
            this.dataSource = dataSource;
        }

        public override IObservable<ITransition> Start(TThreadsafeModel entity)
            => create(entity)
                .Select(convertToThreadsafeModel)
                .Track(AnalyticsService.EntitySynced, Create, entity.GetSafeTypeName())
                .Track(AnalyticsService.EntitySyncStatus, entity.GetSafeTypeName(), $"{Create}:{Resources.Success}")
                .SelectMany(tryOverwrite(entity))
                .Catch(Fail(entity, Create));

        private IObservable<TModel> create(TThreadsafeModel entity)
            => entity == null
                ? Observable.Throw<TModel>(new ArgumentNullException(nameof(entity)))
                : api.Create(entity);

        private Func<TThreadsafeModel, IObservable<ITransition>> tryOverwrite(TThreadsafeModel originalEntity)
            => serverEntity
                => dataSource.OverwriteIfOriginalDidNotChange(originalEntity, serverEntity)
                             .SelectMany(results => updateIdIfNeeded(results, originalEntity, serverEntity));

        private IObservable<ITransition> updateIdIfNeeded(
            IEnumerable<IConflictResolutionResult<TThreadsafeModel>> results,
            TThreadsafeModel originalEntity,
            TThreadsafeModel serverEntity)
        {
            foreach (var result in results)
            {
                switch (result)
                {
                    case UpdateResult<TThreadsafeModel> u when u.OriginalId == originalEntity.Id:
                        return Observable.Return(Finished.Transition(extractFrom(result)));
                    
                    case IgnoreResult<TThreadsafeModel> i when i.Id == originalEntity.Id || i.Id == serverEntity.Id:
                        return updateId(originalEntity.Id, serverEntity.Id);
                }
            }
            throw new ArgumentException("Results must contain result with one of the specified ids.");
        }

        private IObservable<ITransition> updateId(long originalId, long id)
            => dataSource.ChangeId(originalId, id).Select(EntityChanged.Transition);

        private TThreadsafeModel extractFrom(IConflictResolutionResult<TThreadsafeModel> result)
        {
            if (result is UpdateResult<TThreadsafeModel> updateResult)
                return updateResult.Entity;

            throw new ArgumentOutOfRangeException(nameof(result));
        }
    }
}
