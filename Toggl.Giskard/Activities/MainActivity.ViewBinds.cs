using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Android.App;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Views;
using Android.Widget;
using MvvmCross.Droid.Support.V7.AppCompat;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using MvvmCross.WeakSubscription;
using Toggl.Foundation.MvvmCross.Onboarding.MainView;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.Helper;
using Toggl.Multivac.Extensions;
using static Toggl.Foundation.Sync.SyncProgress;
using static Toggl.Giskard.Extensions.CircularRevealAnimation.AnimationType;
using FoundationResources = Toggl.Foundation.Resources;
using Toolbar = Android.Support.V7.Widget.Toolbar;
using Toggl.Giskard.Views;
using System.Reactive.Linq;
using System.Threading;

namespace Toggl.Giskard.Activities
{
    public sealed partial class MainActivity : MvxAppCompatActivity<MainViewModel>
    {
        private FrameLayout projectDotView;

        private void initializeViews()
        {
            projectDotView = FindViewById<FrameLayout>(Resource.Id.MainRunningTimeEntryProjectDot);
        }
    }
}
