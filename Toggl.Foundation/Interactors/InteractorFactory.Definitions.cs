using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Shortcuts;
using Toggl.Multivac;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave.Network;

namespace Toggl.Foundation.Interactors
{
    [Preserve(AllMembers = true)]
    public sealed partial class InteractorFactory : IInteractorFactory
    {
        private readonly IIdProvider idProvider;
        private readonly ITimeService timeService;
        private readonly ITogglDataSource dataSource;
        private readonly IUserPreferences userPreferences;
        private readonly IAnalyticsService analyticsService;
        private readonly IApplicationShortcutCreator shortcutCreator;
        private readonly ILastTimeUsageStorage lastTimeUsageStorage;
        private readonly IPlatformConstants platformConstants;
        private readonly UserAgent userAgent;

        public InteractorFactory(
            IIdProvider idProvider,
            ITimeService timeService,
            ITogglDataSource dataSource,
            IUserPreferences userPreferences,
            IAnalyticsService analyticsService,
            IApplicationShortcutCreator shortcutCreator,
            ILastTimeUsageStorage lastTimeUsageStorage,
            IPlatformConstants platformConstants,
            UserAgent userAgent)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(idProvider, nameof(idProvider));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(userPreferences, nameof(userPreferences));
            Ensure.Argument.IsNotNull(shortcutCreator, nameof(shortcutCreator));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(lastTimeUsageStorage, nameof(lastTimeUsageStorage));
            Ensure.Argument.IsNotNull(platformConstants, nameof(platformConstants));
            Ensure.Argument.IsNotNull(userAgent, nameof(userAgent));

            this.dataSource = dataSource;
            this.idProvider = idProvider;
            this.timeService = timeService;
            this.userPreferences = userPreferences;
            this.shortcutCreator = shortcutCreator;
            this.analyticsService = analyticsService;
            this.lastTimeUsageStorage = lastTimeUsageStorage;
            this.platformConstants = platformConstants;
            this.userAgent = userAgent;
        }
    }
}
