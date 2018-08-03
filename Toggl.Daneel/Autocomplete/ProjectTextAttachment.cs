using System;
using CoreGraphics;
using Foundation;
using UIKit;

namespace Toggl.Daneel.Autocomplete
{
    public sealed class ProjectTextAttachment : TokenTextAttachment
    {
        private const int dotPadding = 6;
        private const int dotDiameter = 4;
        private const int dotRadius = dotDiameter / 2;
        private const int circleWidth = dotDiameter + dotPadding;
        private const int dotYOffset = (LineHeight / 2) - dotRadius;

        public ProjectTextAttachment(NSAttributedString stringToDraw, UIColor projectColor, nfloat textVerticalOffset, nfloat fontDescender)
            : base(fontDescender)
        {
            var size = CalculateSize(stringToDraw, circleWidth);

            UIGraphics.BeginImageContextWithOptions(size, false, 0.0f);
            using (var context = UIGraphics.GetCurrentContext())
            {
                var tokenPath = CalculateTokenPath(size);
                context.AddPath(tokenPath.CGPath);
                context.SetFillColor(projectColor.ColorWithAlpha(0.12f).CGColor);
                context.FillPath();

                var dot = UIBezierPath.FromRoundedRect(new CGRect(
                    x: dotPadding + TokenMargin,
                    y: dotYOffset,
                    width: dotDiameter,
                    height: dotDiameter
                ), dotRadius);
                context.AddPath(dot.CGPath);
                context.SetFillColor(projectColor.CGColor);
                context.FillPath();

                stringToDraw.DrawString(new CGPoint(circleWidth + TokenMargin + TokenPadding, textVerticalOffset));

                var image = UIGraphics.GetImageFromCurrentImageContext();
                UIGraphics.EndImageContext();
                Image = image;
            }
        }
    }
}
