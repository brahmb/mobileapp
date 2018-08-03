using System.Collections.Generic;
using System.Linq;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac;

namespace Toggl.Foundation.Autocomplete.Suggestions
{
    public sealed class TimeEntrySuggestion : AutocompleteSuggestion
    {
        public static IEnumerable<TimeEntrySuggestion> FromTimeEntries(
            IEnumerable<IThreadSafeTimeEntry> timeEntries
        ) => timeEntries.Select(te => new TimeEntrySuggestion(te));

        public string Description { get; } = "";

        public bool HasProject { get; } = false;

        public long? ProjectId { get; }

        public string ProjectName { get; } = "";

        public string ProjectColor { get; } = "";

        public long? TaskId { get; }

        public string TaskName { get; } = "";

        public string ClientName { get; } = "";

        public TimeEntrySuggestion(IThreadSafeTimeEntry timeEntry)
        {
            Description = timeEntry.Description;
            WorkspaceId = timeEntry.WorkspaceId;
            WorkspaceName = timeEntry.Workspace?.Name ?? "";

            if (timeEntry.Project == null) return;
            HasProject = true;
            ProjectId = timeEntry.Project.Id;
            ProjectName = timeEntry.Project.DisplayName();
            ProjectColor = timeEntry.Project.DisplayColor();

            if (timeEntry.Project.Client != null)
                ClientName = timeEntry.Project.Client.Name;

            if (timeEntry.Task != null)
            {
                TaskId = timeEntry.Task.Id;
                TaskName = timeEntry.Task.Name;
            }
        }

        public override int GetHashCode() 
            => HashCode.From(Description, ProjectName, ProjectColor, ClientName);
    }
}
