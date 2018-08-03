using System;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Login;
using Toggl.Foundation.Shortcuts;
using Toggl.Foundation.Services;
using Toggl.Foundation.Tests.Generators;
using Toggl.PrimeRadiant;
using Xunit;
using Toggl.Foundation.Suggestions;
using Toggl.Foundation.Analytics;
using System.Reactive.Concurrency;
using Toggl.Ultrawave.Network;

namespace Toggl.Foundation.Tests
{
    public sealed class FoundationTests
    {
        public class TheCreateMethod
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool userAgent,
                bool useVersion,
                bool useDatabase,
                bool useScheduler,
                bool useApiFactory,
                bool useTimeService,
                bool useMailService,
                bool useRatingService,
                bool useGoogleService,
                bool useLicenseProvider,
                bool useAnalyticsService,
                bool useBackgroundService,
                bool usePlatformConstants,
                bool useRemoteConfigService,
                bool useApplicationShortcutCreator,
                bool useSuggestionProviderContainer)
            {
                var version = useVersion ? Version.Parse("1.0") : null;
                var agent = userAgent ? new UserAgent("Some Client", "1.0") : null;
                var scheduler = useScheduler ? Substitute.For<IScheduler>() : null;
                var database = useDatabase ? Substitute.For<ITogglDatabase>() : null;
                var apiFactory = useApiFactory ? Substitute.For<IApiFactory>() : null;
                var timeService = useTimeService ? Substitute.For<ITimeService>() : null;
                var mailService = useMailService ? Substitute.For<IMailService>() : null;
                var ratinService = useRatingService ? Substitute.For<IRatingService>() : null;
                var googleService = useGoogleService ? Substitute.For<IGoogleService>() : null;
                var licenseProvider = useLicenseProvider ? Substitute.For<ILicenseProvider>() : null;
                var analyticsService = useAnalyticsService ? Substitute.For<IAnalyticsService>() : null;
                var backgroundService = useBackgroundService ? Substitute.For<IBackgroundService>() : null;
                var platformConstants = usePlatformConstants ? Substitute.For<IPlatformConstants>() : null;
                var remoteConfigService = useRemoteConfigService ? Substitute.For<IRemoteConfigService>() : null;
                var applicationShortcutCreator = useApplicationShortcutCreator ? Substitute.For<IApplicationShortcutCreator>() : null;
                var suggestionProviderContainer = useSuggestionProviderContainer ? Substitute.For<ISuggestionProviderContainer>() : null;

                Action tryingToConstructWithEmptyParameters = () =>
                    TogglFoundation
                        .ForClient(agent, version)
                        .WithDatabase(database)
                        .WithScheduler(scheduler)
                        .WithApiFactory(apiFactory)
                        .WithTimeService(timeService)
                        .WithMailService(mailService)
                        .WithRatingService(ratinService)
                        .WithGoogleService(googleService)
                        .WithLicenseProvider(licenseProvider)
                        .WithAnalyticsService(analyticsService)
                        .WithBackgroundService(backgroundService)
                        .WithPlatformConstants(platformConstants)
                        .WithRemoteConfigService(remoteConfigService)
                        .WithApplicationShortcutCreator(applicationShortcutCreator)
                        .WithSuggestionProviderContainer(suggestionProviderContainer)
                        .Build();

                tryingToConstructWithEmptyParameters.Should().Throw<Exception>();
            }

            [Fact]
            public void BuildingWorksIfAllParametersAreProvided()
            {
                var version = Version.Parse("1.0");
                var scheduler = Substitute.For<IScheduler>();
                var apiFactory = Substitute.For<IApiFactory>();
                var agent = new UserAgent("Some Client", "1.0");
                var database = Substitute.For<ITogglDatabase>();
                var timeService = Substitute.For<ITimeService>();
                var mailService = Substitute.For<IMailService>();
                var ratingService = Substitute.For<IRatingService>();
                var googleService = Substitute.For<IGoogleService>();
                var licenseProvider = Substitute.For<ILicenseProvider>();
                var analyticsService = Substitute.For<IAnalyticsService>();
                var platformConstants = Substitute.For<IPlatformConstants>();
                var backgroundService = Substitute.For<IBackgroundService>();
                var remoteConfigService = Substitute.For<IRemoteConfigService>();
                var applicationShortcutCreator = Substitute.For<IApplicationShortcutCreator>();
                var suggestionProviderContainer = Substitute.For<ISuggestionProviderContainer>();

                Action tryingToConstructWithValidParameters = () =>
                    TogglFoundation
                        .ForClient(agent, version)
                        .WithDatabase(database)
                        .WithScheduler(scheduler)
                        .WithApiFactory(apiFactory)
                        .WithTimeService(timeService)
                        .WithMailService(mailService)
                        .WithRatingService(ratingService)
                        .WithGoogleService(googleService)
                        .WithLicenseProvider(licenseProvider)
                        .WithAnalyticsService(analyticsService)
                        .WithBackgroundService(backgroundService)
                        .WithPlatformConstants(platformConstants)
                        .WithRemoteConfigService(remoteConfigService)
                        .WithApplicationShortcutCreator(applicationShortcutCreator)
                        .WithSuggestionProviderContainer(suggestionProviderContainer)
                        .Build();

                tryingToConstructWithValidParameters.Should().NotThrow();
            }
        }
    }
}
