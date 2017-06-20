﻿﻿using Toggl.Daneel.Binding;
using UIKit;

namespace Toggl.Daneel.Extensions
{  
    public static class ViewBindingExtensions
    {

        public static string BindCurrentPage(this UIScrollView self)
            => ScrollViewCurrentPageTargetBinding.BindingName;

        public static string BindAnimatedBackground(this UIView self)
            => ViewAnimatedBackgroundTargetBinding.BindingName;
    }
}