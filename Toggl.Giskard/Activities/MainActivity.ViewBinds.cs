using Android.Support.Design.Widget;
using Android.Support.V7.Widget;
using Android.Views;
using Toggl.Giskard.Views;

namespace Toggl.Giskard.Activities
{
    public sealed partial class MainActivity
    {
        private CoordinatorLayout coordinatorLayout;

        private Toolbar toolbar;

        private MainRecyclerView mainRecyclerView;

        private FloatingActionButton playButton;

        private FloatingActionButton stopButton;

        private View runningEntryCardFrame;

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
