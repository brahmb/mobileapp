using Android.Content;
using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Support.V7.Widget.Helper;
using Android.Views;
using static Android.Support.V7.Widget.RecyclerView;

namespace Toggl.Giskard.Views
{
    public sealed class MainRecyclerViewTouchCallback : ItemTouchHelper.SimpleCallback
    {
        private readonly MainRecyclerView recyclerView;

        public MainRecyclerViewTouchCallback(Context context, MainRecyclerView recyclerView)
            : base(0, ItemTouchHelper.Left | ItemTouchHelper.Right)
        {
            this.recyclerView = recyclerView;
        }

        public override int GetSwipeDirs(RecyclerView recyclerView, ViewHolder viewHolder)
        {
            if (viewHolder is MainRecyclerViewLogViewHolder timeEntriesLogViewHolder && timeEntriesLogViewHolder.CanSync)
            {
                return base.GetSwipeDirs(recyclerView, viewHolder);
            }

            return ItemTouchHelper.ActionStateIdle;
        }

        public override bool OnMove(RecyclerView recyclerView, ViewHolder viewHolder, ViewHolder target)
            => false;

        public override void OnSwiped(ViewHolder viewHolder, int direction)
        {
            var swipedPosition = viewHolder.AdapterPosition;

            if (direction == ItemTouchHelper.Right)
            {
                recyclerView.MainRecyclerAdapter.ContinueTimeEntry(swipedPosition);
                recyclerView.MainRecyclerAdapter.NotifyItemChanged(swipedPosition);
            }
            else
            {
                recyclerView.MainRecyclerAdapter.DeleteTimeEntry(swipedPosition);
            }
        }

        public override void OnChildDraw(Canvas c, RecyclerView recyclerView,
                                         ViewHolder viewHolder,
                                         float dX, float dY,
                                         int actionState, bool isCurrentlyActive)
        {
            if (viewHolder is MainRecyclerViewLogViewHolder logViewHolder)
            {
                if (dX > 0)
                {
                    logViewHolder.ContinueBackground.Visibility = ViewStates.Visible;
                    logViewHolder.DeleteBackground.Visibility = ViewStates.Invisible;
                }
                else if (dX < 0)
                {
                    logViewHolder.ContinueBackground.Visibility = ViewStates.Invisible;
                    logViewHolder.DeleteBackground.Visibility = ViewStates.Visible;
                }
                else {
                    logViewHolder.ContinueBackground.Visibility = ViewStates.Invisible;
                    logViewHolder.DeleteBackground.Visibility = ViewStates.Invisible;
                }

                DefaultUIUtil.OnDraw(c, recyclerView, logViewHolder.ContentView, dX, dY, actionState, isCurrentlyActive);
            }
            else
            {
                base.OnChildDraw(c, recyclerView, viewHolder, dX, dY, actionState, isCurrentlyActive);
            }
        }

        public override void OnSelectedChanged(ViewHolder viewHolder, int actionState)
        {
            if (viewHolder is MainRecyclerViewLogViewHolder logViewHolder)
            {
                DefaultUIUtil.OnSelected(logViewHolder.ContentView);
            }
            else
            {
                base.OnSelectedChanged(viewHolder, actionState);
            }
        }

        public override void ClearView(RecyclerView recyclerView, ViewHolder viewHolder)
        {
            if (viewHolder is MainRecyclerViewLogViewHolder logViewHolder)
            {
                DefaultUIUtil.ClearView(logViewHolder.ContentView);
            }
            else
            {
                base.ClearView(recyclerView, viewHolder);
            }
        }
    }
}
