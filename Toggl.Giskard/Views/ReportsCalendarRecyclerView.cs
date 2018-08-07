using System;
using Android.Content;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using MvvmCross.Droid.Support.V7.RecyclerView;
using Toggl.Giskard.Adapters;

namespace Toggl.Giskard.Views
{
    [Register("toggl.giskard.views.ReportsCalendarRecyclerView")]
    public sealed class ReportsCalendarRecyclerView : MvxRecyclerView
    {
        public ReportsCalendarRecyclerView(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        public ReportsCalendarRecyclerView(Context context)
            : this(context, null)
        {
        }

        public ReportsCalendarRecyclerView(Context context, IAttributeSet attrs)
            : this(context, attrs, 0)
        {
        }

        public ReportsCalendarRecyclerView(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle, new ReportsCalendarRecyclerAdapter())
        {
            SetLayoutManager(new ReportsCalendarLayoutManager(context));
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            var width = MeasureSpec.GetSize(widthMeasureSpec);
            var offset = width % 7;
            var newWidth = width - offset;
            var newWidthMeasureSpec = MeasureSpec.MakeMeasureSpec(newWidth, MeasureSpecMode.Exactly);
            base.OnMeasure(newWidthMeasureSpec, heightMeasureSpec);
        }

        private class ReportsCalendarLayoutManager : GridLayoutManager
        {
            public ReportsCalendarLayoutManager(Context context)
                : base(context, 7, LinearLayoutManager.Vertical, false)
            {
            }

            public override bool CanScrollVertically() => false;
        }
    }
}
