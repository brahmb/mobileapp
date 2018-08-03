using System;
using Android.Content;
using Android.Runtime;
using Android.Support.V4.View;
using Android.Util;
using Android.Views;

namespace Toggl.Giskard.Views
{
    [Register("toggl.giskard.views.NonSwipingViewPager")]
    public class NonSwipingViewPager : ViewPager
    {
        public NonSwipingViewPager(Context context) 
            : base(context) { }
        
        public NonSwipingViewPager(Context context, IAttributeSet attrs) 
            : base(context, attrs) { }
        
        protected NonSwipingViewPager(IntPtr javaReference, JniHandleOwnership transfer) 
            : base(javaReference, transfer) { }

        public override bool OnInterceptTouchEvent(MotionEvent ev) => false;

        public override bool OnTouchEvent(MotionEvent e) => false;
    }
}
