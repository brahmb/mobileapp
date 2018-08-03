using System;
using Toggl.PrimeRadiant;

namespace Toggl.Foundation.Models
{
    internal partial class Workspace
    {
        internal sealed class Builder
        {
            public static Builder Create(long id) => new Builder(id);

            public long Id { get; }

            public string Name { get; private set; }

            public SyncStatus SyncStatus { get; private set; }

            public DateTimeOffset At { get; private set; }

            private Builder(long id)
            {
                Id = id;
            }

            public Workspace Build()
            {
                return new Workspace(this);
            }

            public Builder SetName(string name)
            {
                Name = name;
                return this;
            }

            public Builder SetSyncStatus(SyncStatus syncStatus)
            {
                SyncStatus = syncStatus;
                return this;
            }

            public Builder SetAt(DateTimeOffset at)
            {
                At = at;
                return this;
            }
        }

        private Workspace(Builder builder)
        {
            Id = builder.Id;
            Name = builder.Name;
            SyncStatus = builder.SyncStatus;
            At = builder.At;
        }
    }
}
