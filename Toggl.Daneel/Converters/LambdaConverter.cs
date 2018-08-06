using System;
using System.Globalization;
using MvvmCross.Converters;
using Toggl.Multivac;

namespace Toggl.Daneel.Converters
{
    [MvvmCross.Preserve(AllMembers = true)]
    public sealed class LambdaConverter<TIn, TOut> : MvxValueConverter<TIn, TOut>
    {
        private Func<TIn, TOut> convert;

        public LambdaConverter(Func<TIn, TOut> convert)
        {
            Ensure.Argument.IsNotNull(convert, nameof(convert));

            this.convert = convert;
        }

        protected override TOut Convert(TIn value, Type targetType, object parameter, CultureInfo culture)
            => convert(value);
    }
}
