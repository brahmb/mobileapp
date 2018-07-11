using Android.Support.Design.Widget;
using Android.Support.V7.Widget;
using Toggl.Giskard.Views;

namespace Toggl.Giskard.Activities
{
    public sealed partial class MainActivity
    {
        private void initializeViews()
        {
            coordinatorLayout = FindViewById<CoordinatorLayout>(Resource.Id.MainCoordinatorLayout);

            toolbar = FindViewById<Toolbar>(Resource.Id.Toolbar);

            mainRecyclerView = FindViewById<MainRecyclerView>(Resource.Id.MainRecyclerView);

            playButton = FindViewById<FloatingActionButton>(Resource.Id.MainPlayButton);

            stopButton = FindViewById<FloatingActionButton>(Resource.Id.MainStopButton);

            runningEntryCardFrame = FindViewById(Resource.Id.MainRunningTimeEntryFrame);
        }
    }
}
