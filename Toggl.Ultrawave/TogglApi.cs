using System.Net.Http;
using Toggl.Multivac;
using Toggl.Ultrawave.ApiClients;
using Toggl.Ultrawave.ApiClients.Interfaces;
using Toggl.Ultrawave.Network;
using Toggl.Ultrawave.Serialization;
using static System.Net.DecompressionMethods;

namespace Toggl.Ultrawave
{
    public sealed class TogglApi : ITogglApi
    {
        public TogglApi(ApiConfiguration configuration, HttpClientHandler handler = null)
        {
            Ensure.Argument.IsNotNull(configuration, nameof(configuration));

            var httpHandler = handler ?? new HttpClientHandler { AutomaticDecompression = GZip | Deflate };
            var httpClient = new HttpClient(httpHandler);

            var userAgent = configuration.UserAgent;
            var credentials = configuration.Credentials;
            var serializer = new JsonSerializer();
            var apiClient = new ApiClient(httpClient, userAgent);
            var endpoints = new Endpoints(configuration.Environment);

            Status = new StatusApi(endpoints, apiClient);
            Tags = new TagsApi(endpoints, apiClient, serializer, credentials);
            User = new UserApi(endpoints, apiClient, serializer, credentials);
            Tasks = new TasksApi(endpoints, apiClient, serializer, credentials);
            Clients = new ClientsApi(endpoints, apiClient, serializer, credentials);
            Projects = new ProjectsApi(endpoints, apiClient, serializer, credentials);
            Location = new LocationApi(endpoints, apiClient, serializer, credentials);
            Countries = new CountriesApi(endpoints, apiClient, serializer, credentials);
            Workspaces = new WorkspacesApi(endpoints, apiClient, serializer, credentials);
            Preferences = new PreferencesApi(endpoints, apiClient, serializer, credentials);
            ProjectsSummary = new ProjectsSummaryApi(endpoints, apiClient, serializer, credentials);
            TimeEntries = new TimeEntriesApi(endpoints, apiClient, serializer, credentials, userAgent);
            WorkspaceFeatures = new WorkspaceFeaturesApi(endpoints, apiClient, serializer, credentials);
            Feedback = new FeedbackApiClient(endpoints, apiClient, serializer, credentials);
        }

        public ITagsApi Tags { get; }
        public IUserApi User { get; }
        public ITasksApi Tasks { get; }
        public IStatusApi Status { get; }
        public IClientsApi Clients { get; }
        public ILocationApi Location { get; }
        public IProjectsApi Projects { get; }
        public ICountriesApi Countries { get; }
        public IWorkspacesApi Workspaces { get; }
        public ITimeEntriesApi TimeEntries { get; }
        public IPreferencesApi Preferences { get; }
        public IProjectsSummaryApi ProjectsSummary { get; }
        public IWorkspaceFeaturesApi WorkspaceFeatures { get; }
        public IFeedbackApi Feedback { get; }
    }
}
