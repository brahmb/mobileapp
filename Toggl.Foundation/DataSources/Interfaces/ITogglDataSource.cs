using System;
using System.Reactive;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Reports;
using Toggl.Foundation.Sync;
using Toggl.PrimeRadiant.Models;
using Toggl.Ultrawave.ApiClients;

namespace Toggl.Foundation.DataSources
{
    public interface ITogglDataSource
    {
        ITagsSource Tags { get; }
        IUserSource User { get; }
        IPreferencesSource Preferences { get; }
        ITasksSource Tasks { get; }
        IClientsSource Clients { get; }
        IProjectsSource Projects { get; }
        ITimeEntriesSource TimeEntries { get; }
        IDataSource<IThreadSafeWorkspace, IDatabaseWorkspace> Workspaces { get; }
        IDataSource<IThreadSafeWorkspaceFeatureCollection, IDatabaseWorkspaceFeatureCollection> WorkspaceFeatures { get; }

        ISyncManager SyncManager { get; }

        IObservable<Unit> StartSyncing();
        IReportsProvider ReportsProvider { get; }

        IFeedbackApi FeedbackApi { get; }

        IObservable<bool> HasUnsyncedData();

        IObservable<Unit> Logout();
    }
}
