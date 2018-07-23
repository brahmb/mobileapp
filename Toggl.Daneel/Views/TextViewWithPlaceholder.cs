using System;
using Foundation;
using MvvmCross.Base;
using MvvmCross.Core;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Foundation.MvvmCross.Helper;
using UIKit;

namespace Toggl.Daneel.Views
{
    [Register(nameof(TextViewWithPlaceholder))]
    public class TextViewWithPlaceholder : UITextView, IUITextViewDelegate
    {
        private readonly int defaultPlaceholderSize = 14;
        private readonly UIColor defaultPlaceholderColor = Color.Common.PlaceholderText.ToNativeColor();

        private bool isFocused;
        private UIStringAttributes placeholderAttributes;
        protected UIStringAttributes DefaultTextAttributes { get; private set; } 

        public event EventHandler TextChanged;
        public event EventHandler DidBecomeFirstResponder;

        private UIColor placeholderColor;
        public UIColor PlaceholderColor
        {
            get => placeholderColor;
            set
            {
                placeholderColor = value;
                updatePlaceholderStyle();
            }
        }

        private nfloat placeholderSize;
        public nfloat PlaceholderSize
        {
            get => placeholderSize;
            set
            {
                placeholderSize = value;
                updatePlaceholderStyle();
            }
        }

        private string text;
        public override string Text
        {
            get => text;
            set
            {
                if (text == value)
                    return;
                
                text = value;
                updateAttributedText(value);
                TextChanged.Raise(this);
            }
        }

        public string PlaceholderText { get; set; }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            Delegate = this;

            PlaceholderSize = defaultPlaceholderSize;
            PlaceholderColor = defaultPlaceholderColor;

            DefaultTextAttributes = new UIStringAttributes
            {
                ParagraphStyle = new NSMutableParagraphStyle { Alignment = TextAlignment },
                Font = UIFont.SystemFontOfSize(Font.PointSize)
            };

            updateAttributedText(Text);
        }

        private void updatePlaceholderStyle()
        {
            placeholderAttributes = new UIStringAttributes
            {
                ParagraphStyle = new NSMutableParagraphStyle { Alignment = TextAlignment },
                ForegroundColor = PlaceholderColor,
                Font = UIFont.SystemFontOfSize(PlaceholderSize)
            };

            updateAttributedText(Text);
        }

        public TextViewWithPlaceholder(IntPtr handle) : base(handle)
        {
        }

        private void updateAttributedText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                AttributedText = isFocused
                    ? new NSAttributedString("")
                    : new NSAttributedString(PlaceholderText ?? "", placeholderAttributes);

                return;
            }

            SetText(text);
        }

        protected virtual void SetText(string text)
            => AttributedText = new NSAttributedString(text, DefaultTextAttributes);

        [Export("textView:shouldChangeTextInRange:replacementText:")]
        public virtual new bool ShouldChangeText(UITextView textView, NSRange range, string text)
        {
            if (text == Environment.NewLine)
            {
                ResignFirstResponder();
                return false;
            }

            return true;
        }

        [Export("textViewDidChange:")]
        public virtual new void Changed(UITextView textView)
        {
            // When the `MarkedTextRange` property of the UITextView is not null
            // then it means that the user is in the middle of inputting a multistage character.
            // Hold off on editing the attributedText until they are done.
            // Source: https://stackoverflow.com/questions/31430308/uitextview-attributedtext-with-japanese-keyboard-repeats-input
            if (textView.MarkedTextRange != null) return;

            Text = base.Text.Replace(Environment.NewLine, " ");
        }

        [Export("textViewDidBeginEditing:")]
        public void EditingStarted(UITextView textView)
        {
            isFocused = true;
            if (Text.Length == 0)
            {
                // this will force the text view to change the color of the text
                // so if the person starts typing a multistage character, the color
                // of the text won't be the color of the placeholder anymore
                updateAttributedText(" ");
            }
            updateAttributedText(Text);
        }

        [Export("textViewDidEndEditing:")]
        public void EditingEnded(UITextView view)
        {
            isFocused = false;
            updateAttributedText(Text);
        }

        public override bool BecomeFirstResponder()
        {
            var becomeFirstResponder = base.BecomeFirstResponder();

            if (becomeFirstResponder)
            {
                DidBecomeFirstResponder.Raise(this);
            }

            return becomeFirstResponder;
        }
    }
}
