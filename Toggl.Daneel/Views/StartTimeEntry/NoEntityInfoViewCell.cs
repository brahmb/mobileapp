using System;
using System.ComponentModel;
using Foundation;
using MvvmCross.Platforms.Ios.Binding.Views;
using Toggl.Daneel.Extensions;
using Toggl.Foundation.Autocomplete.Suggestions;
using UIKit;

namespace Toggl.Daneel.Views.StartTimeEntry
{
    public sealed partial class NoEntityInfoViewCell : MvxTableViewCell
    {
        public static readonly NSString Key = new NSString(nameof(NoEntityInfoViewCell));
        public static readonly UINib Nib;

        private NoEntityInfoMessage noEntityInfoMessage;
        public NoEntityInfoMessage NoEntityInfoMessage
        {
            get => noEntityInfoMessage;
            set
            {
                if (noEntityInfoMessage.Equals(value)) return;
                noEntityInfoMessage = value;
                Label.AttributedText = value.ToAttributedString(Label.Font.CapHeight);
            }
        }

        static NoEntityInfoViewCell()
        {
            Nib = UINib.FromName(nameof(NoEntityInfoViewCell), NSBundle.MainBundle);
        }

        protected NoEntityInfoViewCell(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }
    }
}
