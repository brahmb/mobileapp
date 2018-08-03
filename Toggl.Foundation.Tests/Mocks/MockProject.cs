using System;
using System.Collections.Generic;
using Toggl.Foundation.Models.Interfaces;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Tests.Mocks
{
    public sealed class MockProject : IThreadSafeProject
    {
        IDatabaseClient IDatabaseProject.Client => Client;

        IDatabaseWorkspace IDatabaseProject.Workspace => Workspace;

        IEnumerable<IDatabaseTask> IDatabaseProject.Tasks => Tasks;

        public long WorkspaceId { get; set; }

        public long? ClientId { get; set; }

        public string Name { get; set; }

        public bool IsPrivate { get; set; }

        public bool Active { get; set; }

        public DateTimeOffset At { get; set; }

        public DateTimeOffset? ServerDeletedAt { get; set; }

        public string Color { get; set; }

        public bool? Billable { get; set; }

        public bool? Template { get; set; }

        public bool? AutoEstimates { get; set; }

        public long? EstimatedHours { get; set; }

        public double? Rate { get; set; }

        public string Currency { get; set; }

        public int? ActualHours { get; set; }

        public long Id { get; set; }

        public SyncStatus SyncStatus { get; set; }

        public string LastSyncErrorMessage { get; set; }

        public bool IsDeleted { get; set; }

        public IThreadSafeClient Client { get; set; }

        public IThreadSafeWorkspace Workspace { get; set; }

        public IEnumerable<IThreadSafeTask> Tasks { get; set; }

        public bool IsGhost => Workspace.IsGhost;
    }
}
