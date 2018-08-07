using System;
using System.Globalization;
using MvvmCross.Converters;

namespace Toggl.Daneel.Converters
{
    public sealed class ReportsCalendarRowCountToCalendarHeightConverter
        : MvxValueConverter<int, nfloat>
    {
        private readonly nfloat rowHeight;
        private readonly nfloat additionalHeight;

        public ReportsCalendarRowCountToCalendarHeightConverter(
            nfloat rowHeight, nfloat additionalHeight)
        {
            this.rowHeight = rowHeight;
            this.additionalHeight = additionalHeight;
        }

        protected override nfloat Convert(int value, Type targetType, object parameter, CultureInfo culture)
            => value * rowHeight + additionalHeight;
    }
}
