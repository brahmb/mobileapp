using NSubstitute;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Shortcuts;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave.Network;

namespace Toggl.Foundation.Tests
{
    public abstract class BaseInteractorTests
    {
        protected IIdProvider IdProvider { get; } = Substitute.For<IIdProvider>();
        protected ITimeService TimeService { get; } = Substitute.For<ITimeService>();
        protected ITogglDataSource DataSource { get; } = Substitute.For<ITogglDataSource>();
        protected IUserPreferences UserPreferences { get; } = Substitute.For<IUserPreferences>();
        protected IAnalyticsService AnalyticsService { get; } = Substitute.For<IAnalyticsService>();
        protected IPlatformConstants PlatformConstants { get; } = Substitute.For<IPlatformConstants>();
        protected ILastTimeUsageStorage LastTimeUsageStorage { get; } = Substitute.For<ILastTimeUsageStorage>();
        protected IApplicationShortcutCreator ApplicationShortcutCreator { get; }
            = Substitute.For<IApplicationShortcutCreator>();
        protected UserAgent UserAgent { get; } = new UserAgent("Tests", "0.0");

        protected IInteractorFactory InteractorFactory { get; }

        protected BaseInteractorTests()
        {
            InteractorFactory = new InteractorFactory(
                IdProvider,
                TimeService,
                DataSource,
                UserPreferences,
                AnalyticsService,
                ApplicationShortcutCreator,
                LastTimeUsageStorage,
                PlatformConstants,
                UserAgent
            );
        }
    }
}
