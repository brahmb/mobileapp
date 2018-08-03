using System;

namespace Toggl.Multivac
{
    public struct DateTimeOffsetRange : IEquatable<DateTimeOffsetRange>
    {
        public DateTimeOffsetRange(DateTimeOffset minimum, DateTimeOffset maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }

        public DateTimeOffset Minimum { get; }
        public DateTimeOffset Maximum { get; }

        public bool Equals(DateTimeOffsetRange other)
            => Minimum == other.Minimum && Maximum == other.Maximum;

        public override bool Equals(object obj)
            => obj is DateTimeOffsetRange dateFormat
                ? Equals(dateFormat)
                : false;

        public static bool operator ==(DateTimeOffsetRange range, DateTimeOffsetRange other)
            => range.Equals(other);

        public static bool operator !=(DateTimeOffsetRange range, DateTimeOffsetRange other)
            => !range.Equals(other);

        public override int GetHashCode()
            => HashCode.From(Minimum, Maximum);

        public override string ToString()
            => $"[{Minimum}, {Maximum}]";
    }
}
