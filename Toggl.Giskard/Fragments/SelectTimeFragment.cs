using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.View;
using Android.Views;
using Android.Widget;
using MvvmCross.Droid.Support.V4;
using MvvmCross.Platforms.Android.Binding.BindingContext;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using MvvmCross.WeakSubscription;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.Adapters;
using Toggl.Giskard.Extensions;
using Toggl.Multivac.Extensions;

namespace Toggl.Giskard.Fragments
{
    using System.Collections.Generic;
    using System.Reactive.Linq;
    using System.Threading;
    using Toggl.Giskard.Views;
    using static SelectTimeFragment.EditorMode;
    using static SelectTimeViewModel;
    using static SelectTimeViewModel.TemporalInconsistency;

    [MvxDialogFragmentPresentation(AddToBackStack = true)]
    public sealed class SelectTimeFragment : MvxDialogFragment<SelectTimeViewModel>, TabLayout.IOnTabSelectedListener
    {
        internal enum EditorMode
        {
            Date,
            Time,
            Duration,
            RunningTimeEntry,
            EditingDuration
        }

        private readonly int[] heights = { 450, 400, 224, 204, 164 };
        private const int vibrationDuration = 250;

        private EditorMode editorMode = Date;

        private readonly CompositeDisposable disposableBag = new CompositeDisposable();
        private IDisposable on24HoursModeChangeDisposable;

        private FrameLayout startTimePickerContainer;
        private FrameLayout stopTimePickerContainer;
        private TogglDroidTimePicker startTimePicker;
        private TogglDroidTimePicker stopTimePicker;

        private LinearLayout controlButtons;
        private TabLayout tabLayout;
        private ViewPager pager;

        private Toast toast;
        private Vibrator vibrator;

        public SelectTimeFragment()
        {
        }

        public SelectTimeFragment(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            vibrator = (Vibrator)Context.GetSystemService(Context.VibratorService);

            base.OnCreateView(inflater, container, savedInstanceState);
            var view = this.BindingInflate(Resource.Layout.SelectTimeFragment, null);

            controlButtons = view.FindViewById<LinearLayout>(Resource.Id.SelectTimeFragmentControlButtons);
            pager = view.FindViewById<ViewPager>(Resource.Id.SelectTimeFragmentPager);
            tabLayout = view.FindViewById<TabLayout>(Resource.Id.SelectTimeTabView);

            pager.OffscreenPageLimit = 2;
            pager.Adapter = new SelectTimePagerAdapter(BindingContext);
            tabLayout.AddOnTabSelectedListener(this);

            ViewModel.IsCalendarViewObservable
                     .ObserveOn(SynchronizationContext.Current)
                     .Subscribe(onIsCalendarViewChanged)
                     .DisposedBy(disposableBag);

            subscribeAndAddToDisposableBag(nameof(ViewModel.StopTime), onStopTimeChanged);
            subscribeAndAddToDisposableBag(nameof(ViewModel.StartTime), onStartTimeChanged);
            subscribeAndAddToDisposableBag(nameof(ViewModel.IsEditingDuration), onDurationEditingStateChanged);

            ViewModel.TemporalInconsistencyDetected
                     .Subscribe(onTemporalInconsistency)
                     .DisposedBy(disposableBag);

            var startPageView = this.BindingInflate(Resource.Layout.SelectDateTimeStartTimeTabHeader, null);
            var stopPageView = this.BindingInflate(Resource.Layout.SelectDateTimeStopTimeTabHeader, null);
            var durationPageView = this.BindingInflate(Resource.Layout.SelectDateTimeDurationTabHeader, null);

            tabLayout.Post(() =>
            {
                tabLayout.SetupWithViewPager(pager, true);

                tabLayout.GetTabAt(StartTimeTab).SetCustomView(startPageView);
                tabLayout.GetTabAt(StopTimeTab).SetCustomView(stopPageView);
                tabLayout.GetTabAt(DurationTab).SetCustomView(durationPageView);

                pager.SetCurrentItem(ViewModel.StartingTabIndex, false);

                startTimePickerContainer = view.FindViewById<FrameLayout>(Resource.Id.SelectStartTimeClockViewContainer);
                stopTimePickerContainer = view.FindViewById<FrameLayout>(Resource.Id.SelectStopTimeClockViewContainer);

                on24HoursModeChangeDisposable = ViewModel.Is24HoursModeObservable
                    .ObserveOn(SynchronizationContext.Current)
                    .Subscribe(on24HourModeSet);
            });

            setupDialogWindowPosition();

            return view;
        }

        private void subscribeAndAddToDisposableBag(string property, EventHandler<PropertyChangedEventArgs> eventHandler)
        {
            ViewModel.WeakSubscribe<PropertyChangedEventArgs>(property, eventHandler)
                .DisposedBy(disposableBag);
        }

        private void onStopTimeChanged(object sender, PropertyChangedEventArgs args)
        {
            if (stopTimePicker != null && ViewModel.StopTime != null)
            {
                stopTimePicker.Value = ViewModel.StopTime.Value;
            }
        }

        private void onStartTimeChanged(object sender, PropertyChangedEventArgs args)
        {
            if (startTimePicker != null)
            {
                startTimePicker.Value = ViewModel.StartTime;
            }
        }

        private void onDurationEditingStateChanged(object sender, PropertyChangedEventArgs args)
        {
            editorMode = calculateEditorMode();
            updateLayoutHeight();
        }

        private void setupDialogWindowPosition()
        {
            var window = Dialog.Window;
            var layoutParams = window.Attributes;
            layoutParams.Gravity = GravityFlags.Top;
            window.Attributes = layoutParams;
        }

        private void on24HourModeSet(bool is24HoursMode)
        {
            on24HoursModeChangeDisposable.Dispose();
            updateTimePickerWidgets(is24HoursMode);
            startTimePicker.Value = ViewModel.StartTime;
            if (ViewModel.StopTime != null)
            {
                stopTimePicker.Value = ViewModel.StopTime.Value;
            }
        }

        private void updateTimePickerWidgets(bool is24HoursMode)
        {
            if (startTimePicker != null)
            {
                startTimePicker.ValueChanged -= updateStartTime;
                startTimePicker.Dispose();
            }
            startTimePicker = new TogglDroidTimePicker(Context, is24HoursMode);
            startTimePickerContainer.RemoveAllViews();
            startTimePickerContainer.AddView(startTimePicker);
            startTimePicker.ValueChanged += updateStartTime;

            if (stopTimePicker != null)
            {
                stopTimePicker.ValueChanged -= updateStopTime;
                stopTimePicker.Dispose();
            }
            stopTimePicker = new TogglDroidTimePicker(Context, is24HoursMode);
            stopTimePickerContainer.RemoveAllViews();
            stopTimePickerContainer.AddView(stopTimePicker);
            stopTimePicker.ValueChanged += updateStopTime;
        }

        private void updateStartTime(object sender, EventArgs args)
        {
            ViewModel.StartTime = startTimePicker.Value;
        }

        private void updateStopTime(object sender, EventArgs args)
        {
            ViewModel.StopTime = stopTimePicker.Value;
        }

        private Dictionary<TemporalInconsistency, int> inconsistencyMessages = new Dictionary<TemporalInconsistency, int>
        {
            [StartTimeAfterCurrentTime] = Resource.String.StartTimeAfterCurrentTimeWarning,
            [StartTimeAfterStopTime] = Resource.String.StartTimeAfterStopTimeWarning,
            [StopTimeBeforeStartTime] = Resource.String.StopTimeBeforeStartTimeWarning,
            [DurationTooLong] = Resource.String.DurationTooLong,
        };

        private void onTemporalInconsistency(TemporalInconsistency temporalInconsistency)
        {
            if (toast != null)
                toast.Cancel();

            var messageResourceId = inconsistencyMessages[temporalInconsistency];
            var message = Resources.GetString(messageResourceId);

            toast = Toast.MakeText(Context, message, ToastLength.Short);
            toast.Show();
        }

        private void onIsCalendarViewChanged(bool isCalendarView)
        {
            editorMode = isCalendarView ? Date : Time;
            updateLayoutHeight();
        }

        public void OnTabReselected(TabLayout.Tab tab)
            => onTabChange(tab.Position);

        public void OnTabSelected(TabLayout.Tab tab)
            => onTabChange(tab.Position);

        private void onTabChange(int tabPosition)
        {
            ViewModel.CurrentTab = tabPosition;
            editorMode = calculateEditorMode();

            updateLayoutHeight();
        }

        private EditorMode calculateEditorMode()
        {
            if (ViewModel.CurrentTab == DurationTab)
            {
                return ViewModel.IsEditingDuration ? EditingDuration : Duration;
            }

            if (ViewModel.CurrentTab == StopTimeTab && !ViewModel.IsTimeEntryStopped)
                return RunningTimeEntry;

            return ViewModel.IsCalendarView ? Date : Time;
        }

        public override void OnCancel(IDialogInterface dialog)
        {
            ViewModel.CancelCommand.Execute();
        }

        public override void OnDismiss(IDialogInterface dialog)
        {
            if (ViewModel == null) return;

            ViewModel.SaveCommand.Execute();
        }

        private void updateLayoutHeight()
        {
            var newHeight = heights[(int)editorMode];

            tabLayout.RequestLayout();
            controlButtons.ForceLayout();

            Activity.RunOnUiThread(() =>
            {
                var heightInPixels = newHeight.DpToPixels(Context);

                var pagerLayout = pager.LayoutParameters;
                pagerLayout.Height = heightInPixels;
                pager.LayoutParameters = pagerLayout;

                Dialog.Window.SetDefaultDialogLayout(Activity, Context, heightDp: ViewGroup.LayoutParams.WrapContent);
            });
        }

        public void OnTabUnselected(TabLayout.Tab tab)
        {
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing == false) return;

            disposableBag.Dispose();
        }
    }
}
