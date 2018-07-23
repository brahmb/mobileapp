using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Foundation;
using MvvmCross.Plugin.Color.Platforms.Ios;
using MvvmCross.UI;
using Toggl.Foundation.Autocomplete;
using Toggl.Foundation.Autocomplete.Span;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Multivac.Extensions;
using UIKit;
using static Toggl.Daneel.Autocomplete.Constants;

namespace Toggl.Daneel.Autocomplete
{
    using ProjectInformationTuple = ValueTuple<long, string, string, long?, string>;
    using TagInformationTuple = ValueTuple<long, string>;

    public static class AutocompleteExtensions
    {
        private const int lineHeight = 24;
        private const int maxTextLength = 50;

        private static readonly nfloat textVerticalOffset;
        private static readonly NSParagraphStyle paragraphStyle;
        private static readonly UIStringAttributes tagAttributes;
        private static readonly UIFont tokenFont = UIFont.SystemFontOfSize(12, UIFontWeight.Regular);
        private static readonly UIFont regularFont = UIFont.SystemFontOfSize(16, UIFontWeight.Regular);

        static AutocompleteExtensions()
        {
            paragraphStyle = new NSMutableParagraphStyle
            {
                MinimumLineHeight = lineHeight,
                MaximumLineHeight = lineHeight
            };

            tagAttributes = new UIStringAttributes
            {
                Font = tokenFont,
                ForegroundColor = Color.StartTimeEntry.TokenText.ToNativeColor()
            };

            textVerticalOffset = (lineHeight / 2) - (tokenFont.CapHeight / 2) - TokenTextAttachment.TokenMargin;
        }

        public static ProjectInformationTuple GetProjectInformation(this NSDictionary dictionary)
        {
            long? taskId = null;
            string taskName = null;

            var projectId = (long)(dictionary[ProjectId] as NSNumber);
            var projectName = (dictionary[ProjectName] as NSString).ToString();
            var projectColor = (dictionary[ProjectColor] as NSString).ToString();

            if (dictionary.ContainsKey(TaskId))
            {
                taskId = (long)(dictionary[TaskId] as NSNumber);
                taskName = (dictionary[TaskName] as NSString)?.ToString();
            }

            return (projectId, projectName, projectColor, taskId, taskName);
        }

        public static TagInformationTuple GetTagInformation(this NSDictionary dictionary)
        {
            var tagId = (long)(dictionary[TagId] as NSNumber);
            var tagName = (dictionary[TagName] as NSString).ToString();

            return (tagId, tagName);
        }

        public static TokenTextAttachment GetTagToken(this string tag)
            => new TagTextAttachment(
                new NSMutableAttributedString(tag.TruncatedAt(maxTextLength), tagAttributes),
                textVerticalOffset,
                regularFont.Descender
            );

        public static IImmutableList<ISpan> AsImmutableSpans(this NSAttributedString text, int cursorPosition)
            => text.AsSpans(cursorPosition).ToImmutableList();

        private static IEnumerable<ISpan> AsSpans(this NSAttributedString text, int cursorPosition)
        {
            var start = 0;

            while (start != text.Length)
            {
                var attributes = text.GetAttributes(start, out var longestEffectiveRange, new NSRange(start, text.Length - start));
                var length = (int)longestEffectiveRange.Length;
                var end = start + length;

                if (attributes.ContainsKey(ProjectId))
                {
                    var (projectId, projectName, projectColor, taskId, taskName) = attributes.GetProjectInformation();

                    yield return new ProjectSpan(projectId, projectName, projectColor, taskId, taskName);
                }
                else if (attributes.ContainsKey(TagId))
                {
                    var (tagId, tagName) = attributes.GetTagInformation();
                    yield return new TagSpan(tagId, tagName);
                }
                else if (length > 0)
                {
                    var subText = text.Substring(start, length).ToString().Substring(0, length);
                    if (cursorPosition.IsInRange(start, end))
                    {
                        yield return new QueryTextSpan(subText, cursorPosition - start);
                    }
                    else
                    {
                        yield return new TextSpan(subText);
                    }
                }

                start = end;
            }
        }

        public static (NSAttributedString, int) AsAttributedTextAndCursorPosition(this TextFieldInfo self)
        {
            var attributedText = new NSMutableAttributedString("", createBasicAttributes());
            var finalCursorPosition = 0;
            var currentCursorPosition = 0;

            foreach (var span in self.Spans)
            {
                var spanString = span.AsAttributedString();
                attributedText.Append(spanString);

                if (span is QueryTextSpan querySpan)
                {
                    finalCursorPosition = currentCursorPosition + querySpan.CursorPosition;
                }

                currentCursorPosition += (int)spanString.Length;
            }

            return (attributedText, finalCursorPosition);
        }

        private static NSMutableAttributedString AsAttributedString(this ISpan span)
        {
            switch (span)
            {
                case QueryTextSpan querySpan:
                    return querySpan.AsAttributedString();
                case TagSpan tagSpan:
                    return tagSpan.AsAttributedString();
                case ProjectSpan projectSpan:
                    return projectSpan.AsAttributedString();
                case TextSpan textSpan:
                    return textSpan.AsAttributedString();
            }

            throw new ArgumentOutOfRangeException(nameof(span));
        }

        private static NSMutableAttributedString AsAttributedString(this TextSpan textSpan)
            => new NSMutableAttributedString(textSpan.Text, createBasicAttributes());

        private static NSMutableAttributedString AsAttributedString(this ProjectSpan projectSpan)
        {
            var projectColor = MvxColor.ParseHexString(projectSpan.ProjectColor).ToNativeColor();
            var projectName = new NSAttributedString(projectSpan.ProjectName.TruncatedAt(maxTextLength), new UIStringAttributes
            {
                Font = tokenFont,
                ForegroundColor = projectColor
            });

            var textAttachment = new ProjectTextAttachment(
                projectName, projectColor, textVerticalOffset, regularFont.Descender
            );

            var tokenString = new NSMutableAttributedString(NSAttributedString.FromAttachment(textAttachment));
            var attributes = createBasicAttributes();
            attributes.Dictionary[ProjectId] = new NSNumber(projectSpan.ProjectId);
            attributes.Dictionary[ProjectName] = new NSString(projectSpan.ProjectName);
            attributes.Dictionary[ProjectColor] = new NSString(projectSpan.ProjectColor);
            if (projectSpan.TaskId.HasValue)
            {
                attributes.Dictionary[TaskId] = new NSNumber(projectSpan.TaskId.Value);
                attributes.Dictionary[TaskName] = new NSString(projectSpan.TaskName);
            }

            tokenString.AddAttributes(attributes, new NSRange(0, tokenString.Length));

            return tokenString;
        }

        private static NSMutableAttributedString AsAttributedString(this TagSpan tagSpan)
        {
            var tagName = new NSMutableAttributedString(tagSpan.TagName.TruncatedAt(maxTextLength), tagAttributes);
            var textAttachment = new TagTextAttachment(tagName, textVerticalOffset, regularFont.Descender);
            var tokenString = new NSMutableAttributedString(NSAttributedString.FromAttachment(textAttachment));

            var attributes = createBasicAttributes();
            attributes.Dictionary[TagId] = new NSNumber(tagSpan.TagId);
            attributes.Dictionary[TagName] = new NSString(tagSpan.TagName);
            tokenString.AddAttributes(attributes, new NSRange(0, tokenString.Length));

            return tokenString;
        }

        private static UIStringAttributes createBasicAttributes()
            => new UIStringAttributes
            {
                Font = regularFont,
                ParagraphStyle = paragraphStyle
            };
    }
}
