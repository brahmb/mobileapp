﻿using System;
using System.Collections.Generic;
using Toggl.Foundation.Models.Interfaces;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Toggl.Multivac;
using Toggl.Multivac.Models;

namespace Toggl.Foundation.Models
{
    internal partial class Project : IThreadSafeProject
    {
        public long Id { get; }

        public long WorkspaceId { get; }

        public long? ClientId { get; }

        public string Name { get; }

        public bool IsPrivate { get; }

        public bool Active { get; }

        public string Color { get; }

        public bool? Billable { get; }

        public bool? Template { get; }

        public bool? AutoEstimates { get; }

        public long? EstimatedHours { get; }

        public double? Rate { get; }

        public string Currency { get; }

        public int? ActualHours { get; }

        IDatabaseClient IDatabaseProject.Client => Client;

        public IThreadSafeClient Client { get; }

        IDatabaseWorkspace IDatabaseProject.Workspace => Workspace;

        public IThreadSafeWorkspace Workspace { get; }

        IEnumerable<IDatabaseTask> IDatabaseProject.Tasks => Tasks;

        public IEnumerable<IThreadSafeTask> Tasks { get; }

        public bool IsDeleted { get; }

        public SyncStatus SyncStatus { get; }

        public string LastSyncErrorMessage { get; }

        public DateTimeOffset At { get; }

        public DateTimeOffset? ServerDeletedAt { get; }

        public bool IsGhost => Workspace.IsGhost;
    }

    internal partial class Tag : IThreadSafeTag
    {
        public long Id { get; }

        public long WorkspaceId { get; }

        public string Name { get; }

        IDatabaseWorkspace IDatabaseTag.Workspace => Workspace;

        public IThreadSafeWorkspace Workspace { get; }

        public bool IsDeleted { get; }

        public SyncStatus SyncStatus { get; }

        public string LastSyncErrorMessage { get; }

        public DateTimeOffset At { get; }

        public DateTimeOffset? ServerDeletedAt { get; }

        public bool IsGhost => Workspace.IsGhost;
    }

    internal partial class Task : IThreadSafeTask
    {
        public long Id { get; }

        public string Name { get; }

        public long ProjectId { get; }

        public long WorkspaceId { get; }

        public long? UserId { get; }

        public long EstimatedSeconds { get; }

        public bool Active { get; }

        public long TrackedSeconds { get; }

        IDatabaseUser IDatabaseTask.User => User;

        public IThreadSafeUser User { get; }

        IDatabaseProject IDatabaseTask.Project => Project;

        public IThreadSafeProject Project { get; }

        IDatabaseWorkspace IDatabaseTask.Workspace => Workspace;

        public IThreadSafeWorkspace Workspace { get; }

        public bool IsDeleted { get; }

        public SyncStatus SyncStatus { get; }

        public string LastSyncErrorMessage { get; }

        public DateTimeOffset At { get; }

        public bool IsGhost => Workspace.IsGhost;
    }

    internal partial class TimeEntry : IThreadSafeTimeEntry
    {
        public long Id { get; }

        public long WorkspaceId { get; }

        public long? ProjectId { get; }

        public long? TaskId { get; }

        public bool Billable { get; }

        public DateTimeOffset Start { get; }

        public long? Duration { get; }

        public string Description { get; }

        public IEnumerable<long> TagIds { get; }

        public long UserId { get; }

        IDatabaseTask IDatabaseTimeEntry.Task => Task;

        public IThreadSafeTask Task { get; }

        IDatabaseUser IDatabaseTimeEntry.User => User;

        public IThreadSafeUser User { get; }

        IDatabaseProject IDatabaseTimeEntry.Project => Project;

        public IThreadSafeProject Project { get; }

        IDatabaseWorkspace IDatabaseTimeEntry.Workspace => Workspace;

        public IThreadSafeWorkspace Workspace { get; }

        IEnumerable<IDatabaseTag> IDatabaseTimeEntry.Tags => Tags;

        public IEnumerable<IThreadSafeTag> Tags { get; }

        public bool IsDeleted { get; }

        public SyncStatus SyncStatus { get; }

        public string LastSyncErrorMessage { get; }

        public DateTimeOffset At { get; }

        public DateTimeOffset? ServerDeletedAt { get; }

        public bool IsGhost => Workspace.IsGhost;
    }

    internal partial class User : IThreadSafeUser
    {
        public long Id { get; }

        public string ApiToken { get; }

        public long? DefaultWorkspaceId { get; }

        public Email Email { get; }

        public string Fullname { get; }

        public BeginningOfWeek BeginningOfWeek { get; }

        public string Language { get; }

        public string ImageUrl { get; }

        public bool IsDeleted { get; }

        public SyncStatus SyncStatus { get; }

        public string LastSyncErrorMessage { get; }

        public DateTimeOffset At { get; }
    }

    internal partial class Workspace : IThreadSafeWorkspace
    {
        public long Id { get; }

        public string Name { get; }

        public bool Admin { get; }

        public DateTimeOffset? SuspendedAt { get; }

        public double? DefaultHourlyRate { get; }

        public string DefaultCurrency { get; }

        public bool OnlyAdminsMayCreateProjects { get; }

        public bool OnlyAdminsSeeBillableRates { get; }

        public bool OnlyAdminsSeeTeamDashboard { get; }

        public bool ProjectsBillableByDefault { get; }

        public int Rounding { get; }

        public int RoundingMinutes { get; }

        public string LogoUrl { get; }

        public bool IsDeleted { get; }

        public SyncStatus SyncStatus { get; }

        public string LastSyncErrorMessage { get; }

        public DateTimeOffset At { get; }

        public DateTimeOffset? ServerDeletedAt { get; }

        public bool IsGhost { get; }
    }

    internal partial class WorkspaceFeature : IThreadSafeWorkspaceFeature
    {
        public WorkspaceFeatureId FeatureId { get; }

        public bool Enabled { get; }

    }

    internal partial class WorkspaceFeatureCollection : IThreadSafeWorkspaceFeatureCollection
    {
        public long WorkspaceId { get; }

        public IEnumerable<IWorkspaceFeature> Features { get; }

        IDatabaseWorkspace IDatabaseWorkspaceFeatureCollection.Workspace => Workspace;

        public IThreadSafeWorkspace Workspace { get; }

        IEnumerable<IDatabaseWorkspaceFeature> IDatabaseWorkspaceFeatureCollection.DatabaseFeatures => DatabaseFeatures;

        public IEnumerable<IThreadSafeWorkspaceFeature> DatabaseFeatures { get; }

    }
}
