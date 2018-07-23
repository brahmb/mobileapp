using System;
using System.Linq;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using NSubstitute;
using Toggl.Foundation.Models;
using Toggl.Foundation.Sync.ConflictResolution;
using Toggl.Foundation.Tests.Mocks;
using Toggl.PrimeRadiant;
using Xunit;

namespace Toggl.Foundation.Tests.Sync.ConflictResolution
{
    public sealed class TimeEntryRivalsResolverTests
    {
        private readonly TimeEntryRivalsResolver resolver;

        private readonly ITimeService timeService;

        private static readonly DateTimeOffset arbitraryTime = new DateTimeOffset(2017, 9, 1, 12, 0, 0, TimeSpan.Zero);

        private readonly IQueryable<TimeEntry> timeEntries;

        public TimeEntryRivalsResolverTests()
        {
            timeService = Substitute.For<ITimeService>();
            resolver = new TimeEntryRivalsResolver(timeService);
            timeEntries = new EnumerableQuery<TimeEntry>(new[]
            {
                createTimeEntry(id: 10, start: arbitraryTime, duration: (long)TimeSpan.FromHours(2).TotalSeconds),
                createTimeEntry(id: 11, start: arbitraryTime.AddDays(5), duration: (long)TimeSpan.FromDays(1).TotalSeconds),
                createTimeEntry(id: 12, start: arbitraryTime.AddDays(10), duration: (long)TimeSpan.FromHours(1).TotalSeconds),
                createTimeEntry(id: 13, start: arbitraryTime.AddDays(15), duration: (long)TimeSpan.FromSeconds(13).TotalSeconds)
            });
        }

        [Fact, LogIfTooSlow]
        public void TimeEntryWhichHasDurationSetToNullCanHaveRivals()
        {
            var a = createTimeEntry(id: 1, duration: null);

            var canHaveRival = resolver.CanHaveRival(a);

            canHaveRival.Should().BeTrue();
        }

        [Property]
        public void TimeEntryWhichHasDurationSetToAnythingElseThanNullCannotHaveRivals(long duration)
        {
            var a = createTimeEntry(id: 1, duration: duration);

            var canHaveRival = resolver.CanHaveRival(a);

            canHaveRival.Should().BeFalse();
        }

        [Fact, LogIfTooSlow]
        public void TwoTimeEntriesAreRivalsIfBothOfThemHaveTheDurationSetToNull()
        {
            var a = createTimeEntry(id: 1, duration: null);
            var b = createTimeEntry(id: 2, duration: null);

            var areRivals = resolver.AreRivals(a).Compile()(b);

            areRivals.Should().BeTrue();
        }

        [Property]
        public void TwoTimeEntriesAreNotRivalsIfTheLatterOneHasTheDurationNotSetToNull(NonNegativeInt b)
        {
            var x = createTimeEntry(id: 1, duration: null);
            var y = createTimeEntry(id: 2, duration: b.Get);
            var areRivals = resolver.AreRivals(x).Compile()(y);

            areRivals.Should().BeFalse();
        }

        [Property]
        public void TheTimeEntryWhichHasBeenEditedTheLastWillBeRunningAndTheOtherWillBeStoppedAfterResolution(DateTimeOffset startA, DateTimeOffset startB, DateTimeOffset firstAt, DateTimeOffset secondAt)
        {
            (DateTimeOffset earlier, DateTimeOffset later) =
                firstAt < secondAt ? (firstAt, secondAt) : (secondAt, firstAt);
            var a = createTimeEntry(id: 1, start: startA, duration: null, at: earlier);
            var b = createTimeEntry(id: 2, start: startB, duration: null, at: later);
            DateTimeOffset now = (startA > startB ? startA : startB).AddHours(5);
            timeService.CurrentDateTime.Returns(now);

            var (fixedEntityA, fixedRivalB) = resolver.FixRivals(a, b, timeEntries);
            var (fixedEntityB, fixedRivalA) = resolver.FixRivals(b, a, timeEntries);

            fixedEntityA.Duration.Should().NotBeNull();
            fixedRivalA.Duration.Should().NotBeNull();
            fixedRivalB.Duration.Should().BeNull();
            fixedEntityB.Duration.Should().BeNull();
        }

        [Fact, LogIfTooSlow]
        public void TheStoppedTimeEntryMustBeMarkedAsSyncNeededAndTheStatusOfTheOtherOneShouldNotChange()
        {
            var a = createTimeEntry(id: 1, duration: null, at: arbitraryTime.AddDays(10));
            var b = createTimeEntry(id: 2, duration: null, at: arbitraryTime.AddDays(11));

            var (fixedA, fixedB) = resolver.FixRivals(a, b, timeEntries);

            fixedA.SyncStatus.Should().Be(SyncStatus.SyncNeeded);
            fixedB.SyncStatus.Should().Be(SyncStatus.InSync);
        }

        [Fact, LogIfTooSlow]
        public void TheStoppedEntityMustHaveTheStopTimeEqualToTheStartTimeOfTheNextEntryInTheDatabase()
        {
            var a = createTimeEntry(id: 1, duration: null, at: arbitraryTime.AddDays(10), start: arbitraryTime.AddDays(12));
            var b = createTimeEntry(id: 2, duration: null, at: arbitraryTime.AddDays(11), start: arbitraryTime.AddDays(13));

            var (fixedA, _) = resolver.FixRivals(a, b, timeEntries);

            fixedA.Duration.Should().Be((long)TimeSpan.FromDays(3).TotalSeconds);
        }

        [Fact, LogIfTooSlow]
        public void TheStoppedEntityMustHaveTheStopTimeEqualToTheCurrentDateTimeOfTheTimeServiceWhenThereIsNoNextEntryInTheDatabase()
        {
            var now = arbitraryTime.AddDays(25);
            timeService.CurrentDateTime.Returns(now);
            var a = createTimeEntry(id: 1, duration: null, at: arbitraryTime.AddDays(21), start: arbitraryTime.AddDays(20));
            var b = createTimeEntry(id: 2, duration: null, at: arbitraryTime.AddDays(22), start: arbitraryTime.AddDays(21));

            var (fixedA, _) = resolver.FixRivals(a, b, timeEntries);

            fixedA.Duration.Should().Be((long)TimeSpan.FromDays(5).TotalSeconds);
        }

        [Fact, LogIfTooSlow]
        public void TheStoppedEntityMustHaveSyncStatusSetToSyncNeeded()
        {
            var now = arbitraryTime.AddDays(25);
            timeService.CurrentDateTime.Returns(now);
            var a = createTimeEntry(id: 1, duration: null, at: arbitraryTime.AddDays(21), start: arbitraryTime.AddDays(20));
            var b = createTimeEntry(id: 2, duration: null, at: arbitraryTime.AddDays(22), start: arbitraryTime.AddDays(21));

            var (fixedA, _) = resolver.FixRivals(a, b, timeEntries);

            fixedA.SyncStatus.Should().Be(SyncStatus.SyncNeeded);
        }

        [Fact, LogIfTooSlow]
        public void TheStoppedEntityMustHaveAtPropertySetToCurrentTime()
        {
            var now = arbitraryTime.AddDays(25);
            timeService.CurrentDateTime.Returns(now);
            var a = createTimeEntry(id: 1, at: arbitraryTime.AddDays(21), start: arbitraryTime.AddDays(20));
            var b = createTimeEntry(id: 2, at: arbitraryTime.AddDays(22), start: arbitraryTime.AddDays(21));

            var (fixedA, _) = resolver.FixRivals(a, b, timeEntries);

            fixedA.At.Should().Be(now);
        }

        private TimeEntry createTimeEntry(long id, DateTimeOffset? start = null, DateTimeOffset? at = null, long? duration = null)
            => TimeEntry.Clean(new MockTimeEntry
            {
                Id = id,
                Duration = duration,
                At = at ?? arbitraryTime,
                Start = start ?? arbitraryTime,
                Description = "Blah",
                WorkspaceId = 1,
                UserId = 2
            });
    }
}
