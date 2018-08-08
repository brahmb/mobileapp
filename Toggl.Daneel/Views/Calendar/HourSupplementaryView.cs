using System;

using Foundation;
using UIKit;

namespace Toggl.Daneel.Views.Calendar
{
    public partial class HourSupplementaryView : UICollectionReusableView
    {
        public static readonly NSString Key = new NSString("HourSupplementaryView");
        public static readonly UINib Nib;

        static HourSupplementaryView()
        {
            Nib = UINib.FromName("HourSupplementaryView", NSBundle.MainBundle);
        }

        protected HourSupplementaryView(IntPtr handle) : base(handle)
        {
        }
    }
}
