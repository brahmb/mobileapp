using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Toggl.Multivac.Models;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Tests.Integration.BaseTests;
using Xunit;
using TimeEntry = Toggl.Ultrawave.Models.TimeEntry;

namespace Toggl.Ultrawave.Tests.Integration
{
    public sealed class TimeEntriesApiTests
    {
        public sealed class TheGetAllMethod : AuthenticatedGetEndpointBaseTests<List<ITimeEntry>>
        {
            [Fact, LogTestInfo]
            public async Task ReturnsAllTimeEntries()
            {
                var (togglClient, user) = await SetupTestUser();
                var firstTimeEntry = createTimeEntry(user);
                var firstTimeEntryPosted = await togglClient.TimeEntries.Create(firstTimeEntry);
                var secondTimeEntry = createTimeEntry(user);
                var secondTimeEntryPosted = await togglClient.TimeEntries.Create(secondTimeEntry);

                var timeEntries = await CallEndpointWith(togglClient);

                timeEntries.Should().HaveCount(2);
                timeEntries.Should().Contain(entry =>
                    entry.Description == firstTimeEntryPosted.Description
                    && entry.WorkspaceId == user.DefaultWorkspaceId
                    && entry.Start == firstTimeEntryPosted.Start
                    && entry.UserId == user.Id);
                timeEntries.Should().Contain(entry =>
                    entry.Description == secondTimeEntryPosted.Description
                    && entry.WorkspaceId == user.DefaultWorkspaceId
                    && entry.Start == secondTimeEntryPosted.Start
                    && entry.UserId == user.Id);
            }

            [Fact, LogTestInfo]
            public async Task ReturnsNullInsteadOfAnEmptyListWhenThereIsNoTimeEntryOnTheServer()
            {
                var (togglClient, user) = await SetupTestUser();

                var timeEntries = await togglClient.TimeEntries.GetAll();

                timeEntries.Should().BeNull();
            }

            protected override IObservable<List<ITimeEntry>> CallEndpointWith(ITogglApi togglApi)
                => togglApi.TimeEntries.GetAll();
        }

        public sealed class TheGetAllSinceMethod : AuthenticatedGetSinceEndpointBaseTests<ITimeEntry>
        {
            protected override IObservable<List<ITimeEntry>> CallEndpointWith(ITogglApi togglApi, DateTimeOffset threshold)
                => togglApi.TimeEntries.GetAllSince(threshold);

            protected override DateTimeOffset AtDateOf(ITimeEntry model)
                => model.At;

            protected override ITimeEntry MakeUniqueModel(ITogglApi api, IUser user)
                => createTimeEntry(user);

            protected override IObservable<ITimeEntry> PostModelToApi(ITogglApi api, ITimeEntry model)
                => api.TimeEntries.Create(model);

            protected override Expression<Func<ITimeEntry, bool>> ModelWithSameAttributesAs(ITimeEntry model)
                => te => te.Id == model.Id && te.Description == model.Description;
        }

        public sealed class TheGetAllBetweenStartAndEndDatesMethod : AuthenticatedGetEndpointBaseTests<List<ITimeEntry>>
        {
            private static readonly DateTimeOffset start = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(75));
            private static readonly DateTimeOffset end = start.AddDays(60);

            protected override IObservable<List<ITimeEntry>> CallEndpointWith(ITogglApi togglApi)
                => togglApi.TimeEntries.GetAll(start, end);

            [Fact]
            public async Task ReturnsTimeEntriesOnlyUpToMidnightOfTheEndDate()
            {
                var endDateMidnight = midnightUTCFrom(end);
                var (api, user) = await SetupTestUser();
                var timeEntryThatWontBeReturned = createTimeEntry(user, endDateMidnight.AddSeconds(1));
                var expectedTimeEntry = createTimeEntry(user, endDateMidnight.AddMinutes(-5));
                await api.TimeEntries.Create(timeEntryThatWontBeReturned);
                await api.TimeEntries.Create(expectedTimeEntry);

                var list = await api.TimeEntries.GetAll(start, end);

                list.Should().HaveCount(1);
                var item = list[0];
                item.Start.ToUnixTimeSeconds().Should().Be(expectedTimeEntry.Start.ToUnixTimeSeconds());
            }

            [Fact]
            public async Task ReturnsTimeEntriesOnlyAfterMidnightOfTheStartDate()
            {
                var startDateMidnight = midnightUTCFrom(start);
                var (api, user) = await SetupTestUser();
                var expectedTimeEntry = createTimeEntry(user, startDateMidnight);
                var timeEntryThatWontBeReturned = createTimeEntry(user, startDateMidnight.AddMinutes(-5));
                await api.TimeEntries.Create(expectedTimeEntry);
                await api.TimeEntries.Create(timeEntryThatWontBeReturned);

                var list = await api.TimeEntries.GetAll(start, end);

                list.Should().HaveCount(1);
                var item = list[0];
                item.Start.ToUnixTimeSeconds().Should().BeGreaterOrEqualTo(expectedTimeEntry.Start.ToUnixTimeSeconds());
            }
        }

        public sealed class TheCreateMethod : AuthenticatedPostEndpointBaseTests<ITimeEntry>
        {
            [Fact, LogTestInfo]
            public async Task CreatesNewTimeEntry()
            {
                var (togglClient, user) = await SetupTestUser();
                var newTimeEntry = createTimeEntry(user);

                var persistedTimeEntry = await CallEndpointWith(togglClient, newTimeEntry);

                persistedTimeEntry.Description.Should().Be(newTimeEntry.Description);
                persistedTimeEntry.WorkspaceId.Should().Be(newTimeEntry.WorkspaceId);
                persistedTimeEntry.Billable.Should().Be(false);
                persistedTimeEntry.ProjectId.Should().BeNull();
                persistedTimeEntry.TaskId.Should().BeNull();
            }

            [Fact, LogTestInfo]
            public async Task ThrowsWhenTagIdsIsNull()
            {
                var (togglClient, user) = await SetupTestUser();
                var timeEntry = createTimeEntry(user);
                timeEntry.TagIds = null;

                Action post = () => togglClient.TimeEntries.Create(timeEntry).Wait();

                post.Should().Throw<BadRequestException>();
            }

            [Theory, LogTestInfo]
            [InlineData(0)]
            [InlineData(998)]
            [InlineData(-998)]
            public async Task CreatesRunningTimeEntryWhenTheStartIsSetToCurrentTimePlusMinusNineHundredNinetyNineHours(int hoursOffset)
            {
                var (togglApi, user) = await SetupTestUser();
                var start = DateTimeOffset.Now.AddHours(-hoursOffset);
                var timeEntry = new Ultrawave.Models.TimeEntry
                {
                    Description = Guid.NewGuid().ToString(),
                    WorkspaceId = user.DefaultWorkspaceId.Value,
                    Start = start,
                    UserId = user.Id,
                    TagIds = new List<long>(),
                    CreatedWith = "IntegrationTests/0.0"
                };

                var persistedTimeEntry = CallEndpointWith(togglApi, timeEntry).Wait();

                persistedTimeEntry.Id.Should().BePositive();
            }

            [Theory, LogTestInfo]
            [InlineData(1000)]
            [InlineData(-1000)]
            public async Task FailsCreatingARunningTimeEntryWhenTheStartTimeIsSetToTheCurrentTimePlusMinusMoreThanNineHundredNinetyNineHours(int hoursOffset)
            {
                var (togglApi, user) = await SetupTestUser();
                var start = DateTimeOffset.Now.AddHours(-hoursOffset);
                var timeEntry = new Ultrawave.Models.TimeEntry
                {
                    Description = Guid.NewGuid().ToString(),
                    WorkspaceId = user.DefaultWorkspaceId.Value,
                    Start = start,
                    UserId = user.Id,
                    TagIds = new List<long>(),
                    CreatedWith = "IntegrationTests/0.0"
                };

                Action creatingTimeEntry = () => CallEndpointWith(togglApi, timeEntry).Wait();

                creatingTimeEntry.Should().Throw<BadRequestException>();
            }

            [Fact, LogTestInfo]
            public async Task TheTimeEntryReturnedByBackendIsARunningTimeEntryWhenPostingANewRunningTimeEntry()
            {
                var (togglApi, user) = await SetupTestUser();
                var timeEntry = new Ultrawave.Models.TimeEntry
                {
                    Description = Guid.NewGuid().ToString(),
                    WorkspaceId = user.DefaultWorkspaceId.Value,
                    Start = DateTimeOffset.UtcNow,
                    UserId = user.Id,
                    TagIds = new List<long>(),
                    CreatedWith = "IntegrationTests/0.0"
                };

                var postedTimeEntry = await togglApi.TimeEntries.Create(timeEntry);

                postedTimeEntry.Duration.Should().BeNull();
            }

            [Fact, LogTestInfo]
            public async Task TheTimeEntryStoredInBackendIsARunningTimeEntryWhenFetchingItAfterPostingANewRunningTimeEntry()
            {
                var (togglApi, user) = await SetupTestUser();
                var timeEntry = new Ultrawave.Models.TimeEntry
                {
                    Description = Guid.NewGuid().ToString(),
                    WorkspaceId = user.DefaultWorkspaceId.Value,
                    Start = DateTimeOffset.UtcNow,
                    UserId = user.Id,
                    TagIds = new List<long>(),
                    CreatedWith = "IntegrationTests/0.0"
                };

                var postedTimeEntry = await togglApi.TimeEntries.Create(timeEntry);
                var fetchedTimeEntry = await togglApi.TimeEntries.GetAll()
                    .SelectMany(timeEntries => timeEntries)
                    .Where(te => te.Id == postedTimeEntry.Id)
                    .SingleAsync();

                fetchedTimeEntry.Duration.Should().BeNull();
            }

            [Fact, LogTestInfo]
            public async Task BackendStopsPreviousRunningTimeEntryWhenAnotherRunningTimeEntryIsPushed()
            {
                var (togglApi, user) = await SetupTestUser();
                var firstTimeEntry = createTimeEntry(user);
                firstTimeEntry.Duration = null;
                var secondTimeEntry = createTimeEntry(user);
                secondTimeEntry.Duration = null;

                var postedFirstTimeEntry = await togglApi.TimeEntries.Create(firstTimeEntry);
                await Task.Delay(2000);
                var postedSecondTimeEntry = await togglApi.TimeEntries.Create(secondTimeEntry);
                var stoppedFirstTimeEntry =
                    await togglApi.TimeEntries.GetAll().SelectMany(te => te).Where(te => te.Id == postedFirstTimeEntry.Id).FirstAsync();

                postedFirstTimeEntry.Duration.Should().BeNull();
                postedSecondTimeEntry.Duration.Should().BeNull();
                stoppedFirstTimeEntry.Duration.Should().NotBeNull();
                postedFirstTimeEntry.At.Should().NotBe(stoppedFirstTimeEntry.At);
                stoppedFirstTimeEntry.At.Should().Be(postedSecondTimeEntry.At);
                stoppedFirstTimeEntry.Start.AddSeconds(stoppedFirstTimeEntry.Duration.Value).Should().Be(stoppedFirstTimeEntry.At);
            }

            protected override IObservable<ITimeEntry> CallEndpointWith(ITogglApi togglApi)
                => Observable.Defer(async () =>
                {
                    var user = await togglApi.User.Get();
                    var timeEntry = createTimeEntry(user);
                    return CallEndpointWith(togglApi, timeEntry);
                });

            private IObservable<ITimeEntry> CallEndpointWith(ITogglApi togglApi, TimeEntry client)
                => togglApi.TimeEntries.Create(client);
        }

        public sealed class TheUpdateMethod : AuthenticatedPutEndpointBaseTests<ITimeEntry>
        {
            [Fact, LogTestInfo]
            public async Task UpdatesExistingTimeEntry()
            {
                var (togglClient, user) = await SetupTestUser();
                var timeEntry = createTimeEntry(user);
                var persistedTimeEntry = await togglClient.TimeEntries.Create(timeEntry);
                var timeEntryWithUpdates = new Ultrawave.Models.TimeEntry
                {
                    Id = persistedTimeEntry.Id,
                    Description = Guid.NewGuid().ToString(),
                    WorkspaceId = persistedTimeEntry.WorkspaceId,
                    Billable = persistedTimeEntry.Billable,
                    Start = persistedTimeEntry.Start,
                    Duration = persistedTimeEntry.Duration,
                    TagIds = persistedTimeEntry.TagIds,
                    UserId = persistedTimeEntry.UserId,
                };

                var updatedTimeEntry = await togglClient.TimeEntries.Update(timeEntryWithUpdates);

                updatedTimeEntry.Id.Should().Be(persistedTimeEntry.Id);
                updatedTimeEntry.Description.Should().Be(timeEntryWithUpdates.Description);
                updatedTimeEntry.WorkspaceId.Should().Be(persistedTimeEntry.WorkspaceId);
                updatedTimeEntry.Billable.Should().Be(false);
                updatedTimeEntry.ProjectId.Should().BeNull();
                updatedTimeEntry.TaskId.Should().BeNull();
            }

            [Fact, LogTestInfo]
            public async Task AddTagsToAnExistingTimeEntry()
            {
                var (togglClient, user) = await SetupTestUser();
                var timeEntry = createTimeEntry(user);
                var persistedTimeEntry = await togglClient.TimeEntries.Create(timeEntry);
                var tag = await togglClient.Tags.Create(new Models.Tag { Name = Guid.NewGuid().ToString(), WorkspaceId = user.DefaultWorkspaceId.Value });
                var timeEntryWithUpdates = new TimeEntry
                {
                    Id = persistedTimeEntry.Id,
                    Description = Guid.NewGuid().ToString(),
                    WorkspaceId = persistedTimeEntry.WorkspaceId,
                    Billable = persistedTimeEntry.Billable,
                    Start = persistedTimeEntry.Start,
                    Duration = persistedTimeEntry.Duration,
                    TagIds = new List<long> { tag.Id },
                    UserId = persistedTimeEntry.UserId,
                };

                var updatedTimeEntry = await togglClient.TimeEntries.Update(timeEntryWithUpdates);

                updatedTimeEntry.Id.Should().Be(persistedTimeEntry.Id);
                updatedTimeEntry.Description.Should().Be(timeEntryWithUpdates.Description);
                updatedTimeEntry.WorkspaceId.Should().Be(persistedTimeEntry.WorkspaceId);
                updatedTimeEntry.Billable.Should().Be(false);
                updatedTimeEntry.ProjectId.Should().BeNull();
                updatedTimeEntry.TaskId.Should().BeNull();
                updatedTimeEntry.TagIds.Count().Should().Be(1);
                updatedTimeEntry.TagIds.First().Should().Be(tag.Id);
            }

            [Fact, LogTestInfo]
            public async Task ThrowsWhenTagIdsIsNull()
            {
                var (togglClient, user) = await SetupTestUser();
                var timeEntry = createTimeEntry(user);
                var persistedTimeEntry = await togglClient.TimeEntries.Create(timeEntry);
                timeEntry.Id = persistedTimeEntry.Id;
                timeEntry.TagIds = null;

                Action put = () => togglClient.TimeEntries.Update(timeEntry).Wait();

                put.Should().Throw<BadRequestException>();
            }

            protected override IObservable<ITimeEntry> PrepareForCallingUpdateEndpoint(ITogglApi togglApi)
                => Observable.Defer(async () =>
                {
                    var user = await togglApi.User.Get();
                    var timeEntry = createTimeEntry(user);
                    return togglApi.TimeEntries.Create(timeEntry);
                });

            protected override IObservable<ITimeEntry> CallUpdateEndpoint(ITogglApi togglApi, ITimeEntry timeEntry)
            {
                var timeEntryWithUpdates = new TimeEntry
                {
                    Id = timeEntry.Id,
                    Description = Guid.NewGuid().ToString(),
                    WorkspaceId = timeEntry.WorkspaceId,
                    Billable = timeEntry.Billable,
                    Start = timeEntry.Start,
                    Duration = timeEntry.Duration,
                    TagIds = timeEntry.TagIds,
                    UserId = timeEntry.UserId,
                };

                return togglApi.TimeEntries.Update(timeEntryWithUpdates);
            }
        }

        public sealed class TheDeleteMethod : AuthenticatedDeleteEndpointBaseTests<ITimeEntry>
        {
            [Fact, LogTestInfo]
            public async Task DeletesTheTimeEntryFromTheServer()
            {
                var (togglApi, user) = await SetupTestUser();
                var timeEntry = createTimeEntry(user);
                var persistedTimeEntry = await togglApi.TimeEntries.Create(timeEntry);

                var timeEntriesOnServerBefore = await togglApi.TimeEntries.GetAll();
                await togglApi.TimeEntries.Delete(persistedTimeEntry);
                var timeEntriesOnServerAfter = await togglApi.TimeEntries.GetAll();

                timeEntriesOnServerBefore.Should().HaveCount(1);
                timeEntriesOnServerAfter.Should().BeNull();
            }

            [Fact, LogTestInfo]
            public async Task DeletesTheTimeEntryFromTheServerAndKeepsAllTheOtherTimeEntriesIntact()
            {
                var (togglApi, user) = await SetupTestUser();
                var timeEntryA = createTimeEntry(user);
                var persistedTimeEntryA = await togglApi.TimeEntries.Create(timeEntryA);
                var timeEntryB = createTimeEntry(user);
                var persistedTimeEntryB = await togglApi.TimeEntries.Create(timeEntryB);
                var timeEntryC = createTimeEntry(user);
                var persistedTimeEntryC = await togglApi.TimeEntries.Create(timeEntryC);

                await togglApi.TimeEntries.Delete(persistedTimeEntryB);
                var timeEntriesOnServer = await togglApi.TimeEntries.GetAll();

                timeEntriesOnServer.Should().HaveCount(2);
                timeEntriesOnServer.Should().Contain(te => te.Id == persistedTimeEntryA.Id).And.Contain(te => te.Id == persistedTimeEntryC.Id);
            }

            [Fact, LogTestInfo]
            public async Task FailsIfDeletingANonExistingTimeEntryInAWorkspaceWhereTheUserBelongs()
            {
                var (togglApi, user) = await SetupTestUser();
                var timeEntry = createTimeEntry(user);
                var persistedTimeEntry = await togglApi.TimeEntries.Create(timeEntry);
                var timeEntryToDelete = new TimeEntry { Id = persistedTimeEntry.Id + 1000000, WorkspaceId = persistedTimeEntry.WorkspaceId };

                Action deleteNonExistingTimeEntry = () => togglApi.TimeEntries.Delete(timeEntryToDelete).Wait();

                deleteNonExistingTimeEntry.Should().Throw<NotFoundException>();
                (await togglApi.TimeEntries.GetAll()).Should().Contain(te => te.Id == persistedTimeEntry.Id);
            }

            [Fact, LogTestInfo]
            public async Task FailsIfDeletingANonExistingTimeEntryInAWorkspaceWhereTheUserDoesNotBelong()
            {
                var (togglApi, user) = await SetupTestUser();
                var timeEntry = createTimeEntry(user);
                var persistedTimeEntry = await togglApi.TimeEntries.Create(timeEntry);
                var timeEntryToDelete = new TimeEntry { Id = persistedTimeEntry.Id + 1000000, WorkspaceId = persistedTimeEntry.WorkspaceId + 1000000 };

                Action deleteNonExistingTimeEntry = () => togglApi.TimeEntries.Delete(timeEntryToDelete).Wait();

                deleteNonExistingTimeEntry.Should().Throw<ForbiddenException>();
                (await togglApi.TimeEntries.GetAll()).Should().Contain(te => te.Id == persistedTimeEntry.Id);
            }

            [Fact, LogTestInfo]
            public async Task FailsIfDeletingAnExistingTimeEntryInAWorkspaceWhereTheUserDoesNotBelong()
            {
                var (togglApi, user) = await SetupTestUser();
                var timeEntry = createTimeEntry(user);
                var persistedTimeEntry = await togglApi.TimeEntries.Create(timeEntry);
                var timeEntryToDelete = new TimeEntry { Id = persistedTimeEntry.Id, WorkspaceId = persistedTimeEntry.WorkspaceId + 1000000 };

                Action deleteNonExistingTimeEntry = () => togglApi.TimeEntries.Delete(timeEntryToDelete).Wait();

                deleteNonExistingTimeEntry.Should().Throw<ForbiddenException>();
                (await togglApi.TimeEntries.GetAll()).Should().Contain(te => te.Id == persistedTimeEntry.Id);
            }

            [Fact, LogTestInfo]
            public async Task FailsIfDeletingAnAlreadyDeletedTimeEntry()
            {
                var (togglApi, user) = await SetupTestUser();
                var timeEntry = createTimeEntry(user);
                var persistedTimeEntry = await togglApi.TimeEntries.Create(timeEntry);

                await togglApi.TimeEntries.Delete(persistedTimeEntry);
                Action secondDelete = () => togglApi.TimeEntries.Delete(persistedTimeEntry).Wait();

                secondDelete.Should().Throw<NotFoundException>();
            }

            [Fact, LogTestInfo]
            public async Task FailsIfDeletingOtherUsersTimeEntry()
            {
                var (togglApiA, userA) = await SetupTestUser();
                var (togglApiB, userB) = await SetupTestUser();
                var timeEntry = createTimeEntry(userA);
                var persistedTimeEntry = await togglApiA.TimeEntries.Create(timeEntry);

                Action secondDelete = () => togglApiB.TimeEntries.Delete(persistedTimeEntry).Wait();

                secondDelete.Should().Throw<ForbiddenException>();
            }

            protected override IObservable<ITimeEntry> Initialize(ITogglApi togglApi)
                => Observable.Defer(async () =>
                {
                    var user = await togglApi.User.Get();
                    var timeEntry = createTimeEntry(user);
                    return togglApi.TimeEntries.Create(timeEntry);
                });

            protected override IObservable<Unit> Delete(ITogglApi togglApi, ITimeEntry timeEntry)
                => togglApi.TimeEntries.Delete(timeEntry);
        }

        private static TimeEntry createTimeEntry(IUser user, DateTimeOffset? start = null)
            => new TimeEntry
            {
                WorkspaceId = user.DefaultWorkspaceId.Value,
                Billable = false,
                Start = start ?? new DateTimeOffset(DateTime.Now - TimeSpan.FromMinutes(5)),
                Duration = (long)TimeSpan.FromMinutes(5).TotalSeconds,
                Description = Guid.NewGuid().ToString(),
                TagIds = new List<long>(),
                UserId = user.Id,
                CreatedWith = "Ultraware Integration Tests"
            };

        private static DateTimeOffset midnightUTCFrom(DateTimeOffset dateTimeOffset)
        {
            return new DateTimeOffset(dateTimeOffset.Year, dateTimeOffset.Month, dateTimeOffset.Day, 0, 0, 0, 0, TimeSpan.Zero);
        }
    }
}
