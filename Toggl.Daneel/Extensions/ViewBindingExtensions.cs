using Toggl.Daneel.Binding;
using UIKit;
using Toggl.Daneel.Views;
using MvvmCross.Platforms.Ios.Binding.Views;
using System.Collections.Generic;

namespace Toggl.Daneel.Extensions
{
    public static class ViewBindingExtensions
    {

        public static string BindAnimatedEnabled(this UIBarButtonItem self)
            => BarButtonAnimatedEnabledTargetBinding.BindingName;

        public static string BindCommand(this UIBarButtonItem self)
            => BarButtonCommandTargetBinding.BindingName;

        public static string BindAnimatedTitle(this UIButton self)
            => ButtonAnimatedTitleTargetBinding.BindingName;

        public static string BindImage(this UIButton self)
            => ButtonImageTargetBinding.BindingName;

        public static string BindDateTimeOffset(this UIDatePicker self)
            => DatePickerDateTimeOffsetTargetBinding.BindingName;

        public static string BindAnimatedImage(this UIImageView self)
            => ImageViewAnimatedImageTargetBinding.BindingName;

        public static string BindAnimatedConstant(this NSLayoutConstraint self)
            => LayoutConstraintAnimatedConstantTargetBinding.BindingName;

        public static string BindConstant(this NSLayoutConstraint self)
            => LayoutConstraintConstantTargetBinding.BindingName;

        public static string BindActive(this NSLayoutConstraint self)
            => LayoutConstraintActiveTargetBinding.BindingName;

        public static string BindFirstResponder(this LoginTextField self)
            => LoginTextFieldFirstResponderTargetBinding.BindingName;

        public static string BindHidesBackButton(this UINavigationItem self)
            => NavigationItemHidesBackButtonTargetBinding.BindingName;

        public static string BindAnimatedCurrentPage(this UIScrollView self)
            => ScrollViewAnimatedCurrentPageTargetBinding.BindingName;

        public static string BindCurrentPage(this UIScrollView self)
            => ScrollViewCurrentPageTargetBinding.BindingName;

        public static string BindAnimatedOn(this UISwitch self)
            => SwitchAnimatedOnTargetBinding.BindingName;

        public static string BindFocus(this UITextField self)
            => TextFieldFocusTargetBinding.BindingName;

        public static string BindPlaceholder(this UITextField self)
            => TextFieldPlaceholderTargetBinding.BindingName;

        public static string BindSecureTextEntry(this UITextField self)
            => TextFieldSecureTextEntryTargetBinding.BindingName;

        public static string BindTags(this UITextView self)
            => TextViewTagListTargetBinding.BindingName;

        public static string BindText(this TextViewWithPlaceholder self)
            => TextViewWithPlaceholderTextTargetBinding.BindingName;

        public static string BindDidBecomeFirstResponder(this TextViewWithPlaceholder self)
            => TextViewWithPlaceholderDidBecomeFirstResponderTargetBinding.BindingName;

        public static string BindAnimatedBackground(this UIView self)
            => ViewAnimatedBackgroundTargetBinding.BindingName;

        public static string BindAnimatedVisibility(this UIView self)
            => ViewAnimatedVisibilityTargetBinding.BindingName;

        public static string BindLongPress(this UIView self)
            => ViewLongPressCommandTargetBinding.BindingName;

        public static string BindVisibilityWithFade(this UIView self)
            => ViewVisibilityWithFadeTargetBinding.BindingName;
    }
}
