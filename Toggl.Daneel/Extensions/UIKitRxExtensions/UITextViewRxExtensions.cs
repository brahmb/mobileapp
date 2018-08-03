using System;
using System.Reactive.Linq;
using Foundation;
using UIKit;

namespace Toggl.Daneel.Extensions
{
    public static partial class UIKitRxExtensions
    {
        private const string selectedTextRangeChangedKey = "selectedTextRange";

        public static IObservable<string> Text(this UITextView textView)
            => Observable
                .FromEventPattern(e => textView.Changed += e, e => textView.Changed -= e)
                .Select(e => ((UITextView)e.Sender).Text);

        public static IObservable<NSAttributedString> AttributedText(this UITextView textView)
            => Observable
                .FromEventPattern(e => textView.Changed += e, e => textView.Changed -= e)
                .Select(e => ((UITextView)e.Sender).AttributedText);

        public static IObservable<int> CursorPosition(this UITextView textView)
            => Observable.Create<int>(observer =>
                {
                    var selectedTextRangeDisposable = textView.AddObserver(
                        selectedTextRangeChangedKey,
                        NSKeyValueObservingOptions.OldNew,
                        _ => observer.OnNext((int)textView.SelectedRange.Location)
                    );

                    return selectedTextRangeDisposable;
                })
                .StartWith((int)textView.SelectedRange.Location);

        public static Action<string> BindText(this UITextView textView)
            => text => textView.Text = text;

        public static Action<string> BindText(this UITextField textField)
            => text => textField.Text = text;
    }
}
