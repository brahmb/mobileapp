using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Lang;

namespace Toggl.Giskard.Extensions
{
    //Default is Left | Bottom relative to the anchor when using ShowAsDropDown
    public static class PopupWindowWithDefaultGravityExtenstions
    {
        public static PopupOffsets LeftVerticallyCenteredOffsetsTo(this PopupWindow popupWindow,
            View anchor,
            int dpExtraLeftMargin = 0,
            int dpExtraTopMargin = 0,
            int dpExtraRightMargin = 0,
            int dpExtraBottomMargin = 0)
        {
            var contentWindow = popupWindow.ContentView;
            if (contentWindow == null)
            {
                throw new AndroidRuntimeException("The contentView must be set before calling this method");
            }

            var horizontalOffset = -(contentWindow.MeasuredWidth + dpExtraRightMargin.DpToPixels(contentWindow.Context));
            var verticalOffset = -(contentWindow.MeasuredHeight + Math.Abs(contentWindow.MeasuredHeight - anchor.Height) / 2);
            return new PopupOffsets(horizontalOffset, verticalOffset);
        }

        public static PopupOffsets TopRightFrom(this PopupWindow popupWindow,
            View anchor,
            int dpExtraRightMargin = 0,
            int dpExtraBottomMargin = 0)
        {
            var contentWindow = popupWindow.ContentView;
            if (contentWindow == null)
            {
                throw new AndroidRuntimeException("The contentView must be set before calling this method");
            }

            var horizontalOffset = -contentWindow.MeasuredWidth + anchor.Width + dpExtraRightMargin.DpToPixels(contentWindow.Context);
            var verticalOffset = -(contentWindow.MeasuredHeight + anchor.Height + dpExtraBottomMargin.DpToPixels(contentWindow.Context));
            return new PopupOffsets(horizontalOffset, verticalOffset);
        }

        public static PopupOffsets TopHorizontallyCenteredOffsetsTo(this PopupWindow popupWindow,
            View anchor,
            int dpExtraTopMargin = 0)
        {
            var contentWindow = popupWindow.ContentView;
            if (contentWindow == null)
            {
                throw new AndroidRuntimeException("The contentView must be set before calling this method");
            }

            var horizontalOffset = -(contentWindow.MeasuredWidth - anchor.Width) / 2;
            var verticalOffset = -(contentWindow.MeasuredHeight + anchor.Height + dpExtraTopMargin.DpToPixels(contentWindow.Context));
            return new PopupOffsets(horizontalOffset, verticalOffset);
        }

        public static PopupOffsets BottomRightOffsetsTo(this PopupWindow popupWindow,
            View anchor,
            int dpExtraRightMargin = 0,
            int dpExtraTopMargin = 0)
        {
            var contentWindow = popupWindow.ContentView;
            if (contentWindow == null)
            {
                throw new AndroidRuntimeException("The contentView must be set before calling this method");
            }

            var horizontalOffset = -contentWindow.MeasuredWidth + anchor.Width + dpExtraRightMargin.DpToPixels(contentWindow.Context);
            var verticalOffset = dpExtraTopMargin;
            return new PopupOffsets(horizontalOffset, verticalOffset);
        }
    }
}
