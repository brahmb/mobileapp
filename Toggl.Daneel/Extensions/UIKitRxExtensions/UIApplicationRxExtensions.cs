using System;
using UIKit;

namespace Toggl.Daneel.Extensions
{
    public static partial class UIKitRxExtensions
    {
        public static Action<bool> BindNetworkActivityIndicatorVisible(this UIApplication application)
            => visible => application.NetworkActivityIndicatorVisible = visible;
    }
}
