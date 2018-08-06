// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;

namespace Toggl.Daneel.ViewControllers
{
    [Register ("StartTimeEntryViewController")]
    partial class StartTimeEntryViewController
    {
        [Outlet]
        UIKit.UIView AddProjectOnboardingBubble { get; set; }


        [Outlet]
        UIKit.UILabel DescriptionRemainingLengthLabel { get; set; }


        [Outlet]
        Toggl.Daneel.Views.AutocompleteTextViewPlaceholder Placeholder { get; set; }


        [Outlet]
        UIKit.UIButton StartDateButton { get; set; }


        [Outlet]
        Toggl.Daneel.Views.EditDuration.DurationField TimeInput { get; set; }


        [Outlet]
        UIKit.NSLayoutConstraint TimeInputTrailingConstraint { get; set; }


        [Outlet]
        UIKit.UILabel TimeLabel { get; set; }


        [Outlet]
        UIKit.NSLayoutConstraint TimeLabelTrailingConstraint { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton BillableButton { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.NSLayoutConstraint BillableButtonWidthConstraint { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.NSLayoutConstraint BottomDistanceConstraint { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIView BottomOptionsSheet { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton CloseButton { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton DateTimeButton { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITextView DescriptionTextView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton DoneButton { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton ProjectsButton { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITableView SuggestionsTableView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton TagsButton { get; set; }

        void ReleaseDesignerOutlets ()
        {
            if (AddProjectOnboardingBubble != null) {
                AddProjectOnboardingBubble.Dispose ();
                AddProjectOnboardingBubble = null;
            }

            if (BillableButton != null) {
                BillableButton.Dispose ();
                BillableButton = null;
            }

            if (BillableButtonWidthConstraint != null) {
                BillableButtonWidthConstraint.Dispose ();
                BillableButtonWidthConstraint = null;
            }

            if (BottomDistanceConstraint != null) {
                BottomDistanceConstraint.Dispose ();
                BottomDistanceConstraint = null;
            }

            if (BottomOptionsSheet != null) {
                BottomOptionsSheet.Dispose ();
                BottomOptionsSheet = null;
            }

            if (CloseButton != null) {
                CloseButton.Dispose ();
                CloseButton = null;
            }

            if (DateTimeButton != null) {
                DateTimeButton.Dispose ();
                DateTimeButton = null;
            }

            if (DescriptionRemainingLengthLabel != null) {
                DescriptionRemainingLengthLabel.Dispose ();
                DescriptionRemainingLengthLabel = null;
            }

            if (DescriptionTextView != null) {
                DescriptionTextView.Dispose ();
                DescriptionTextView = null;
            }

            if (DoneButton != null) {
                DoneButton.Dispose ();
                DoneButton = null;
            }

            if (Placeholder != null) {
                Placeholder.Dispose ();
                Placeholder = null;
            }

            if (ProjectsButton != null) {
                ProjectsButton.Dispose ();
                ProjectsButton = null;
            }

            if (StartDateButton != null) {
                StartDateButton.Dispose ();
                StartDateButton = null;
            }

            if (SuggestionsTableView != null) {
                SuggestionsTableView.Dispose ();
                SuggestionsTableView = null;
            }

            if (TagsButton != null) {
                TagsButton.Dispose ();
                TagsButton = null;
            }

            if (TimeInput != null) {
                TimeInput.Dispose ();
                TimeInput = null;
            }

            if (TimeInputTrailingConstraint != null) {
                TimeInputTrailingConstraint.Dispose ();
                TimeInputTrailingConstraint = null;
            }

            if (TimeLabel != null) {
                TimeLabel.Dispose ();
                TimeLabel = null;
            }

            if (TimeLabelTrailingConstraint != null) {
                TimeLabelTrailingConstraint.Dispose ();
                TimeLabelTrailingConstraint = null;
            }
        }
    }
}