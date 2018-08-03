using Toggl.Ultrawave.ApiClients;
using Toggl.Ultrawave.ApiClients.Interfaces;

namespace Toggl.Ultrawave
{
    public interface ITogglApi
    {
        ITagsApi Tags { get; }
        IUserApi User { get; }
        ITasksApi Tasks { get; }
        IStatusApi Status { get; }
        IClientsApi Clients { get; }
        ILocationApi Location { get; }
        IProjectsApi Projects { get; }
        ICountriesApi Countries { get; }
        IWorkspacesApi Workspaces { get; }
        IPreferencesApi Preferences { get; }
        ITimeEntriesApi TimeEntries { get; }
        IProjectsSummaryApi ProjectsSummary { get; }
        IWorkspaceFeaturesApi WorkspaceFeatures { get; }
        IFeedbackApi Feedback { get; }
    }
}
