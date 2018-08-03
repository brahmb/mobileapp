using System;
using Toggl.Ultrawave.Helpers;
using ReportsEndpoints = Toggl.Ultrawave.Network.Reports.Endpoints;

namespace Toggl.Ultrawave.Network
{
    internal sealed class Endpoints
    {
        private readonly Uri baseUrl;

        public UserEndpoints User => new UserEndpoints(baseUrl);
        public WorkspaceEndpoints Workspaces => new WorkspaceEndpoints(baseUrl);
        public ClientEndpoints Clients => new ClientEndpoints(baseUrl);
        public ProjectEndpoints Projects => new ProjectEndpoints(baseUrl);
        public TaskEndpoints Tasks => new TaskEndpoints(baseUrl);
        public TimeEntryEndpoints TimeEntries => new TimeEntryEndpoints(baseUrl);
        public TagEndpoints Tags => new TagEndpoints(baseUrl);
        public StatusEndpoints Status => new StatusEndpoints(baseUrl);
        public WorkspaceFeaturesEndpoints WorkspaceFeatures => new WorkspaceFeaturesEndpoints(baseUrl);
        public Endpoint LoggedIn => Endpoint.Get(baseUrl, "me/logged");
        public PreferencesEndpoints Preferences => new PreferencesEndpoints(baseUrl);
        public CountryEndpoints Countries => new CountryEndpoints(baseUrl);
        public LocationEndpoints Location => new LocationEndpoints(baseUrl);
        public FeedbackEndpoints Feedback => new FeedbackEndpoints(baseUrl);

        public ReportsEndpoints ReportsEndpoints { get; }

        public Endpoints(ApiEnvironment environment)
        {
            baseUrl = BaseUrls.ForApi(environment);
            ReportsEndpoints = new ReportsEndpoints(environment);
        }
    }
}
