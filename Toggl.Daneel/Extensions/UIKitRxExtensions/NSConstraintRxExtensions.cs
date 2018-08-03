using System;
using UIKit;

namespace Toggl.Daneel.Extensions
{
    public static partial class UIKitRxExtensions
    {
        public static Action<nfloat> BindConstant(this NSLayoutConstraint constraint)
            => constant => constraint.Constant = constant;
    }
}
