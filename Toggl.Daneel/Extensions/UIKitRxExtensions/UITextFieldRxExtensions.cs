using System;
using System.Reactive.Linq;
using UIKit;

namespace Toggl.Daneel.Extensions
{
    public static partial class UIKitRxExtensions
    {
        public static IObservable<string> Text(this UITextField textField)
            => Observable
                .FromEventPattern(handler => textField.EditingChanged += handler, handler => textField.EditingChanged -= handler)
                .Select(_ => textField.Text);
        
        public static Action<bool> BindSecureTextEntry(this UITextField textField) => isSecure =>
        {
            textField.ResignFirstResponder();
            textField.SecureTextEntry = isSecure;
            textField.BecomeFirstResponder();
        };
    }
}
