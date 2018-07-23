using System;
using System.Reactive;
using System.Reactive.Linq;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Multivac.Extensions;
using UIKit;

namespace Toggl.Daneel.Extensions
{
    public static partial class UIKitRxExtensions
    {
        public static IObservable<Unit> Tapped(this UIButton button)
            => Observable
                .FromEventPattern(e => button.TouchUpInside += e, e => button.TouchUpInside -= e)
                .SelectUnit();

        public static Action<string> BindTitle(this UIButton button)
            => title => button.SetTitle(title, UIControlState.Normal);

        public static Action<UIColor> BindTitleColor(this UIButton button)
            => color => button.SetTitleColor(color, UIControlState.Normal);

        public static Action<string> BindAnimatedTitle(this UIButton button)
            => title =>
            {
                UIView.Transition(
                    button,
                    Animation.Timings.EnterTiming,
                    UIViewAnimationOptions.TransitionCrossDissolve,
                    () => button.SetTitle(title, UIControlState.Normal),
                    null
                );
            };
    }
}
