using System;
using System.Reactive.Linq;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Sync.States.Push.Interfaces;
using Toggl.Multivac;
using Toggl.Ultrawave.Exceptions;

namespace Toggl.Foundation.Sync.States.Push
{
    public abstract class BasePushEntityState<T> : IPushEntityState<T>
        where T : class, IThreadSafeModel
    {
        protected readonly IAnalyticsService AnalyticsService;

        public StateResult<(Exception, T)> ServerError { get; } = new StateResult<(Exception, T)>();

        public StateResult<(Exception, T)> ClientError { get; } = new StateResult<(Exception, T)>();

        public StateResult<(Exception, T)> UnknownError { get; } = new StateResult<(Exception, T)>();

        protected BasePushEntityState(IAnalyticsService analyticsService)
        {
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));

            AnalyticsService = analyticsService;
        }

        protected Func<Exception, IObservable<ITransition>> Fail(T entity, PushSyncOperation operation)
            => exception
                => Observable
                    .Return(entity)
                    .Track(typeof(T).ToSyncErrorAnalyticsEvent(AnalyticsService), $"{operation}:{exception.Message}")
                    .Track(AnalyticsService.EntitySyncStatus, entity.GetSafeTypeName(), $"{operation}:{Resources.Failure}")
                    .SelectMany(_ => shouldRethrow(exception)
                        ? Observable.Throw<ITransition>(exception)
                        : Observable.Return(failTransition(entity, exception)));

        private bool shouldRethrow(Exception e)
            => e is ApiDeprecatedException || e is ClientDeprecatedException || e is UnauthorizedException || e is OfflineException;

        private ITransition failTransition(T entity, Exception e)
            => e is ServerErrorException
                ? ServerError.Transition((e, entity))
                : e is ClientErrorException
                    ? ClientError.Transition((e, entity))
                    : UnknownError.Transition((e, entity));

        public abstract IObservable<ITransition> Start(T entity);
    }
}
