using System;
using System.Reactive.Linq;
using UIKit;

namespace Toggl.Daneel.Extensions
{
    public static partial class UIKitRxExtensions
    {
        public static IObservable<DateTimeOffset> DateChanged(this UIDatePicker datePicker)
            => Observable
                .FromEventPattern(e => datePicker.ValueChanged += e, e => datePicker.ValueChanged -= e)
                .Select(e => ((UIDatePicker) e.Sender).Date.ToDateTimeOffset());

        public static IObservable<DateTimeOffset> DateComponentChanged(this UIDatePicker datePicker)
            => datePicker.DateChanged()
                .StartWith(datePicker.Date.ToDateTimeOffset())
                .DistinctUntilChanged(d => d.Date)
                .Skip(1);

        public static IObservable<DateTimeOffset> TimeComponentChanged(this UIDatePicker datePicker)
            => datePicker.DateChanged()
                .StartWith(datePicker.Date.ToDateTimeOffset())
                .DistinctUntilChanged(d => d.TimeOfDay)
                .Skip(1);
    }
}
