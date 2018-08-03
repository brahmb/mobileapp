using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Foundation;
using MvvmCross.Binding.Bindings.SourceSteps;
using MvvmCross.Binding.Combiners;
using MvvmCross.UI;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Daneel.Extensions;
using Toggl.Multivac;
using UIKit;

namespace Toggl.Daneel.Transformations
{
    public class ProjectTaskClientToAttributedString
    {
        private const string projectDotImageResource = "icProjectDot";
        private readonly UIColor clientColor;
        private readonly bool shouldColorProject;
        private readonly nfloat fontHeight;

        public ProjectTaskClientToAttributedString(nfloat fontHeight, UIColor clientColor, bool shouldColorProject)
        {
            Ensure.Argument.IsNotNull(clientColor, nameof(clientColor));

            this.clientColor = clientColor;
            this.shouldColorProject = shouldColorProject;
            this.fontHeight = fontHeight;
        }

        public NSAttributedString Convert(string project, string task, string client, UIColor color)
        {
            var dotString = projectDotImageResource.GetAttachmentString(fontHeight);
            var clone = new NSMutableAttributedString(dotString);
            var dottedString = tryAddColorToDot(clone, color);
            var projectInfo = buildString(project, task, client, color);
            dottedString.Append(projectInfo);

            return dottedString;
        }

        private static NSMutableAttributedString tryAddColorToDot(NSMutableAttributedString dotString, UIColor color)
        {
            if (color == null) return dotString;

            var range = new NSRange(0, 1);
            var attributes = new UIStringAttributes { ForegroundColor = color };
            dotString.AddAttributes(attributes, range);

            return dotString;
        }

        private NSAttributedString buildString(string project, string task, string client, UIColor color)
        {
            var builder = new StringBuilder();
            var hasClient = !string.IsNullOrEmpty(client);

            if (!string.IsNullOrEmpty(project))
                builder.Append($" {project}");

            if (!string.IsNullOrEmpty(task))
                builder.Append($": {task}");

            if (hasClient)
                builder.Append($" {client}");

            var text = builder.ToString();

            var result = new NSMutableAttributedString(text);
            var clientIndex = text.Length - client.Length;
            if (shouldColorProject)
            {
                var projectNameRange = new NSRange(0, clientIndex);
                var projectNameAttributes = new UIStringAttributes { ForegroundColor = color };
                result.AddAttributes(projectNameAttributes, projectNameRange);
            }

            if (!hasClient) return result;

            var clientNameRange = new NSRange(clientIndex, client.Length);
            var clientNameAttributes = new UIStringAttributes { ForegroundColor = clientColor };
            result.AddAttributes(clientNameAttributes, clientNameRange);

            return result;
        }
    }
}
