using System;
using Realms;
using Toggl.PrimeRadiant.Models;

namespace Toggl.PrimeRadiant.Realm
{
    internal partial class RealmTask : RealmObject, IDatabaseTask
    {
        public string Name { get; set; }

        public long EstimatedSeconds { get; set; }

        public bool Active { get; set; }

        public DateTimeOffset At { get; set; }

        public long TrackedSeconds { get; set; }

        public RealmWorkspace RealmWorkspace { get; set; }

        public long WorkspaceId => RealmWorkspace?.Id ?? 0;

        public IDatabaseWorkspace Workspace => RealmWorkspace;

        public RealmProject RealmProject { get; set; }

        public long ProjectId => RealmProject?.Id ?? 0;

        public IDatabaseProject Project => RealmProject;

        public RealmUser RealmUser { get; set; }

        public long? UserId => RealmUser?.Id;

        public IDatabaseUser User => RealmUser;

        public bool IsGhost => Workspace.IsGhost;
    }
}
