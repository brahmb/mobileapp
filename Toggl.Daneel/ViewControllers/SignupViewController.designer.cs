// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace Toggl.Daneel.ViewControllers
{
	[Register ("SignupViewController")]
	partial class SignupViewController
	{
		[Outlet]
		Toggl.Daneel.Views.ActivityIndicatorView ActivityIndicator { get; set; }

		[Outlet]
		UIKit.UIImageView CountryDropDownCaretImageView { get; set; }

		[Outlet]
		UIKit.UIImageView CountryNotSelectedImageView { get; set; }

		[Outlet]
		UIKit.NSLayoutConstraint EmailFieldTopConstraint { get; set; }

		[Outlet]
		Toggl.Daneel.Views.LoginTextField EmailTextField { get; set; }

		[Outlet]
		UIKit.UILabel ErrorLabel { get; set; }

		[Outlet]
		UIKit.UIButton GoogleSignupButton { get; set; }

		[Outlet]
		UIKit.UIView LoginCard { get; set; }

		[Outlet]
		Toggl.Daneel.Views.LoginTextField PasswordTextField { get; set; }

		[Outlet]
		UIKit.UIButton SelectCountryButton { get; set; }

		[Outlet]
		UIKit.UIButton ShowPasswordButton { get; set; }

		[Outlet]
		UIKit.UIButton SignupButton { get; set; }

		[Outlet]
		UIKit.UIButton SignupShakeTriggerButton { get; set; }

		[Outlet]
		UIKit.NSLayoutConstraint TopConstraint { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (ActivityIndicator != null) {
				ActivityIndicator.Dispose ();
				ActivityIndicator = null;
			}

			if (CountryDropDownCaretImageView != null) {
				CountryDropDownCaretImageView.Dispose ();
				CountryDropDownCaretImageView = null;
			}

			if (CountryNotSelectedImageView != null) {
				CountryNotSelectedImageView.Dispose ();
				CountryNotSelectedImageView = null;
			}

			if (EmailTextField != null) {
				EmailTextField.Dispose ();
				EmailTextField = null;
			}

			if (ErrorLabel != null) {
				ErrorLabel.Dispose ();
				ErrorLabel = null;
			}

			if (GoogleSignupButton != null) {
				GoogleSignupButton.Dispose ();
				GoogleSignupButton = null;
			}

			if (LoginCard != null) {
				LoginCard.Dispose ();
				LoginCard = null;
			}

			if (PasswordTextField != null) {
				PasswordTextField.Dispose ();
				PasswordTextField = null;
			}

			if (SelectCountryButton != null) {
				SelectCountryButton.Dispose ();
				SelectCountryButton = null;
			}

			if (ShowPasswordButton != null) {
				ShowPasswordButton.Dispose ();
				ShowPasswordButton = null;
			}

			if (SignupButton != null) {
				SignupButton.Dispose ();
				SignupButton = null;
			}

			if (SignupShakeTriggerButton != null) {
				SignupShakeTriggerButton.Dispose ();
				SignupShakeTriggerButton = null;
			}

			if (TopConstraint != null) {
				TopConstraint.Dispose ();
				TopConstraint = null;
			}

			if (EmailFieldTopConstraint != null) {
				EmailFieldTopConstraint.Dispose ();
				EmailFieldTopConstraint = null;
			}
		}
	}
}
