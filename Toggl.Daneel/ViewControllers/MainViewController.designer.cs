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
    [Register ("MainViewController")]
    partial class MainViewController
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIView CurrentTimeEntryCard { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIKit.UILabel CurrentTimeEntryDescriptionLabel { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIKit.UILabel CurrentTimeEntryElapsedTimeLabel { get; set; }

		[Outlet]
		UIKit.UILabel CurrentTimeEntryProjectTaskClientLabel { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIKit.UIButton EditTimeEntryButton { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		Toggl.Daneel.Views.FadeView RunningEntryDescriptionFadeView { get; set; }

        [Outlet]
        UIKit.UIView SendFeedbackSuccessView { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIKit.UIButton StartTimeEntryButton { get; set; }

		[Outlet]
		UIKit.UIView StartTimeEntryOnboardingBubbleView { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIKit.UIButton StopTimeEntryButton { get; set; }

		[Outlet]
		UIKit.UIView StopTimeEntryOnboardingBubbleView { get; set; }

		[Outlet]
		UIKit.UIView SwipeLeftBubbleView { get; set; }

		[Outlet]
		UIKit.NSLayoutConstraint SwipeLeftTopConstraint { get; set; }

		[Outlet]
		UIKit.UIView SwipeRightBubbleView { get; set; }

		[Outlet]
		UIKit.NSLayoutConstraint SwipeRightTopConstraint { get; set; }

		[Outlet]
		UIKit.UIView TapToEditBubbleView { get; set; }

		[Outlet]
		UIKit.NSLayoutConstraint TapToEditBubbleViewTopConstraint { get; set; }

		[Outlet]
        Toggl.Daneel.Views.TimeEntriesLogTableView TimeEntriesLogTableView { get; set; }

		[Outlet]
		UIKit.NSLayoutConstraint TopConstraint { get; set; }

		[Outlet]
		UIKit.UIView TopSeparator { get; set; }

		[Outlet]
		UIKit.UIView WelcomeBackView { get; set; }

		void ReleaseDesignerOutlets ()
		{
			if (CurrentTimeEntryCard != null) {
				CurrentTimeEntryCard.Dispose ();
				CurrentTimeEntryCard = null;
			}

			if (CurrentTimeEntryDescriptionLabel != null) {
				CurrentTimeEntryDescriptionLabel.Dispose ();
				CurrentTimeEntryDescriptionLabel = null;
			}

			if (CurrentTimeEntryElapsedTimeLabel != null) {
				CurrentTimeEntryElapsedTimeLabel.Dispose ();
				CurrentTimeEntryElapsedTimeLabel = null;
			}

			if (CurrentTimeEntryProjectTaskClientLabel != null) {
				CurrentTimeEntryProjectTaskClientLabel.Dispose ();
				CurrentTimeEntryProjectTaskClientLabel = null;
			}

			if (EditTimeEntryButton != null) {
				EditTimeEntryButton.Dispose ();
				EditTimeEntryButton = null;
			}

			if (RunningEntryDescriptionFadeView != null) {
				RunningEntryDescriptionFadeView.Dispose ();
				RunningEntryDescriptionFadeView = null;
			}

			if (StartTimeEntryButton != null) {
				StartTimeEntryButton.Dispose ();
				StartTimeEntryButton = null;
			}

			if (StartTimeEntryOnboardingBubbleView != null) {
				StartTimeEntryOnboardingBubbleView.Dispose ();
				StartTimeEntryOnboardingBubbleView = null;
			}

			if (StopTimeEntryButton != null) {
				StopTimeEntryButton.Dispose ();
				StopTimeEntryButton = null;
			}

			if (TapToEditBubbleView != null) {
				TapToEditBubbleView.Dispose ();
				TapToEditBubbleView = null;
			}

			if (TapToEditBubbleViewTopConstraint != null) {
				TapToEditBubbleViewTopConstraint.Dispose ();
				TapToEditBubbleViewTopConstraint = null;
			}

			if (StopTimeEntryOnboardingBubbleView != null) {
				StopTimeEntryOnboardingBubbleView.Dispose ();
				StopTimeEntryOnboardingBubbleView = null;
			}

			if (TimeEntriesLogTableView != null) {
				TimeEntriesLogTableView.Dispose ();
				TimeEntriesLogTableView = null;
			}

			if (TopConstraint != null) {
				TopConstraint.Dispose ();
				TopConstraint = null;
			}

			if (TopSeparator != null) {
				TopSeparator.Dispose ();
				TopSeparator = null;
			}

			if (WelcomeBackView != null) {
				WelcomeBackView.Dispose ();
				WelcomeBackView = null;
			}

		    if (SendFeedbackSuccessView != null) {
		        SendFeedbackSuccessView.Dispose ();
		        SendFeedbackSuccessView = null;
		    }

			if (SwipeRightBubbleView != null) {
				SwipeRightBubbleView.Dispose ();
				SwipeRightBubbleView = null;
			}

			if (SwipeLeftBubbleView != null) {
				SwipeLeftBubbleView.Dispose ();
				SwipeLeftBubbleView = null;
			}

			if (SwipeLeftTopConstraint != null) {
				SwipeLeftTopConstraint.Dispose ();
				SwipeLeftTopConstraint = null;
			}

			if (SwipeRightTopConstraint != null) {
				SwipeRightTopConstraint.Dispose ();
				SwipeRightTopConstraint = null;
			}
		}
	}
}
