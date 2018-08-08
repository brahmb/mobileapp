using System;
using System.Globalization;
using FluentAssertions;
using FsCheck;
using Toggl.Foundation.MvvmCross.Transformations;
using Xunit;
using FsCheck.Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.Transformations
{
    public sealed class DateToTitleStringTests
    {
        public sealed class TheConvertMethod
        {
            [Fact, LogIfTooSlow]
            public void ReturnsASpecialCaseStringForTheCurrentDay()
            {
                var date = DateTimeOffset.Now;

                var result = DateToTitleString.Convert(date);

                result.Should().Be(Resources.Today);
            }

            [Fact, LogIfTooSlow]
            public void ReturnsASpecialCaseStringForThePreviousDay()
            {
                var date = DateTimeOffset.Now.AddDays(-1);

                var result = DateToTitleString.Convert(date);

                result.Should().Be(Resources.Yesterday);
            }

            [Property]
            public Property ReturnsAFormattedStringForAnyOtherDate()
            {
                var arb = Arb.Default.DateTimeOffset().Filter(d => d < DateTime.UtcNow.AddDays(-1));
                return Prop.ForAll(arb, date =>
                {
                    var result = DateToTitleString.Convert(date);
                    var expectedCulture = CultureInfo.CreateSpecificCulture("en-US");
                    result.Should().Be(date.ToString("ddd, dd MMM", expectedCulture));
                });
            }
        }
    }
}
