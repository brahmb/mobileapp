using System;
using Android.App;
using Android.Runtime;
using MvvmCross.Droid.Support.V7.AppCompat;
using Toggl.Foundation.MvvmCross;
using Toggl.Foundation.MvvmCross.ViewModels;

namespace Toggl.Giskard
{
    [Application]
    public class TogglApplication : MvxAppCompatApplication<Setup, App<LoginViewModel>>
    {
        public TogglApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

#if USE_ANALYTICS
        public override void OnCreate()
        {
            base.OnCreate();
            Firebase.FirebaseApp.InitializeApp(this);
            Microsoft.AppCenter.AppCenter.Start(
                "{TOGGL_APP_CENTER_ID_DROID}",
                typeof(Microsoft.AppCenter.Crashes.Crashes),
                typeof(Microsoft.AppCenter.Analytics.Analytics));
        }
#endif
    }
}
