using System;
using CoreGraphics;
using Foundation;
using UIKit;

namespace Toggl.Daneel.Autocomplete
{
    public abstract class TokenTextAttachment : NSTextAttachment
    {
        public const int TokenMargin = 3;

        protected const int LineHeight = 24;
        protected const int TokenPadding = 6;

        private const int tokenHeight = 22;
        private const float tokenCornerRadius = 6.0f;
        private const int tokenVerticallOffset = (LineHeight - tokenHeight) / 2;

        private readonly nfloat fontDescender;

        protected TokenTextAttachment(nfloat fontDescender)
        {
            this.fontDescender = fontDescender;
        }

        public override CGRect GetAttachmentBounds(NSTextContainer textContainer,
            CGRect proposedLineFragment, CGPoint glyphPosition, nuint characterIndex)
        {
            var rect = base.GetAttachmentBounds(textContainer,
                proposedLineFragment, glyphPosition, characterIndex);

            rect.Y = fontDescender;
            return rect;
        }

        protected CGSize CalculateSize(NSAttributedString stringToDraw, int extraElementsWidth = 0)
            => new CGSize(
                stringToDraw.Size.Width + (TokenMargin * 2) + (TokenPadding * 2) + extraElementsWidth,
                LineHeight
            );

        protected UIBezierPath CalculateTokenPath(CGSize size)
            => UIBezierPath.FromRoundedRect(new CGRect(
                    x: TokenMargin,
                    y: tokenVerticallOffset,
                    width: size.Width - (TokenMargin * 2),
                    height: tokenHeight
               ), tokenCornerRadius);
    }
}
