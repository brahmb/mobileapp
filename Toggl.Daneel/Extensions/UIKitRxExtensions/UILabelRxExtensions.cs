using System;
using Foundation;
using UIKit;

namespace Toggl.Daneel.Extensions
{
    public static partial class UIKitRxExtensions
    {
        public static Action<string> BindText(this UILabel label)
            => text => label.Text = text;

        public static Action<NSAttributedString> BindAttributedText(this UILabel label)
            => text => label.AttributedText = text;
    }
}
