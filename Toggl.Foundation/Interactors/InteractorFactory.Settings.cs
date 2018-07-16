using System;
using System.Reactive;
using Toggl.Foundation.Interactors.Settings;

namespace Toggl.Foundation.Interactors
{
    public partial class InteractorFactory : IInteractorFactory
    {
        public IInteractor<IObservable<Unit>> SendFeedback(string message)
            => new SendFeedbackInteractor(
                dataSource.FeedbackApi,
                dataSource.User,
                dataSource.Workspaces,
                dataSource.TimeEntries,
                platformConstants,
                userPreferences,
                lastTimeUsageStorage,
                timeService,
                userAgent,
                message);
    }
}
