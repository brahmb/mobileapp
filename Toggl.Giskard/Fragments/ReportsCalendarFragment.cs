using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using Android.OS;
using Android.Support.V4.View;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using MvvmCross;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Droid.Support.V4;
using MvvmCross.Droid.Support.V7.RecyclerView;
using MvvmCross.Platforms.Android.Binding.BindingContext;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using MvvmCross.WeakSubscription;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.Activities;
using Toggl.Giskard.Adapters;
using Toggl.Giskard.Extensions;
using Toggl.Multivac.Extensions;

namespace Toggl.Giskard.Fragments
{
    [MvxFragmentPresentation(typeof(ReportsViewModel), Resource.Id.ReportsCalendarContainer, AddToBackStack = false)]
    public sealed class ReportsCalendarFragment : MvxFragment<ReportsCalendarViewModel>
    {
        private int rowHeight;
        private ViewPager pager;
        private int currentRowCount;
        private CompositeDisposable disposableBag = new CompositeDisposable();

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);
            var view = this.BindingInflate(Resource.Layout.ReportsCalendarFragment, null);

            rowHeight = Activity.Resources.DisplayMetrics.WidthPixels / 7;

            Mvx.Resolve<IMvxBindingContextStack<IMvxAndroidBindingContext>>()
               .Push(BindingContext as IMvxAndroidBindingContext);

            pager = view.FindViewById<ViewPager>(Resource.Id.ReportsCalendarFragmentViewPager);
            pager.Adapter = new ReportsCalendarPagerAdapter(Activity, ViewModel);
            pager.SetCurrentItem(ViewModel.Months.Count - 1, false);

            view.FindViewById<MvxRecyclerView>(Resource.Id.ReportsCalendarFragmentShortcuts)
                .SetLayoutManager(new LinearLayoutManager(Activity, LinearLayoutManager.Horizontal, false));

            view.FindViewById<LinearLayout>(Resource.Id.ReportsCalendarFragmentHeader)
                .GetChildren<TextView>()
                .Indexed()
                .ForEach((textView, index)
                    => textView.Text = ViewModel.DayHeaderFor(index));

            ViewModel.WeakSubscribe<PropertyChangedEventArgs>(nameof(ViewModel.RowsInCurrentMonth), onRowCountChanged)
                .DisposedBy(disposableBag);

            ViewModel.SelectedDateRangeObservable.Subscribe(onDateRangeChanged)
                .DisposedBy(disposableBag);

            recalculatePagerHeight();

            return view;
        }

        private void onDateRangeChanged(DateRangeParameter dateRange)
        {
            var anyShortcutIsSelected = ViewModel.QuickSelectShortcuts.Any(shortcut => shortcut.Selected);
            if (!anyShortcutIsSelected) return;

            var dateRangeStartDate = dateRange.StartDate;
            var monthToScroll = ViewModel.Months.IndexOf(month => month.CalendarMonth.Month == dateRangeStartDate.Month);
            if (monthToScroll == pager.CurrentItem) return;

            var dateRangeStartDateIsContaintedInCurrentMonthView = ViewModel
                .Months[pager.CurrentItem]
                .Days.Any(day => day.DateTimeOffset == dateRangeStartDate);

            if (!dateRangeStartDateIsContaintedInCurrentMonthView || dateRangeStartDate.Month == dateRange.EndDate.Month)
            {
                pager.SetCurrentItem(monthToScroll, true);
            }
        }

        private void onRowCountChanged(object sender, PropertyChangedEventArgs e)
        {
            if (currentRowCount == ViewModel.RowsInCurrentMonth)
                return;

            recalculatePagerHeight();
        }

        private void recalculatePagerHeight()
        {
            currentRowCount = ViewModel.RowsInCurrentMonth;

            var layoutParams = pager.LayoutParameters;
            layoutParams.Height = rowHeight * currentRowCount;
            pager.LayoutParameters = layoutParams;

            var activity = (ReportsActivity)Activity;
            activity.RecalculateCalendarHeight();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing || disposableBag == null) return;

            disposableBag.Dispose();
        }
    }
}
