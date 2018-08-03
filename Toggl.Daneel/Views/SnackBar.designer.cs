// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace Toggl.Daneel
{
	[Register ("SnackBar")]
	partial class SnackBar
	{
		[Outlet]
		UIKit.UIStackView buttonsStackView { get; set; }

		[Outlet]
		UIKit.UILabel label { get; set; }

		[Outlet]
		UIKit.UIStackView stackView { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (label != null) {
				label.Dispose ();
				label = null;
			}

			if (stackView != null) {
				stackView.Dispose ();
				stackView = null;
			}

			if (buttonsStackView != null) {
				buttonsStackView.Dispose ();
				buttonsStackView = null;
			}
		}
	}
}
