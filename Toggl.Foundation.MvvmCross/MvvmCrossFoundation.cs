using System;
using System.Reactive.Concurrency;
using MvvmCross.Navigation;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Login;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Services;
using Toggl.Foundation.Shortcuts;
using Toggl.Foundation.Suggestions;
using Toggl.Multivac;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave;
using Toggl.Ultrawave.Network;

namespace Toggl.Foundation.MvvmCross
{
    public struct MvvmCrossFoundation
    {
        public Version Version { get; }
        public UserAgent UserAgent { get; }
        public IApiFactory ApiFactory { get; }
        public ITogglDatabase Database { get; }
        public ITimeService TimeService { get; }
        public IScheduler Scheduler { get; }
        public IMailService MailService { get; }
        public IGoogleService GoogleService { get; }
        public IRatingService RatingService { get; }
        public ApiEnvironment ApiEnvironment { get; }
        public ILicenseProvider LicenseProvider { get; }
        public IAnalyticsService AnalyticsService { get; }
        public IBackgroundService BackgroundService { get; }
        public IPlatformConstants PlatformConstants { get; }
        public IRemoteConfigService RemoteConfigService { get; }
        public IApplicationShortcutCreator ShortcutCreator { get; }
        public ISuggestionProviderContainer SuggestionProviderContainer { get; }

        public IDialogService DialogService { get; }
        public IBrowserService BrowserService { get; }
        public IKeyValueStorage KeyValueStorage { get; }
        public IUserPreferences UserPreferences { get; }
        public IFeedbackService FeedbackService { get; }
        public IOnboardingStorage OnboardingStorage { get; }
        public IMvxNavigationService NavigationService { get; }
        public IPasswordManagerService PasswordManagerService { get; }
        public IErrorHandlingService ErrorHandlingService { get; }
        public IAccessRestrictionStorage AccessRestrictionStorage { get; }
        public ILastTimeUsageStorage LastTimeUsageStorage { get; }

        private MvvmCrossFoundation(Builder builder)
        {
            builder.EnsureValidity();

            DialogService = builder.DialogService;
            BrowserService = builder.BrowserService;
            KeyValueStorage = builder.KeyValueStorage;
            UserPreferences = builder.UserPreferences;
            FeedbackService = builder.FeedbackService;
            OnboardingStorage = builder.OnboardingStorage;
            NavigationService = builder.NavigationService;
            PasswordManagerService = builder.PasswordManagerService;
            ErrorHandlingService = builder.ErrorHandlingService;
            AccessRestrictionStorage = builder.AccessRestrictionStorage;
            LastTimeUsageStorage = builder.LastTimeUsageStorage;

            Version = builder.Foundation.Version;
            Database = builder.Foundation.Database;
            UserAgent = builder.Foundation.UserAgent;
            Scheduler = builder.Foundation.Scheduler;
            ApiFactory = builder.Foundation.ApiFactory;
            TimeService = builder.Foundation.TimeService;
            MailService = builder.Foundation.MailService;
            RatingService = builder.Foundation.RatingService;
            GoogleService = builder.Foundation.GoogleService;
            ApiEnvironment = builder.Foundation.ApiEnvironment;
            LicenseProvider = builder.Foundation.LicenseProvider;
            ShortcutCreator = builder.Foundation.ShortcutCreator;
            AnalyticsService = builder.Foundation.AnalyticsService;
            PlatformConstants = builder.Foundation.PlatformConstants;
            BackgroundService = builder.Foundation.BackgroundService;
            RemoteConfigService = builder.Foundation.RemoteConfigService;
            SuggestionProviderContainer = builder.Foundation.SuggestionProviderContainer;
        }

        public class Builder
        {
            internal TogglFoundation Foundation { get; }
            public IDialogService DialogService { get; private set; }
            public IBrowserService BrowserService { get; private set; }
            public IKeyValueStorage KeyValueStorage { get; private set; }
            public IUserPreferences UserPreferences { get; private set; }
            public IFeedbackService FeedbackService { get; private set; }
            public IOnboardingStorage OnboardingStorage { get; private set; }
            public IMvxNavigationService NavigationService { get; private set; }
            public IPasswordManagerService PasswordManagerService { get; private set; }
            public IErrorHandlingService ErrorHandlingService { get; private set; }
            public IAccessRestrictionStorage AccessRestrictionStorage { get; private set; }
            public ILastTimeUsageStorage LastTimeUsageStorage { get; private set; }

            public Builder(TogglFoundation foundation)
            {
                Ensure.Argument.IsNotNull(foundation, nameof(foundation));

                Foundation = foundation;
            }

            public Builder WithDialogService(IDialogService dialogService)
            {
                DialogService = dialogService;
                return this;
            }

            public Builder WithBrowserService(IBrowserService browserService)
            {
                BrowserService = browserService;
                return this;
            }

            public Builder WithKeyValueStorage(IKeyValueStorage keyValueStorage)
            {
                KeyValueStorage = keyValueStorage;
                return this;
            }

            public Builder WithAccessRestrictionStorage(IAccessRestrictionStorage accessRestrictionStorage)
            {
                AccessRestrictionStorage = accessRestrictionStorage;
                return this;
            }

            public Builder WithLastTimeUsageStorage(ILastTimeUsageStorage lastTimeUsageStorage)
            {
                LastTimeUsageStorage = lastTimeUsageStorage;
                return this;
            }

            public Builder WithUserPreferences(IUserPreferences userPreferences)
            {
                UserPreferences = userPreferences;
                return this;
            }

            public Builder WithOnboardingStorage(IOnboardingStorage onboardingStorage)
            {
                OnboardingStorage = onboardingStorage;
                return this;
            }

            public Builder WithNavigationService(IMvxNavigationService navigationService)
            {
                NavigationService = navigationService;
                return this;
            }

            public Builder WithPasswordManagerService(IPasswordManagerService passwordManagerService)
            {
                PasswordManagerService = passwordManagerService;
                return this;
            }

            public Builder WithErrorHandlingService(IErrorHandlingService errorHandlingService)
            {
                ErrorHandlingService = errorHandlingService;
                return this;
            }

            public Builder WithFeedbackService(IFeedbackService feedbackService)
            {
                FeedbackService = feedbackService;
                return this;
            }

            public Builder WithDialogService<TDialogService>()
                where TDialogService : IDialogService, new()
                => WithDialogService(new TDialogService());

            public Builder WithBrowserService<TBrowserService>()
                where TBrowserService : IBrowserService, new()
                => WithBrowserService(new TBrowserService());

            public Builder WithKeyValueStorage<TKeyValueStorage>()
                where TKeyValueStorage : IKeyValueStorage, new()
                => WithKeyValueStorage(new TKeyValueStorage());

            public Builder WithAccessRestrictionStorage<TAccessRestrictionStorage>()
                where TAccessRestrictionStorage : IAccessRestrictionStorage, new()
                => WithAccessRestrictionStorage(new TAccessRestrictionStorage());

            public Builder WithUserPreferences<TUserPreferences>()
                where TUserPreferences : IUserPreferences, new()
                => WithUserPreferences(new TUserPreferences());

            public Builder WithOnboardingStorage<TOnboardingStorage>()
                where TOnboardingStorage : IOnboardingStorage, new()
                => WithOnboardingStorage(new TOnboardingStorage());

            public Builder WithNavigationService<TNavigationService>()
                where TNavigationService : IMvxNavigationService, new()
                => WithNavigationService(new TNavigationService());

            public Builder WithPasswordManagerService<TPasswordManagerService>()
                where TPasswordManagerService : IPasswordManagerService, new()
                => WithPasswordManagerService(new TPasswordManagerService());

            public Builder WithErrorHandlingService<TErrorHandlingService>()
                where TErrorHandlingService : IErrorHandlingService, new()
                => WithErrorHandlingService(new TErrorHandlingService());

            public Builder WithFeedbackService<TFeedbackService>()
                where TFeedbackService : IFeedbackService, new()
                => WithFeedbackService(new TFeedbackService());

            public MvvmCrossFoundation Build()
                => new MvvmCrossFoundation(this);

            public void EnsureValidity()
            {
                Ensure.Argument.IsNotNull(DialogService, nameof(DialogService));
                Ensure.Argument.IsNotNull(BrowserService, nameof(BrowserService));
                Ensure.Argument.IsNotNull(KeyValueStorage, nameof(KeyValueStorage));
                Ensure.Argument.IsNotNull(UserPreferences, nameof(UserPreferences));
                Ensure.Argument.IsNotNull(FeedbackService, nameof(FeedbackService));
                Ensure.Argument.IsNotNull(OnboardingStorage, nameof(OnboardingStorage));
                Ensure.Argument.IsNotNull(NavigationService, nameof(NavigationService));
                Ensure.Argument.IsNotNull(ErrorHandlingService, nameof(ErrorHandlingService));
                Ensure.Argument.IsNotNull(AccessRestrictionStorage, nameof(AccessRestrictionStorage));
                Ensure.Argument.IsNotNull(LastTimeUsageStorage, nameof(LastTimeUsageStorage));
            }
        }
    }
}
