using System;
using Foundation;
using Toggl.Foundation.Calendar;
using UIKit;

namespace Toggl.Daneel.Views
{
    public sealed partial class CalendarItemView : ReactiveCollectionViewCell<CalendarItem>
    {
        public static readonly NSString Key = new NSString(nameof(CalendarItemView));
        public static readonly UINib Nib;

        static CalendarItemView()
        {
            Nib = UINib.FromName(nameof(CalendarItemView), NSBundle.MainBundle);
        }

        protected CalendarItemView(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }
    }
}
