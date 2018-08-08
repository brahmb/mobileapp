using System;
using MvvmCross;
using Toggl.Foundation.Autocomplete;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Login;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Sync;
using Toggl.Ultrawave;

namespace Toggl.Foundation.MvvmCross
{

    public static class FoundationExtensions
    {
        private const int newUserThreshold = 60;
        private static readonly TimeSpan retryDelayLimit = TimeSpan.FromSeconds(60);

        public static MvvmCrossFoundation.Builder StartRegisteringPlatformServices(this TogglFoundation.Builder builder)
            => builder.Build().StartRegisteringPlatformServices();

        public static MvvmCrossFoundation.Builder StartRegisteringPlatformServices(this TogglFoundation foundation)
            => new MvvmCrossFoundation.Builder(foundation);

        public static MvvmCrossFoundation RevokeNewUserIfNeeded(this MvvmCrossFoundation foundation)
        {
            var now = foundation.TimeService.CurrentDateTime;
            var lastUsed = foundation.OnboardingStorage.GetLastOpened();
            foundation.OnboardingStorage.SetLastOpened(now);
            if (lastUsed == null) return foundation;

            var lastUsedDate = DateTimeOffset.Parse(lastUsed);
            var offset = now - lastUsedDate;
            if (offset < TimeSpan.FromDays(newUserThreshold)) return foundation;

            foundation.OnboardingStorage.SetIsNewUser(false);
            return foundation;
        }

        public static void Initialize(this MvvmCrossFoundation foundation)
        {
            initializeInversionOfControl(foundation);

            Func<ITogglDataSource, ISyncManager> createSyncManager(ITogglApi api) => dataSource =>
                TogglSyncManager.CreateSyncManager(foundation.Database, api, dataSource, foundation.TimeService, foundation.AnalyticsService, foundation.LastTimeUsageStorage, retryDelayLimit, foundation.Scheduler);

            ITogglDataSource createDataSource(ITogglApi api)
            {
                var dataSource = new TogglDataSource(
                        api,
                        foundation.Database,
                        foundation.TimeService,
                        foundation.ErrorHandlingService,
                        foundation.BackgroundService,
                        createSyncManager(api),
                        TimeSpan.FromMinutes(5),
                        foundation.ShortcutCreator,
                        foundation.AnalyticsService)
                    .RegisterServices();

                Mvx.ConstructAndRegisterSingleton<IInteractorFactory, InteractorFactory>();
                Mvx.ConstructAndRegisterSingleton<IAutocompleteProvider, AutocompleteProvider>();

                return dataSource;
            }

            var loginManager =
                new LoginManager(foundation.ApiFactory, foundation.Database, foundation.GoogleService, foundation.ShortcutCreator, foundation.AccessRestrictionStorage, foundation.AnalyticsService, createDataSource, foundation.Scheduler);

            Mvx.RegisterSingleton<ILoginManager>(loginManager);
        }

        private static void initializeInversionOfControl(MvvmCrossFoundation foundation)
        {
            Mvx.RegisterSingleton(foundation.BackgroundService);
            Mvx.RegisterSingleton(foundation.DialogService);
            Mvx.RegisterSingleton(foundation.Database);
            Mvx.RegisterSingleton(foundation.BrowserService);
            Mvx.RegisterSingleton(foundation.UserAgent);
            Mvx.RegisterSingleton(foundation.Scheduler);
            Mvx.RegisterSingleton(foundation.ApiFactory);
            Mvx.RegisterSingleton(foundation.TimeService);
            Mvx.RegisterSingleton(foundation.MailService);
            Mvx.RegisterSingleton(foundation.RatingService);
            Mvx.RegisterSingleton(foundation.ShortcutCreator);
            Mvx.RegisterSingleton(foundation.LicenseProvider);
            Mvx.RegisterSingleton(foundation.FeedbackService);
            Mvx.RegisterSingleton(foundation.ShortcutCreator);
            Mvx.RegisterSingleton(foundation.AnalyticsService);
            Mvx.RegisterSingleton(foundation.PlatformConstants);
            Mvx.RegisterSingleton(foundation.Database.IdProvider);
            Mvx.RegisterSingleton(foundation.RemoteConfigService);
            Mvx.RegisterSingleton(foundation.SuggestionProviderContainer);
            Mvx.RegisterSingleton(foundation.UserPreferences);
            Mvx.RegisterSingleton(foundation.OnboardingStorage);
            Mvx.RegisterSingleton(foundation.AccessRestrictionStorage);
            Mvx.RegisterSingleton(foundation.LastTimeUsageStorage);
            Mvx.RegisterSingleton(foundation.ErrorHandlingService);
            Mvx.RegisterSingleton(foundation.PasswordManagerService ?? new StubPasswordManagerService());
        }
    }
}
