﻿using System;
using UIKit;

namespace Toggl.Daneel.Extensions
{
    public static partial class UIKitRxExtensions
    {
        public static Action<bool> BindEnabled(this UIControl control)
            => enabled => control.Enabled = enabled;
    }
}
