using System;
using System.Reactive.Concurrency;
using Android.Content;
using MvvmCross;
using MvvmCross.Binding;
using MvvmCross.Droid.Support.V7.AppCompat;
using MvvmCross.Navigation;
using MvvmCross.Platforms.Android;
using MvvmCross.Platforms.Android.Presenters;
using MvvmCross.Platforms.Android.Views;
using MvvmCross.Plugin;
using MvvmCross.ViewModels;
using MvvmCross.Views;
using Toggl.Foundation;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Login;
using Toggl.Foundation.MvvmCross;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Services;
using Toggl.Foundation.Suggestions;
using Toggl.Giskard.Presenters;
using Toggl.Giskard.Services;
using Toggl.PrimeRadiant.Realm;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave;
using Toggl.Ultrawave.Network;

namespace Toggl.Giskard
{
    public sealed partial class Setup : MvxAppCompatSetup<App<LoginViewModel>>
    {
        private const int maxNumberOfSuggestions = 5;

        private IAnalyticsService analyticsService;
        private IMvxNavigationService navigationService;

#if USE_PRODUCTION_API
        private const ApiEnvironment environment = ApiEnvironment.Production;
#else
        private const ApiEnvironment environment = ApiEnvironment.Staging;
#endif

        protected override MvxBindingBuilder CreateBindingBuilder() => new TogglBindingBuilder();

        protected override IMvxNavigationService InitializeNavigationService(IMvxViewModelLocatorCollection collection)
        {
            analyticsService = new AnalyticsService();

            var loader = CreateViewModelLoader(collection);
            Mvx.RegisterSingleton<IMvxViewModelLoader>(loader);

            navigationService = new TrackingNavigationService(null, loader, analyticsService);

            Mvx.RegisterSingleton<IMvxNavigationService>(navigationService);
            return navigationService;
        }

        protected override IMvxAndroidViewPresenter CreateViewPresenter()
            => new TogglPresenter(AndroidViewAssemblies);

        protected override void InitializeApp(IMvxPluginManager pluginManager, IMvxApplication app)
        {
            const string clientName = "Giskard";
            var packageInfo = ApplicationContext.PackageManager.GetPackageInfo(ApplicationContext.PackageName, 0);
            var version = packageInfo.VersionName;
            var sharedPreferences = ApplicationContext.GetSharedPreferences(clientName, FileCreationMode.Private);
            var database = new Database();
            var scheduler = Scheduler.Default;
            var timeService = new TimeService(scheduler);
            var suggestionProviderContainer = new SuggestionProviderContainer(
                new MostUsedTimeEntrySuggestionProvider(database, timeService, maxNumberOfSuggestions)
            );

            var appVersion = Version.Parse(version);
            var userAgent = new UserAgent(clientName, version);
            var mailService = new MailService(ApplicationContext);
            var dialogService = new DialogService();
            var platformConstants = new PlatformConstants();
            var keyValueStorage = new SharedPreferencesStorage(sharedPreferences);
            var settingsStorage = new SettingsStorage(appVersion, keyValueStorage);
            var feedbackService = new FeedbackService(userAgent, mailService, dialogService, platformConstants);

            var foundation =
                TogglFoundation
                    .ForClient(userAgent, appVersion)
                    .WithDatabase(database)
                    .WithScheduler(scheduler)
                    .WithTimeService(timeService)
                    .WithMailService(mailService)
                    .WithApiEnvironment(environment)
                    .WithGoogleService<GoogleService>()
                    .WithRatingService<RatingService>()
                    .WithLicenseProvider<LicenseProvider>()
                    .WithAnalyticsService(analyticsService)
                    .WithPlatformConstants(platformConstants)
                    .WithRemoteConfigService<RemoteConfigService>()
                    .WithApiFactory(new ApiFactory(environment, userAgent))
                    .WithBackgroundService(new BackgroundService(timeService))
                    .WithSuggestionProviderContainer(suggestionProviderContainer)
                    .WithApplicationShortcutCreator(new ApplicationShortcutCreator(ApplicationContext))

                    .StartRegisteringPlatformServices()
                    .WithDialogService(dialogService)
                    .WithFeedbackService(feedbackService)
                    .WithLastTimeUsageStorage(settingsStorage)
                    .WithBrowserService<BrowserService>()
                    .WithKeyValueStorage(keyValueStorage)
                    .WithUserPreferences(settingsStorage)
                    .WithOnboardingStorage(settingsStorage)
                    .WithNavigationService(navigationService)
                    .WithAccessRestrictionStorage(settingsStorage)
                    .WithErrorHandlingService(new ErrorHandlingService(navigationService, settingsStorage))
                    .Build();

            foundation.RevokeNewUserIfNeeded().Initialize();

            base.InitializeApp(pluginManager, app);
        }

        protected override IMvxAndroidCurrentTopActivity CreateAndroidCurrentTopActivity()
        {
            var mvxApplication = MvxAndroidApplication.Instance;
            var activityLifecycleCallbacksManager = new QueryableMvxLifecycleMonitorCurrentTopActivity();
            mvxApplication.RegisterActivityLifecycleCallbacks(activityLifecycleCallbacksManager);
            return activityLifecycleCallbacksManager;
        }
    }
}
