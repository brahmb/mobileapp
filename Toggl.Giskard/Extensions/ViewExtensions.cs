using System;
using System.Collections.Generic;
using System.Linq;
using Android.Content;
using Android.Views;
using Android.Views.InputMethods;

namespace Toggl.Giskard.Extensions
{
    public static class ViewExtensions
    {
        public static IEnumerable<View> GetChildren(this ViewGroup view)
        {
            for (int i = 0; i < view.ChildCount; i++)
                yield return view.GetChildAt(i);
        }

        public static IEnumerable<T> GetChildren<T>(this ViewGroup view)
            => view.GetChildren().OfType<T>();

        public static void SetFocus(this View view)
        {
            var service = (InputMethodManager)view.Context.GetSystemService(Context.InputMethodService);

            view.Post(() =>
            {
                view.RequestFocus();
                service.ShowSoftInput(view, ShowFlags.Forced);
            });
        }

        public static void RemoveFocus(this View view)
        {
            var service = (InputMethodManager)view.Context.GetSystemService(Context.InputMethodService);

            view.Post(() =>
            {
                view.ClearFocus();
                service.HideSoftInputFromWindow(view.WindowToken, 0);
            });
        }

        public static void RunWhenAttachedToWindow(this View view, Action action)
        {
            if (view.IsAttachedToWindow)
            {
                view.Post(action);
            }
            else
            {
                EventHandler<View.ViewAttachedToWindowEventArgs> onViewAttachedToWindow = null;
                onViewAttachedToWindow = (sender, args) =>
                {
                    view.ViewAttachedToWindow -= onViewAttachedToWindow;
                    view.Post(action);
                };
                view.ViewAttachedToWindow += onViewAttachedToWindow;
            }
        }
    }
}
