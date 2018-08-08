// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace Toggl.Daneel.Views.Calendar
{
	[Register ("HourSupplementaryView")]
	partial class HourSupplementaryView
	{
		[Outlet]
		UIKit.UILabel HourLabel { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (HourLabel != null) {
				HourLabel.Dispose ();
				HourLabel = null;
			}
		}
	}
}
