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
    }
}
