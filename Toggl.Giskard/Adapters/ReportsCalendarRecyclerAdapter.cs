using System;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using MvvmCross;
using MvvmCross.Droid.Support.V7.RecyclerView;
using MvvmCross.Platforms.Android;

namespace Toggl.Giskard.Adapters
{
    public sealed class ReportsCalendarRecyclerAdapter : MvxRecyclerAdapter
    {
        private static readonly int itemWidth;

        static ReportsCalendarRecyclerAdapter()
        {
            var context = Mvx.Resolve<IMvxAndroidGlobals>().ApplicationContext;
            var service = context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
            var display = service.DefaultDisplay;
            var size = new Point();
            display.GetSize(size);

            itemWidth = size.X / 7;
        }

        public ReportsCalendarRecyclerAdapter()
        {
        }

        public ReportsCalendarRecyclerAdapter(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        public override int ItemCount
            => base.ItemCount;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            base.OnBindViewHolder(holder, position);

            var layoutParams = holder.ItemView.LayoutParameters;
            layoutParams.Width = itemWidth;
            holder.ItemView.LayoutParameters = layoutParams;
        }
    }
}
