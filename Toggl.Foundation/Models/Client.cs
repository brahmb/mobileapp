using System;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Models
{
    internal class Client : IThreadSafeClient
    {
        public long Id { get; }
        public long WorkspaceId { get; }
        public string Name { get; }
        public DateTimeOffset At { get; }
        public SyncStatus SyncStatus { get; }
        public string LastSyncErrorMessage { get; }
        public bool IsDeleted { get; }
        public DateTimeOffset? ServerDeletedAt { get; }
        public IThreadSafeWorkspace Workspace { get; }
        IDatabaseWorkspace IDatabaseClient.Workspace => Workspace;

        public bool IsGhost => Workspace.IsGhost;

        private Client(IClient entity, SyncStatus syncStatus, string lastSyncErrorMessage, bool isDeleted = false, IThreadSafeWorkspace workspace = null)
            : this(entity.Id, entity.WorkspaceId, entity.Name, entity.At, syncStatus, lastSyncErrorMessage, isDeleted, entity.ServerDeletedAt, workspace)
        { }

        public Client(long id, long workspaceId, string name, DateTimeOffset at, SyncStatus syncStatus, string lastSyncErrorMessage = "", bool isDeleted = false, DateTimeOffset? serverDeletedAt = null, IThreadSafeWorkspace workspace = null)
        {
            Id = id;
            WorkspaceId = workspaceId;
            Name = name;
            At = at;
            SyncStatus = syncStatus;
            LastSyncErrorMessage = lastSyncErrorMessage;
            IsDeleted = isDeleted;
            ServerDeletedAt = serverDeletedAt;
            Workspace = workspace;
        }

        public static Client From(IDatabaseClient entity)
        {
            var workspace = entity.Workspace == null ? null : Models.Workspace.From(entity.Workspace);
            return new Client(entity, entity.SyncStatus, entity.LastSyncErrorMessage, entity.IsDeleted, workspace);
        }

        public static Client Clean(IClient entity)
            => new Client(entity, SyncStatus.InSync, null);

        public static Client Dirty(IClient entity)
            => new Client(entity, SyncStatus.SyncNeeded, null);

        public static Client Unsyncable(IClient entity, string errorMessage)
            => new Client(entity, SyncStatus.SyncFailed, errorMessage);
    }
}
