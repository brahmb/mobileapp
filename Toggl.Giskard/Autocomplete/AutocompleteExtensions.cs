using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Android.Text;
using Android.Text.Style;
using Java.Lang;
using Toggl.Foundation.Autocomplete;
using Toggl.Foundation.Autocomplete.Span;
using Toggl.Giskard.Autocomplete;
using Toggl.Multivac.Extensions;

namespace Toggl.Giskard.Extensions
{
    public static class AutocompleteExtensions
    {
        private const string unbreakableSpace = " ";
        private const float spanSizeProportion = 0.8f;

        public static IImmutableList<ISpan> AsImmutableSpans(this ICharSequence text, int cursorPosition)
            => text.AsSpans(cursorPosition).ToImmutableList();

        private static IEnumerable<ISpan> AsSpans(this ICharSequence text, int cursorPosition)
        {
            var spannable = text as SpannableStringBuilder;
            var tokenSpans = spannable.GetSpans(0, spannable.Length(), Class.FromType(typeof(TokenSpan)));

            var currentPosition = 0;
            var length = spannable.Length();

            if (tokenSpans.None())
            {
                yield return new QueryTextSpan(text.ToString(), cursorPosition);
                yield break;
            }

            foreach (var tokenSpan in tokenSpans)
            {
                var tokenStart = spannable.GetSpanStart(tokenSpan);
                var tokenEnd = spannable.GetSpanEnd(tokenSpan);

                if (tokenStart != currentPosition)
                    yield return textSpanFromRange(currentPosition, tokenStart);

                switch (tokenSpan)
                {
                    case ProjectTokenSpan projectTokenSpan:
                        yield return new ProjectSpan(projectTokenSpan.ProjectId, projectTokenSpan.ProjectName, projectTokenSpan.ProjectColor);
                        break;
                    case TagsTokenSpan tagTokenSpan:
                        yield return new TagSpan(tagTokenSpan.TagId, tagTokenSpan.TagName);
                        break;
                }

                currentPosition = tokenEnd;
            }

            if (currentPosition != length)
                yield return textSpanFromRange(currentPosition, length);

            TextSpan textSpanFromRange(int start, int end)
            {
                var spanText = spannable.SubSequence(start, end);
                if (cursorPosition.IsInRange(start, end))
                {
                    return new QueryTextSpan(spanText, cursorPosition - currentPosition);
                }
                else
                {
                    return new TextSpan(spanText);
                }
            }
        }

        public static (ISpannable text, int cursorPosition) AsSpannableTextAndCursorPosition(this TextFieldInfo textFieldInfo)
        {
            var builder = new SpannableStringBuilder();
            var finalCursorPosition = 0;
            var currentCursorPosition = 0;

            foreach (var span in textFieldInfo.Spans)
            {
                var startingSize = builder.Length();
                builder.AppendSpan(span);

                if (span is QueryTextSpan querySpan)
                {
                    finalCursorPosition = currentCursorPosition + querySpan.CursorPosition;
                }

                currentCursorPosition += builder.Length() - startingSize;
            }

            return (builder, finalCursorPosition);
        }

        private static SpannableStringBuilder AppendSpan(this SpannableStringBuilder mutableString, ISpan span)
        {
            switch (span)
            {
                case TextSpan textSpan:
                    mutableString.AppendNormalText(textSpan);
                    break;
                case ProjectSpan projectSpan:
                    mutableString.AppendProjectText(projectSpan);
                    break;
                case TagSpan tagSpan:
                    mutableString.AppendTagText(tagSpan);
                    break;
            }

            return mutableString;
        }

        private static void AppendNormalText(this SpannableStringBuilder mutableString, TextSpan textSpan)
        {
            var start = mutableString.Length();
            mutableString.Append(textSpan.Text);
            var end = mutableString.Length();

            mutableString.SetSpan(new RelativeSizeSpan(1), start, end, SpanTypes.ExclusiveExclusive);
        }

        private static void AppendProjectText(this SpannableStringBuilder spannable, ProjectSpan projectSpan)
        {
            /* HACK: This unbreakable space is needed because of a bug in
             * the way android handles ReplacementSpans. It makes sure that
             * all token boundaries can't be changed by the soft input once 
             * they are set. */ 
            var start = spannable.Length();
            spannable.Append(projectSpan.ProjectName);
            spannable.Append(unbreakableSpace);
            var end = spannable.Length();

            var projectTokenSpan = new ProjectTokenSpan(
                projectSpan.ProjectId,
                projectSpan.ProjectName,
                projectSpan.ProjectColor
            );

            spannable.SetSpan(projectTokenSpan, start, end, SpanTypes.ExclusiveExclusive);
            spannable.SetSpan(new RelativeSizeSpan(spanSizeProportion), start, end, SpanTypes.ExclusiveExclusive);
        }

        private static void AppendTagText(this SpannableStringBuilder spannable, TagSpan tagSpan)
        {
            /* HACK: This unbreakable space is needed because of a bug in
             * the way android handles ReplacementSpans. It makes sure that
             * all token boundaries can't be changed by the soft input once 
             * they are set. */
            var start = spannable.Length();
            spannable.Append(tagSpan.TagName);
            spannable.Append(unbreakableSpace);
            var end = spannable.Length();

            spannable.SetSpan(new RelativeSizeSpan(spanSizeProportion), start, end, SpanTypes.ExclusiveExclusive);
            spannable.SetSpan(new TagsTokenSpan(tagSpan.TagId, tagSpan.TagName), start, end, SpanTypes.ExclusiveExclusive);
        }
    }
}
