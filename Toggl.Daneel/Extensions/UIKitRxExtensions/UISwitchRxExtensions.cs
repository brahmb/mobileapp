using System;
using UIKit;

namespace Toggl.Daneel.Extensions
{
    public static partial class UIKitRxExtensions
    {
        public static Action<bool> BindIsOn(this UISwitch @switch)
            => isOn => @switch.SetState(isOn, true);
    }
}
