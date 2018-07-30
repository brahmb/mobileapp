using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Services;
using Toggl.Foundation.Suggestions;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave.Network;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class MainTabBarViewModel : MvxViewModel
    {
        private MainViewModel mainViewModel;
        private ReportsViewModel reportsViewModel;
        private SettingsViewModel settingsViewModel;

        public IEnumerable<(MvxViewModel, String)> ViewModelTuples { get; private set; }

        public MainTabBarViewModel(
            ITogglDataSource dataSource,
            ITimeService timeService,
            IRatingService ratingService,
            IUserPreferences userPreferences,
            IFeedbackService feedbackService,
            IAnalyticsService analyticsService,
            IOnboardingStorage onboardingStorage,
            IInteractorFactory interactorFactory,
            IMvxNavigationService navigationService,
            IRemoteConfigService remoteConfigService,
            ISuggestionProviderContainer suggestionProviders,
            UserAgent userAgent,
            IMailService mailService,
            IDialogService dialogService,
            IPlatformConstants platformConstants)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(ratingService, nameof(ratingService));
            Ensure.Argument.IsNotNull(userPreferences, nameof(userPreferences));
            Ensure.Argument.IsNotNull(feedbackService, nameof(feedbackService));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(onboardingStorage, nameof(onboardingStorage));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(remoteConfigService, nameof(remoteConfigService));
            Ensure.Argument.IsNotNull(suggestionProviders, nameof(suggestionProviders));
            Ensure.Argument.IsNotNull(userAgent, nameof(userAgent));
            Ensure.Argument.IsNotNull(mailService, nameof(mailService));
            Ensure.Argument.IsNotNull(dialogService, nameof(dialogService));
            Ensure.Argument.IsNotNull(platformConstants, nameof(platformConstants));

            mainViewModel = new MainViewModel(
                dataSource,
                timeService,
                ratingService,
                userPreferences,
                feedbackService,
                analyticsService,
                onboardingStorage,
                interactorFactory, 
                navigationService, 
                remoteConfigService, 
                suggestionProviders);

            reportsViewModel = new ReportsViewModel(
                dataSource,
                timeService,
                navigationService,
                interactorFactory,
                analyticsService);

            settingsViewModel = new SettingsViewModel(
                userAgent,
                mailService,
                dataSource,
                dialogService,
                userPreferences,
                feedbackService,
                analyticsService,
                interactorFactory,
                platformConstants,
                onboardingStorage,
                navigationService);
        }

        public override async Task Initialize()
        {
            await base.Initialize();

            ViewModelTuples = new(MvxViewModel, String)[] { (mainViewModel, "icTime"), (reportsViewModel, "icReports"), (settingsViewModel, "icSettings") }.Do(async tuple => await tuple.Item1.Initialize());
        }
    }
}
