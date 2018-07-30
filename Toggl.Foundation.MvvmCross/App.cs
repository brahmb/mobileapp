using System.Reactive.Linq;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using Toggl.Foundation.Login;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac;
using Toggl.PrimeRadiant.Settings;

namespace Toggl.Foundation.MvvmCross
{
    public sealed class App<TFirstViewModelWhenNotLoggedIn> : MvxApplication
        where TFirstViewModelWhenNotLoggedIn : MvxViewModel
    {
        public override void Initialize()
        {
            RegisterCustomAppStart<AppStart<TFirstViewModelWhenNotLoggedIn>>();
        }
    }

    [Preserve(AllMembers = true)]
    public sealed class AppStart<TFirstViewModelWhenNotLoggedIn> : MvxAppStart
        where TFirstViewModelWhenNotLoggedIn : MvxViewModel
    {
        private readonly ITimeService timeService;
        private readonly ILoginManager loginManager;
        private readonly IOnboardingStorage onboardingStorage;
        private readonly IMvxNavigationService navigationService;
        private readonly IAccessRestrictionStorage accessRestrictionStorage;

        public AppStart(
            IMvxApplication app,
            ITimeService timeService,
            ILoginManager loginManager,
            IOnboardingStorage onboardingStorage,
            IMvxNavigationService navigationService,
            IAccessRestrictionStorage accessRestrictionStorage)
            : base (app, navigationService)
        {
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(loginManager, nameof(loginManager));
            Ensure.Argument.IsNotNull(onboardingStorage, nameof(onboardingStorage));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(accessRestrictionStorage, nameof(accessRestrictionStorage));

            this.timeService = timeService;
            this.loginManager = loginManager;
            this.onboardingStorage = onboardingStorage;
            this.navigationService = navigationService;
            this.accessRestrictionStorage = accessRestrictionStorage;
        }

        protected override async void NavigateToFirstViewModel(object hint = null)
        {
            onboardingStorage.SetFirstOpened(timeService.CurrentDateTime);

            if (accessRestrictionStorage.IsApiOutdated() || accessRestrictionStorage.IsClientOutdated())
            {
                await navigationService.Navigate<OnboardingViewModel>();
                await navigationService.Navigate<OutdatedAppViewModel>();
                return;
            }

            var dataSource = loginManager.GetDataSourceIfLoggedIn();
            if (dataSource == null)
            {
                await navigationService.Navigate<TFirstViewModelWhenNotLoggedIn>();
                return;
            }

            var user = await dataSource.User.Current.FirstAsync();
            if (accessRestrictionStorage.IsUnauthorized(user.ApiToken))
            {
                await navigationService.Navigate<TokenResetViewModel>();
                return;
            }

            var _ = dataSource.StartSyncing();

            await navigationService.Navigate<MainTabBarViewModel>();
        }
    }
}