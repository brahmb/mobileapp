using System;
using Android.Content;
using Android.Runtime;
using Android.Support.V4.View;
using Android.Views;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.Views;
using Object = Java.Lang.Object;

namespace Toggl.Giskard.Adapters
{
    public sealed class ReportsCalendarPagerAdapter : PagerAdapter
    {
        private static readonly int itemWidth;

        private readonly Context context;
        private readonly ReportsCalendarViewModel viewModel;

        static ReportsCalendarPagerAdapter()
        {
        }

        public ReportsCalendarPagerAdapter(Context context, ReportsCalendarViewModel viewModel)
        {
            this.context = context;
            this.viewModel = viewModel;
        }

        public ReportsCalendarPagerAdapter(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }


        public override int Count => viewModel.Months.Count;

        public override Object InstantiateItem(ViewGroup container, int position)
        {
            var inflater = LayoutInflater.FromContext(context);
            var inflatedView = inflater.Inflate(Resource.Layout.ReportsCalendarFragmentPage, container, false);

            var calendarRecyclerView = (ReportsCalendarRecyclerView)inflatedView;
            calendarRecyclerView.ItemClick = viewModel.CalendarDayTappedCommand;
            calendarRecyclerView.ItemsSource = viewModel.Months[position].Days;
            calendarRecyclerView.GetAdapter().NotifyItemRangeChanged(0, viewModel.Months[position].Days.Count);
            container.AddView(inflatedView);

            return inflatedView;
        }

        public override void DestroyItem(ViewGroup container, int position, Object @object)
        {
            container.RemoveView(@object as View);
        }

        public override bool IsViewFromObject(View view, Object @object)
            => view == @object;
    }
}
