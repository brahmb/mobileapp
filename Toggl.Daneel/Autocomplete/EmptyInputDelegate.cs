using Foundation;
using UIKit;

namespace Toggl.Daneel.Autocomplete
{
    public sealed class EmptyInputDelegate : NSObject, IUITextInputDelegate
    {
        public void SelectionDidChange(IUITextInput uiTextInput)
        {
        }

        public void SelectionWillChange(IUITextInput uiTextInput)
        {
        }

        public void TextDidChange(IUITextInput textInput)
        {
        }

        public void TextWillChange(IUITextInput textInput)
        {
        }
    }
}
