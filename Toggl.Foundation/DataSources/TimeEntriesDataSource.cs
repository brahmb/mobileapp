﻿using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Linq;
using Toggl.Foundation.Models;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Toggl.Foundation.DTOs;
using Toggl.Foundation.Exceptions;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Sync.ConflictResolution;
using System.Reactive.Subjects;
using Toggl.Foundation.Analytics;

namespace Toggl.Foundation.DataSources
{
    internal sealed class TimeEntriesDataSource
        : ObservableDataSource<IThreadSafeTimeEntry, IDatabaseTimeEntry, TimeEntryDto>,
          ITimeEntriesSource
    {
        private long? currentlyRunningTimeEntryId;

        private readonly ITimeService timeService;
        private readonly Func<IDatabaseTimeEntry, TimeEntryDto, ConflictResolutionMode> alwaysCreate
            = (a, b) => ConflictResolutionMode.Create;

        private readonly Subject<IThreadSafeTimeEntry> timeEntryStartedSubject = new Subject<IThreadSafeTimeEntry>();
        private readonly Subject<IThreadSafeTimeEntry> timeEntryStoppedSubject = new Subject<IThreadSafeTimeEntry>();
        private readonly Subject<IThreadSafeTimeEntry> timeEntryContinuedSubject = new Subject<IThreadSafeTimeEntry>();
        private readonly Subject<IThreadSafeTimeEntry> suggestionStartedSubject = new Subject<IThreadSafeTimeEntry>();

        public IObservable<IThreadSafeTimeEntry> TimeEntryStarted { get; }
        public IObservable<IThreadSafeTimeEntry> TimeEntryStopped { get; }
        public IObservable<IThreadSafeTimeEntry> TimeEntryContinued { get; }
        public IObservable<IThreadSafeTimeEntry> SuggestionStarted { get; }

        public IObservable<bool> IsEmpty { get; }

        public IObservable<IThreadSafeTimeEntry> CurrentlyRunningTimeEntry { get; }

        protected override IRivalsResolver<IDatabaseTimeEntry, TimeEntryDto> RivalsResolver { get; }

        public TimeEntriesDataSource(
            IRepository<IDatabaseTimeEntry, TimeEntryDto> repository,
            ITimeService timeService)
            : base(repository)
        {
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));

            this.timeService = timeService;

            CurrentlyRunningTimeEntry =
                GetAll(te => te.IsDeleted == false && te.Duration == null)
                    .Select(tes => tes.SingleOrDefault())
                    .StartWith()
                    .Merge(Created.Where(te => te.IsRunning()))
                    .Merge(Updated.Where(update => update.Id == currentlyRunningTimeEntryId).Select(update => update.Entity))
                    .Merge(Deleted.Where(id => id == currentlyRunningTimeEntryId).Select(_ => null as IThreadSafeTimeEntry))
                    .Select(runningTimeEntry)
                    .ConnectedReplay();

            IsEmpty =
                Observable.Return(default(IDatabaseTimeEntry))
                    .StartWith()
                    .Merge(Updated.Select(tuple => tuple.Entity))
                    .Merge(Created)
                    .SelectMany(_ => GetAll(te => te.IsDeleted == false))
                    .Select(timeEntries => timeEntries.None());

            RivalsResolver = new TimeEntryRivalsResolver(timeService);

            TimeEntryStarted = timeEntryStartedSubject.AsObservable();
            TimeEntryStopped = timeEntryStoppedSubject.AsObservable();
            SuggestionStarted = suggestionStartedSubject.AsObservable();
            TimeEntryContinued = timeEntryContinuedSubject.AsObservable();
        }

        public override IObservable<IThreadSafeTimeEntry> Create(TimeEntryDto entity)
            => Repository.UpdateWithConflictResolution(entity.Id, entity, alwaysCreate, RivalsResolver)
                .OfType<CreateResult<IDatabaseTimeEntry>>()
                .Select(result => result.Entity)
                .Select(Convert)
                .Do(CreatedSubject.OnNext);

        public IObservable<IThreadSafeTimeEntry> Stop(DateTimeOffset stopTime)
            => GetAll(te => te.IsDeleted == false && te.Duration == null)
                .Select(timeEntries => timeEntries.SingleOrDefault() ?? throw new NoRunningTimeEntryException())
                .SelectMany(timeEntry => timeEntry
                    .With((long)(stopTime - timeEntry.Start).TotalSeconds)
                    .Apply(TimeEntryDto.Dirty)
                    .Apply(Update))
                .Do(timeEntryStoppedSubject.OnNext);

        public IObservable<IThreadSafeTimeEntry> Update(EditTimeEntryDto dto)
            => GetById(dto.Id)
                .Select(timeEntry =>
                    TimeEntryDto.From<IThreadSafeTimeEntry>(
                        timeEntry,
                        description: dto.Description,
                        duration: dto.StopTime.HasValue ? (long?)(dto.StopTime.Value - dto.StartTime).TotalSeconds : null,
                        tagIds: New<IEnumerable<long>>.Value(dto.TagIds),
                        start: dto.StartTime,
                        taskId: dto.TaskId,
                        billable: dto.Billable,
                        projectId: dto.ProjectId,
                        workspaceId: dto.WorkspaceId,
                        userId: timeEntry.UserId,
                        isDeleted: timeEntry.IsDeleted,
                        serverDeletedAt: timeEntry.ServerDeletedAt,
                        at: timeService.CurrentDateTime,
                        syncStatus: SyncStatus.SyncNeeded))
                .SelectMany(Update);

        public IObservable<Unit> SoftDelete(IThreadSafeTimeEntry timeEntry)
            => Observable.Return(timeEntry)
                .Select(TimeEntryDto.DirtyDeleted)
                .SelectMany(Repository.Update)
                .Do(entity => DeletedSubject.OnNext(entity.Id))
                .Select(_ => Unit.Default);

        public void OnTimeEntryStarted(IThreadSafeTimeEntry timeEntry, TimeEntryStartOrigin origin)
        {
            switch (origin)
            {
                case TimeEntryStartOrigin.Continue:
                case TimeEntryStartOrigin.ContinueMostRecent:
                    timeEntryContinuedSubject.OnNext(timeEntry);
                    break;

                case TimeEntryStartOrigin.Manual:
                case TimeEntryStartOrigin.Timer:
                    timeEntryStartedSubject.OnNext(timeEntry);
                    break;

                case TimeEntryStartOrigin.Suggestion:
                    suggestionStartedSubject.OnNext(timeEntry);
                    break;
            }
        }

        protected override IThreadSafeTimeEntry Convert(IDatabaseTimeEntry entity)
            => TimeEntry.From(entity);

        protected override ConflictResolutionMode ResolveConflicts(IDatabaseTimeEntry first, TimeEntryDto second)
            => Resolver.ForTimeEntries.Resolve(first, second);

        private IThreadSafeTimeEntry runningTimeEntry(IThreadSafeTimeEntry timeEntry)
        {
            timeEntry = timeEntry != null && timeEntry.IsRunning() ? timeEntry : null;
            currentlyRunningTimeEntryId = timeEntry?.Id;
            return timeEntry;
        }
    }
}
