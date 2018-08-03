using Java.Lang;

namespace Toggl.Giskard.Extensions
{
    public static class StringExtensions
    {
        public static ICharSequence AsCharSequence(this string text)
            => text.AsJavaString();

        public static String AsJavaString(this string text)
            => new String(text);
    }
}
