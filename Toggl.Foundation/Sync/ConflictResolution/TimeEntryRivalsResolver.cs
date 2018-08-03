using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Toggl.Foundation.Models;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Sync.ConflictResolution
{
    internal sealed class TimeEntryRivalsResolver : IRivalsResolver<IDatabaseTimeEntry>
    {
        private ITimeService timeService;

        public TimeEntryRivalsResolver(ITimeService timeService)
        {
            this.timeService = timeService;
        }

        public bool CanHaveRival(IDatabaseTimeEntry entity) => entity.IsRunning();

        public Expression<Func<IDatabaseTimeEntry, bool>> AreRivals(IDatabaseTimeEntry entity)
        {
            if (!CanHaveRival(entity))
                throw new InvalidOperationException("The entity cannot have any rivals.");

            return potentialRival => potentialRival.Duration == null && potentialRival.Id != entity.Id;
        }

        public (IDatabaseTimeEntry FixedEntity, IDatabaseTimeEntry FixedRival) FixRivals<TDatabaseObject>(
            IDatabaseTimeEntry entity, IDatabaseTimeEntry rival, IQueryable<TDatabaseObject> allTimeEntries)
            where TDatabaseObject : IDatabaseTimeEntry
            => rival.At < entity.At ? (entity, stop(rival, allTimeEntries)) : (stop(entity, allTimeEntries), rival);

        private IDatabaseTimeEntry stop<TDatabaseObject>(IDatabaseTimeEntry toBeStopped, IQueryable<TDatabaseObject> allTimeEntries)
            where TDatabaseObject : IDatabaseTimeEntry
        {
            var timeEntriesStartingAfter = (IEnumerable<IDatabaseTimeEntry>)allTimeEntries
                .Where(startsAfter<TDatabaseObject>(toBeStopped.Start));
            var stopTime = timeEntriesStartingAfter
                .Select(te => te.Start)
                .Where(start => start != default(DateTimeOffset))
                .DefaultIfEmpty(timeService.CurrentDateTime)
                .Min();
            var duration = (long)(stopTime - toBeStopped.Start).TotalSeconds; // truncates towards zero (floor)

            return TimeEntry.Builder.Create(toBeStopped.Id)
                .SetDescription(toBeStopped.Description)
                .SetDuration(duration)
                .SetTagIds(toBeStopped.TagIds)
                .SetStart(toBeStopped.Start)
                .SetTaskId(toBeStopped.TaskId)
                .SetBillable(toBeStopped.Billable)
                .SetProjectId(toBeStopped.ProjectId)
                .SetWorkspaceId(toBeStopped.WorkspaceId)
                .SetUserId(toBeStopped.UserId)
                .SetIsDeleted(toBeStopped.IsDeleted)
                .SetServerDeletedAt(toBeStopped.ServerDeletedAt)
                .SetSyncStatus(SyncStatus.SyncNeeded)
                .SetAt(timeService.CurrentDateTime)
                .Build();
        }

        private Expression<Func<TDatabaseObject, bool>> startsAfter<TDatabaseObject>(DateTimeOffset start)
            where TDatabaseObject : IDatabaseTimeEntry
        {
            var other = Expression.Parameter(typeof(TDatabaseObject), "other");
            var otherStart = Expression.Property(other, nameof(ITimeEntry.Start));
            var startDate = Expression.Constant(start, typeof(DateTimeOffset));
            var expression = Expression.GreaterThan(otherStart, startDate);
            return Expression.Lambda<Func<TDatabaseObject, bool>>(expression, new[] { other });
        }
    }
}
