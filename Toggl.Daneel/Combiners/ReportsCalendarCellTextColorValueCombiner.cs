using System;
using System.Collections.Generic;
using System.Linq;
using MvvmCross.Binding.Bindings.SourceSteps;
using MvvmCross.Binding.Combiners;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Foundation.MvvmCross.Helper;
using UIKit;

namespace Toggl.Daneel.Combiners
{
    public sealed class ReportsCalendarCellTextColorValueCombiner : MvxValueCombiner
    {
        private readonly UIColor otherMonthColor = Color.ReportsCalendar.CellTextColorOutOfCurrentMonth.ToNativeColor();
        private readonly UIColor thisMonthColor = Color.ReportsCalendar.CellTextColorInCurrentMonth.ToNativeColor();
        private readonly UIColor selectedColor = Color.ReportsCalendar.CellTextColorSelected.ToNativeColor();

        public override bool TryGetValue(IEnumerable<IMvxSourceStep> steps, out object value)
        {
            var stepArray = steps.ToArray();

            if (stepArray.Length < 2)
                throw new ArgumentException($"{nameof(steps)} must have 2 values");

            var isInCurrentMonth = (bool)stepArray[0].GetValue();
            var isSelected = (bool)stepArray[1].GetValue();

            if (isSelected)
            {
                value = selectedColor;
                return true;
            }

            value = isInCurrentMonth ? thisMonthColor : otherMonthColor;

            return true;
        }
    }
}
