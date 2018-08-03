using System;
using System.Linq;
using FluentAssertions;
using FsCheck.Xunit;
using Xunit;

namespace Toggl.Foundation.Tests
{
    public sealed class DurationFieldInfoTests
    {
        public abstract class BaseDurationFieldInfoTest
        {
            protected DurationFieldInfo Field;

            public BaseDurationFieldInfoTest()
            {
                Field = DurationFieldInfo.Empty;
            }

            protected void inputData(string digitsString)
            {
                var digits = digitsString.ToCharArray().Select(digit => digit - '0').ToArray();
                inputDigits(digits);
            }

            protected void inputDigits(params int[] digits)
            {
                foreach (var digit in digits)
                {
                    Field = Field.Push(digit);
                }
            }

            protected void popNTimes(int n)
            {
                for (int i = 0; i < n; i++)
                {
                    Field = Field.Pop();
                }
            }
        }

        public sealed class TheFromTimeSpanMethod : BaseDurationFieldInfoTest
        {
            [Property]
            public void ClampsTheValueBetweenZeroAndTheMaxValue(TimeSpan input)
            {
                var output = DurationFieldInfo.FromTimeSpan(input).ToTimeSpan();

                output.Should().BeGreaterOrEqualTo(TimeSpan.Zero);
                output.Should().BeLessOrEqualTo(TimeSpan.FromHours(999));
            }

            [Theory, LogIfTooSlow]
            [InlineData(0, 0)]
            [InlineData(0, 1)]
            [InlineData(1, 0)]
            [InlineData(12, 30)]
            [InlineData(0, 30)]
            [InlineData(123, 1)]
            public void ConvertsTheTimeSpanCorrectly(int hours, int minutes)
            {
                var timeSpan = TimeSpan.FromHours(hours).Add(TimeSpan.FromMinutes(minutes));

                var durationField = DurationFieldInfo.FromTimeSpan(timeSpan);

                durationField.Hours.Should().Be(hours);
                durationField.Minutes.Should().Be(minutes);
            }
        }

        public sealed class ThePushMethod : BaseDurationFieldInfoTest
        {
            [Theory, LogIfTooSlow]
            [InlineData("", 0, 0)]
            [InlineData("1", 0, 1)]
            [InlineData("12", 0, 12)]
            [InlineData("123", 1, 23)]
            [InlineData("1234", 12, 34)]
            [InlineData("12345", 123, 45)]
            [InlineData("99999", 999, 99)]
            [InlineData("87", 0, 87)]
            public void InterpretsTheDigitsInCorrectOrder(string inputSequence, int expectedHours, int expectedMinutes)
            {
                inputData(inputSequence);

                Field.Hours.Should().Be(expectedHours);
                Field.Minutes.Should().Be(expectedMinutes);
            }

            [Theory, LogIfTooSlow]
            [InlineData("123456", 123, 45)]
            [InlineData("1234567", 123, 45)]
            [InlineData("12345678", 123, 45)]
            [InlineData("123456789", 123, 45)]
            public void IgnoresMoreThanFiveInputDigits(string inputSequence, int expectedHours, int expectedMinutes)
            {
                inputData(inputSequence);

                Field.Hours.Should().Be(expectedHours);
                Field.Minutes.Should().Be(expectedMinutes);
            }
        }

        public sealed class ThePopMethod : BaseDurationFieldInfoTest
        {
            [Theory, LogIfTooSlow]
            [InlineData("19", 1, 0, 1)]
            [InlineData("127", 1, 0, 12)]
            [InlineData("650", 1, 0, 65)]
            [InlineData("1234", 2, 0, 12)]
            [InlineData("12345", 2, 1, 23)]
            [InlineData("99999", 3, 0, 99)]
            [InlineData("87", 2, 0, 0)]
            public void RemovesTheDigitsWhichWereAddedTheLast(string inputSequence, int popCount, int expectedHours, int expectedMinutes)
            {
                inputData(inputSequence);

                popNTimes(popCount);

                Field.Hours.Should().Be(expectedHours);
                Field.Minutes.Should().Be(expectedMinutes);
            }

            [Theory, LogIfTooSlow]
            [InlineData("")]
            [InlineData("12345")]
            [InlineData("661")]
            public void StopsPoppingWhenEmpty(string inputSequence)
            {
                inputData(inputSequence);

                popNTimes(inputSequence.Length + 3);

                Field.Hours.Should().Be(0);
                Field.Minutes.Should().Be(0);
            }
        }

        public sealed class TheToStringMethod : BaseDurationFieldInfoTest
        {
            [Theory, LogIfTooSlow]
            [InlineData("", "00:00")]
            [InlineData("0", "00:00")]
            [InlineData("00", "00:00")]
            [InlineData("000", "00:00")]
            [InlineData("0000", "00:00")]
            [InlineData("00000", "00:00")]
            [InlineData("000000", "00:00")]
            public void IgnoresLeadingZeros(string inputSequence, string expectedOutput)
            {
                inputData(inputSequence);

                var output = Field.ToString();

                output.Should().Be(expectedOutput);
            }

            [Theory, LogIfTooSlow]
            [InlineData("60", "00:60")]
            [InlineData("181", "01:81")]
            [InlineData("99999", "999:99")]
            public void DoesNotCarryMinutesToHoursWhenTheNumberOfMinutesIsMoreThanFiftyNine(string inputSequence, string expectedOutput)
            {
                inputData(inputSequence);

                var output = Field.ToString();

                output.Should().Be(expectedOutput);
            }

            [Theory, LogIfTooSlow]
            [InlineData("1", "00:01")]
            [InlineData("2", "00:02")]
            [InlineData("12", "00:12")]
            [InlineData("89", "00:89")]
            [InlineData("45", "00:45")]
            [InlineData("90", "00:90")]
            public void ShowsTwoLeadingZerosWhenThereAreZeroHours(string inputSequence, string expectedOutput)
            {
                inputData(inputSequence);

                var output = Field.ToString();

                output.Should().Be(expectedOutput);
            }

            [Theory, LogIfTooSlow]
            [InlineData("100", "01:00")]
            [InlineData("200", "02:00")]
            [InlineData("120", "01:20")]
            [InlineData("950", "09:50")]
            public void ShowsALeadingZerosWhenThereIsASingleDigitHour(string inputSequence, string expectedOutput)
            {
                inputData(inputSequence);

                var output = Field.ToString();

                output.Should().Be(expectedOutput);
            }

            [Theory, LogIfTooSlow]
            [InlineData("1200", "12:00")]
            [InlineData("4201", "42:01")]
            [InlineData("1234", "12:34")]
            [InlineData("9999", "99:99")]
            public void DoesNotShowALeadingZeroWhenTheDurationIsFourDigitsLong(string inputSequence, string expectedOutput)
            {
                inputData(inputSequence);

                var output = Field.ToString();

                output.Should().Be(expectedOutput);
            }

            [Theory, LogIfTooSlow]
            [InlineData("10000", "100:00")]
            [InlineData("12345", "123:45")]
            [InlineData("99999", "999:99")]
            public void ShowsFiveCharactersWhenTheNumberOfHoursIsMoreThanNinetyNine(string inputSequence, string expectedOutput)
            {
                inputData(inputSequence);

                var output = Field.ToString();

                output.Should().Be(expectedOutput);
            }
        }

        public sealed class TheToTimeSpanMethod : BaseDurationFieldInfoTest
        {
            [Theory, LogIfTooSlow]
            [InlineData("99861")]
            [InlineData("99901")]
            [InlineData("99999")]
            public void ClampsDurationToMaximumTime(string inputSequence)
            {
                inputData(inputSequence);

                var output = Field.ToTimeSpan();

                output.Should().Be(TimeSpan.FromHours(999));
            }

            [Theory, LogIfTooSlow]
            [InlineData("12", 12)]
            [InlineData("123", 1 * 60 + 23)]
            [InlineData("1177", 11 * 60 + 77)]
            [InlineData("789", 7 * 60 + 89)]
            public void CorrectlyCalculatesTheDuration(string inputSequence, double totalMinutes)
            {
                inputData(inputSequence);

                var output = Field.ToTimeSpan();

                output.TotalMinutes.Should().Be(totalMinutes);
            }
        }

        public sealed class TheEmptyStaticProperty
        {
            [Fact, LogIfTooSlow]
            public void MinutesAndHoursAreZero()
            {
                var field = DurationFieldInfo.Empty;

                field.Hours.Should().Be(0);
                field.Minutes.Should().Be(0);
            }

            [Fact, LogIfTooSlow]
            public void SerializesIntoZerosOnly()
            {
                DurationFieldInfo.Empty.ToString().Should().Be("00:00");
            }

            [Fact, LogIfTooSlow]
            public void ConvertsIntoZeroTimeSpan()
            {
                DurationFieldInfo.Empty.ToTimeSpan().Should().Be(TimeSpan.Zero);
            }
        }
    }
}
