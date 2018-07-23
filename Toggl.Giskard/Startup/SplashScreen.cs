using Android.App;
using Android.Content.PM;
using Android.Graphics;
using Android.Support.V4.Content;
using Toggl.Giskard.Extensions;
using Android.OS;
using Toggl.Foundation.MvvmCross;
using Toggl.Foundation.MvvmCross.ViewModels;
using MvvmCross.Droid.Support.V7.AppCompat;
using MvvmCross;
using MvvmCross.Navigation;

namespace Toggl.Giskard
{
    [Activity(Label = "Toggl for Devs",
              NoHistory = true,
              MainLauncher = true,
              Icon = "@mipmap/ic_launcher",
              Theme = "@style/Theme.Splash",
              ScreenOrientation = ScreenOrientation.Portrait,
              ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    [IntentFilter(
        new[] { "android.intent.action.VIEW", "android.intent.action.EDIT" },
        Categories = new[] { "android.intent.category.BROWSABLE", "android.intent.category.DEFAULT" },
        DataSchemes = new[] { "toggl" },
        DataHost = "*")]
    public class SplashScreen : MvxSplashScreenAppCompatActivity<Setup, App<LoginViewModel>>
    {
        public SplashScreen()
            : base(Resource.Layout.SplashScreen)
        {

        }

        protected override void RunAppStart(Bundle bundle)
        {
            base.RunAppStart(bundle);
            var navigationUrl = Intent.Data?.ToString();
            if (string.IsNullOrEmpty(navigationUrl))
                return;

            Mvx.Resolve<IMvxNavigationService>().Navigate(navigationUrl);
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            var statusBarColor = new Color(ContextCompat.GetColor(this, Resource.Color.lightGray));
            this.ChangeStatusBarColor(statusBarColor);

#if USE_ANALYTICS
            Firebase.FirebaseApp.InitializeApp(this);
            Microsoft.AppCenter.AppCenter.Start(
                "{TOGGL_APP_CENTER_ID_DROID}",
                typeof(Microsoft.AppCenter.Crashes.Crashes),
                typeof(Microsoft.AppCenter.Analytics.Analytics));
#endif
        }
    }
}
