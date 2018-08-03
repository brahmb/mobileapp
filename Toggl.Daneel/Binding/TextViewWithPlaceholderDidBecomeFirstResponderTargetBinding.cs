using System;
using MvvmCross.Binding;
using MvvmCross.Binding.Bindings.Target;
using MvvmCross.Commands;
using Toggl.Daneel.Views;

namespace Toggl.Daneel.Binding
{
    public sealed class TextViewWithPlaceholderDidBecomeFirstResponderTargetBinding: MvxTargetBinding<TextViewWithPlaceholder, IMvxCommand>
    {
        public const string BindingName = "DidBecomeFirstResponder";
        private IMvxCommand command;

        public override MvxBindingMode DefaultMode => MvxBindingMode.OneWay;

        public TextViewWithPlaceholderDidBecomeFirstResponderTargetBinding(TextViewWithPlaceholder target)
            : base(target)
        {
            target.DidBecomeFirstResponder += onDidBecomeFirstResponder;
        }

        protected override void SetValue(IMvxCommand value)
            => command = value;

        private void onDidBecomeFirstResponder(object sender, EventArgs e)
            => command.Execute();

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (!isDisposing)
                return;

            Target.DidBecomeFirstResponder -= onDidBecomeFirstResponder;
        }
    }
}
