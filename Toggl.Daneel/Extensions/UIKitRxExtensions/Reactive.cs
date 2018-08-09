﻿using Foundation;

namespace Toggl.Daneel.Extensions
{
    public class Reactive<TBase>
    {
        public TBase Base { get; set; }

        public Reactive(TBase @base)
        {
            Base = @base;
        }
    }

    public static class NSObjectReactiveCompatibleExtensions
    {
        public static Reactive<T> Rx<T>(this T type) where T: NSObject
            => new Reactive<T>(type);
    }
}

