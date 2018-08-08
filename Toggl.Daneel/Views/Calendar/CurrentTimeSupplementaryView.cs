using System;

using Foundation;
using UIKit;

namespace Toggl.Daneel.Views.Calendar
{
    public partial class CurrentTimeSupplementaryView : UICollectionViewCell
    {
        public static readonly NSString Key = new NSString("CurrentTimeSupplementaryView");
        public static readonly UINib Nib;

        static CurrentTimeSupplementaryView()
        {
            Nib = UINib.FromName("CurrentTimeSupplementaryView", NSBundle.MainBundle);
        }

        protected CurrentTimeSupplementaryView(IntPtr handle) : base(handle)
        {
        }
    }
}
