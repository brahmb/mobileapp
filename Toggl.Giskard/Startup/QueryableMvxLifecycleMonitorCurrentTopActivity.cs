using System;
using System.Collections.Concurrent;
using System.Linq;
using Android.App;
using Android.OS;
using MvvmCross.Droid.Support.V7.AppCompat;
using MvvmCross.Platforms.Android;
using MvvmCross.ViewModels;

namespace Toggl.Giskard
{
    public class QueryableMvxLifecycleMonitorCurrentTopActivity : Java.Lang.Object, Application.IActivityLifecycleCallbacks, IMvxAndroidCurrentTopActivity
     {
        private ConcurrentDictionary<string, ActivityInfo> activities = new ConcurrentDictionary<string, ActivityInfo>();
        public Activity Activity => GetCurrentActivity();

        public void OnActivityCreated(Activity activity, Bundle savedInstanceState)
        {
            UpdateActivityListItem(activity, true);
        }

        public void OnActivityDestroyed(Activity activity)
        {
            var activityName = getActivityName(activity);
            activities.TryRemove(activityName, out ActivityInfo removed);
        }

        public void OnActivityPaused(Activity activity)
        {
            UpdateActivityListItem(activity, false);
        }

        public void OnActivityResumed(Activity activity)
        {
            UpdateActivityListItem(activity, true);
        }

        public void OnActivitySaveInstanceState(Activity activity, Bundle outState)
        {
        }

        public void OnActivityStarted(Activity activity)
        {
            UpdateActivityListItem(activity, true);
        }

        public void OnActivityStopped(Activity activity)
        {
            UpdateActivityListItem(activity, false);
        }

        public Activity FindActivityByViewModel(IMvxViewModel viewModel)
        {
            var weakReferenceToActivity = activities.FirstOrDefault(activityInfo =>
            {
                activityInfo.Value.Activity.TryGetTarget(out var activity);
                return (activity as MvxAppCompatActivity)?.ViewModel == viewModel;
            });

            Activity activityToReturn = null;
            weakReferenceToActivity.Value?.Activity?.TryGetTarget(out activityToReturn);
            return activityToReturn;
        }

        private Activity GetCurrentActivity()
        {
            Activity activity = null;
            activities.FirstOrDefault(activityInfo => activityInfo.Value.IsCurrent)
                .Value
                ?.Activity
                ?.TryGetTarget(out activity);

            return activity;
        }

         private void UpdateActivityListItem(Activity activity, bool isCurrent)
         {
             var toAdd = new ActivityInfo { Activity = new WeakReference<Activity>(activity), IsCurrent = isCurrent};
             var activityName = getActivityName(activity);
             activities.AddOrUpdate(activityName, toAdd, (key, existing) =>
             {
                 existing.Activity.SetTarget(activity);
                 existing.IsCurrent = isCurrent;
                 return existing;
             });
         }

        private string getActivityName(Activity activity) => $"{activity.Class.SimpleName}_{activity.Handle.ToString()}";

        private class ActivityInfo
        {
            public bool IsCurrent { get; set; }
            public WeakReference<Activity> Activity { get; set; }
        }
    }
}
