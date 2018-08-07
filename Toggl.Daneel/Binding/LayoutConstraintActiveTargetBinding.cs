using System;
using MvvmCross.Binding;
using MvvmCross.Binding.Bindings.Target;
using UIKit;

namespace Toggl.Daneel.Binding
{
    public sealed class LayoutConstraintActiveTargetBinding
        : MvxTargetBinding<NSLayoutConstraint, bool>
    {
        public const string BindingName = "Active";

        public LayoutConstraintActiveTargetBinding(NSLayoutConstraint target)
            : base(target)
        {
        }

        public override MvxBindingMode DefaultMode => MvxBindingMode.OneWay;

        protected override void SetValue(bool value)
        {
            Target.Active = value;
            var targetView = (UIView)Target.FirstItem;
            targetView.LayoutIfNeeded();
        }
    }
}
