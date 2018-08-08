using System;
using System.Collections.Generic;
using Toggl.Foundation.Models.Interfaces;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Tests.Mocks
{
    public sealed class MockTimeEntry : IThreadSafeTimeEntry
    {
        private IThreadSafeTimeEntry entity;

        public MockTimeEntry() { }

        public MockTimeEntry(IThreadSafeTimeEntry entity)
        {
            Id = entity.Id;
            WorkspaceId = entity.WorkspaceId;
            ProjectId = entity.ProjectId;
            TaskId = entity.TaskId;
            Billable = entity.Billable;
            Start = entity.Start;
            Duration = entity.Duration;
            Description = entity.Description;
            TagIds = entity.TagIds;
            At = entity.At;
            ServerDeletedAt = entity.ServerDeletedAt;
            UserId = entity.UserId;
        }

        IDatabaseTask IDatabaseTimeEntry.Task => Task;

        IDatabaseUser IDatabaseTimeEntry.User => User;

        IDatabaseProject IDatabaseTimeEntry.Project => Project;

        IDatabaseWorkspace IDatabaseTimeEntry.Workspace => Workspace;

        IEnumerable<IDatabaseTag> IDatabaseTimeEntry.Tags => Tags;

        public long WorkspaceId { get; set; }

        public long? ProjectId { get; set; }

        public long? TaskId { get; set; }

        public bool Billable { get; set; }

        public DateTimeOffset Start { get; set; }

        public long? Duration { get; set; }

        public string Description { get; set; }

        public IEnumerable<long> TagIds { get; set; }

        public DateTimeOffset At { get; set; }

        public DateTimeOffset? ServerDeletedAt { get; set; }

        public long UserId { get; set; }

        public long Id { get; set; }

        public SyncStatus SyncStatus { get; set; }

        public string LastSyncErrorMessage { get; set; }

        public bool IsDeleted { get; set; }

        public IThreadSafeTask Task { get; }

        public IThreadSafeUser User { get; }

        public IThreadSafeProject Project { get; }

        public IThreadSafeWorkspace Workspace { get; }

        public IEnumerable<IThreadSafeTag> Tags { get; }

        public bool IsGhost => Workspace.IsGhost;
    }
}
