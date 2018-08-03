using System;
using System.Collections.Generic;
using System.Linq;
using Realms;
using Toggl.PrimeRadiant.Models;

namespace Toggl.PrimeRadiant.Realm
{
    internal partial class RealmProject : RealmObject, IDatabaseProject
    {
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

        public RealmWorkspace RealmWorkspace { get; set; }

        public long WorkspaceId => RealmWorkspace?.Id ?? 0;

        public IDatabaseWorkspace Workspace => RealmWorkspace;

        public RealmClient RealmClient { get; set; }

        public long? ClientId => RealmClient?.Id;

        public IDatabaseClient Client => RealmClient;

        [Backlink(nameof(RealmTask.RealmProject))]
        public IQueryable<RealmTask> RealmTasks { get; }

        public IEnumerable<IDatabaseTask> Tasks => RealmTasks;

        public bool IsGhost => Workspace.IsGhost;
    }
}
