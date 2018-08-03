using System;
using Foundation;
using MvvmCross;
using MvvmCross.Navigation;
using MvvmCross.Platforms.Ios.Core;
using MvvmCross.Plugin.Color.Platforms.Ios;
using MvvmCross.ViewModels;
using Toggl.Daneel.Extensions;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.MvvmCross;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Services;
using Toggl.Foundation.Shortcuts;
using UIKit;

#if USE_ANALYTICS
using System.Linq;
#endif

namespace Toggl.Daneel
{
    [Register(nameof(AppDelegate))]
    public sealed class AppDelegate : MvxApplicationDelegate<Setup, App<OnboardingViewModel>>
    {
        private IAnalyticsService analyticsService;
        private IBackgroundService backgroundService;
        private IMvxNavigationService navigationService;

        public override UIWindow Window { get; set; }

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            #if USE_ANALYTICS
            Microsoft.AppCenter.AppCenter.Start(
                "{TOGGL_APP_CENTER_ID_IOS}",
                typeof(Microsoft.AppCenter.Crashes.Crashes),
                typeof(Microsoft.AppCenter.Analytics.Analytics));
            Firebase.Core.App.Configure();
            Google.SignIn.SignIn.SharedInstance.ClientID =
                Firebase.Core.App.DefaultInstance.Options.ClientId;
            #endif

            base.FinishedLaunching(application, launchOptions);

            #if ENABLE_TEST_CLOUD
            Xamarin.Calabash.Start();
            #endif
            #if USE_ANALYTICS
            Facebook.CoreKit.ApplicationDelegate.SharedInstance.FinishedLaunching(application, launchOptions);
            #endif

            return true;
        }

        protected override void RunAppStart(object hint = null)
        {
            base.RunAppStart(hint);

            analyticsService = Mvx.Resolve<IAnalyticsService>();
            backgroundService = Mvx.Resolve<IBackgroundService>();
            navigationService = Mvx.Resolve<IMvxNavigationService>();
            setupNavigationBar();
        }

        #if USE_ANALYTICS
        public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
        {
            var openUrlOptions = new UIApplicationOpenUrlOptions(options);
            var facebookOptions = new NSDictionary<NSString, NSObject>(
                options.Keys.Select(key => (NSString)key).ToArray(),
                options.Values);

            return Google.SignIn.SignIn.SharedInstance.HandleUrl(url, openUrlOptions.SourceApplication, openUrlOptions.Annotation)
                || Facebook.CoreKit.ApplicationDelegate.SharedInstance.OpenUrl(app, url, facebookOptions);
        }

        public override void OnActivated(UIApplication application)
        {
            Facebook.CoreKit.AppEvents.ActivateApp();
        }
        #endif

        public override void WillEnterForeground(UIApplication application)
        {
            base.WillEnterForeground(application);
            backgroundService.EnterForeground();
        }

        public override void DidEnterBackground(UIApplication application)
        {
            base.DidEnterBackground(application);
            backgroundService.EnterBackground();
        }

        public override void PerformActionForShortcutItem(UIApplication application, UIApplicationShortcutItem shortcutItem, UIOperationHandler completionHandler)
        {
            analyticsService.ApplicationShortcut.Track(shortcutItem.LocalizedTitle);

            var key = new NSString(nameof(ApplicationShortcut.Type));
            if (!shortcutItem.UserInfo.ContainsKey(key))
                return;

            var shortcutNumber = shortcutItem.UserInfo[key] as NSNumber;
            if (shortcutNumber == null)
                return;

            var shortcutType = (ShortcutType)(int)shortcutNumber;

            switch (shortcutType)
            {
                case ShortcutType.ContinueLastTimeEntry:
                    var interactorFactory = Mvx.Resolve<IInteractorFactory>();
                    if (interactorFactory == null) return;
                    IDisposable subscription = null;
                    subscription = interactorFactory
                        .ContinueMostRecentTimeEntry()
                        .Execute()
                        .Subscribe(_ =>
                        {
                            subscription.Dispose();
                            subscription = null;
                        });
                    break;

                case ShortcutType.Reports:
                    navigationService.Navigate<ReportsViewModel>();
                    break;

                case ShortcutType.StartTimeEntry:
                    navigationService.Navigate<StartTimeEntryViewModel>();
                    break;
            }
        }

        private void setupNavigationBar()
        {
            //Back button title
            var attributes = new UITextAttributes
            {
                Font = UIFont.SystemFontOfSize(14, UIFontWeight.Medium),
                TextColor = Color.NavigationBar.BackButton.ToNativeColor()
            };
            UIBarButtonItem.Appearance.SetTitleTextAttributes(attributes, UIControlState.Normal);
            UIBarButtonItem.Appearance.SetTitleTextAttributes(attributes, UIControlState.Highlighted);
            UIBarButtonItem.Appearance.SetBackButtonTitlePositionAdjustment(new UIOffset(6, 0), UIBarMetrics.Default);

            //Back button icon
            var image = UIImage.FromBundle("icBackNoPadding");
            UINavigationBar.Appearance.BackIndicatorImage = image;
            UINavigationBar.Appearance.BackIndicatorTransitionMaskImage = image;

            //Title and background
            var barBackgroundColor = Color.NavigationBar.BackgroundColor.ToNativeColor();
            UINavigationBar.Appearance.ShadowImage = new UIImage();
            UINavigationBar.Appearance.BarTintColor = barBackgroundColor;
            UINavigationBar.Appearance.BackgroundColor = barBackgroundColor;
            UINavigationBar.Appearance.TintColor = Color.NavigationBar.BackButton.ToNativeColor();
            UINavigationBar.Appearance.SetBackgroundImage(ImageExtension.ImageWithColor(barBackgroundColor), UIBarMetrics.Default);
            UINavigationBar.Appearance.TitleTextAttributes = new UIStringAttributes
            {
                Font = UIFont.SystemFontOfSize(14, UIFontWeight.Medium),
                ForegroundColor = UIColor.Black
            };
        }
    }
}
