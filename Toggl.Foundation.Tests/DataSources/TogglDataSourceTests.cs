using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Models;
using Toggl.Foundation.Services;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Tests.Helpers;
using Toggl.Foundation.Tests.Sync;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Xunit;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Network;
using ThreadingTask = System.Threading.Tasks.Task;
using Toggl.Ultrawave;
using Toggl.Foundation.Shortcuts;

namespace Toggl.Foundation.Tests.DataSources
{
    public sealed class TogglDataSourceTests
    {
        public abstract class TogglDataSourceTest
        {
            protected ITogglDataSource DataSource { get; }
            protected ITogglApi Api { get; } = Substitute.For<ITogglApi>();
            protected ITogglDatabase Database { get; } = Substitute.For<ITogglDatabase>();
            protected ITimeService TimeService { get; } = Substitute.For<ITimeService>();
            protected ISyncManager SyncManager { get; } = Substitute.For<ISyncManager>();
            protected IBackgroundService BackgroundService { get; } = Substitute.For<IBackgroundService>();
            protected IErrorHandlingService ErrorHandlingService { get; } = Substitute.For<IErrorHandlingService>();
            protected ISubject<SyncProgress> ProgressSubject = new Subject<SyncProgress>();
            protected TimeSpan MinimumTimeInBackgroundForFullSync = TimeSpan.FromMinutes(5);
            protected IApplicationShortcutCreator ApplicationShortcutCreator { get; } = Substitute.For<IApplicationShortcutCreator>();
            protected IAnalyticsService AnalyticsService { get; } = Substitute.For<IAnalyticsService>();

            public TogglDataSourceTest()
            {
                SyncManager.ProgressObservable.Returns(ProgressSubject.AsObservable());
                DataSource = new TogglDataSource(
                    Api,
                    Database,
                    TimeService,
                    ErrorHandlingService,
                    BackgroundService,
                    _ => SyncManager,
                    MinimumTimeInBackgroundForFullSync,
                    ApplicationShortcutCreator,
                    AnalyticsService);
            }
        }

        public sealed class TheLogoutMethod : TogglDataSourceTest
        {
            [Fact, LogIfTooSlow]
            public void ClearsTheDatabase()
            {
                DataSource.Logout().Wait();

                Database.Received(1).Clear();
            }

            [Fact, LogIfTooSlow]
            public void FreezesTheSyncManager()
            {
                DataSource.Logout().Wait();

                SyncManager.Received().Freeze();
            }

            [Fact, LogIfTooSlow]
            public void DoesNotClearTheDatabaseBeforeTheSyncManagerCompletesFreezing()
            {
                var scheduler = new TestScheduler();
                SyncManager.Freeze().Returns(Observable.Never<SyncState>());

                var observable = DataSource.Logout().SubscribeOn(scheduler).Publish();
                observable.Connect();
                scheduler.AdvanceBy(TimeSpan.FromDays(1).Ticks);

                Database.DidNotReceive().Clear();
            }

            [Fact, LogIfTooSlow]
            public void ClearTheDatabaseOnlyOnceTheSyncManagerFreezeEmitsAValueEvenThoughItDoesNotComplete()
            {
                var freezingSubject = new Subject<SyncState>();
                SyncManager.Freeze().Returns(freezingSubject.AsObservable());

                var observable = DataSource.Logout().Publish();
                observable.Connect();

                Database.DidNotReceive().Clear();

                freezingSubject.OnNext(SyncState.Sleep);

                Database.Received().Clear();
            }

            [Fact, LogIfTooSlow]
            public void EmitsUnitValueAndCompletesWhenFreezeAndDatabaseClearEmitSingleValueButDoesNotComplete()
            {
                var clearingSubject = new Subject<Unit>();
                SyncManager.Freeze().Returns(_ => Observable.Return(SyncState.Sleep));
                Database.Clear().Returns(clearingSubject.AsObservable());
                bool emitsUnitValue = false;
                bool completed = false;

                var observable = DataSource.Logout();
                observable.Subscribe(
                    _ => emitsUnitValue = true,
                    () => completed = true);
                clearingSubject.OnNext(Unit.Default);

                emitsUnitValue.Should().BeTrue();
                completed.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask NotifiesShortcutCreatorAboutLogout()
            {
                await DataSource.Logout();

                ApplicationShortcutCreator.Received().OnLogout();
            }
        }

        public sealed class TheStartSyncingMethod : TogglDataSourceTest
        {
            [Fact, LogIfTooSlow]
            public void SubscribesSyncManagerToTheBackgroundServiceSignal()
            {
                var observable = Substitute.For<IObservable<TimeSpan>>();
                BackgroundService.AppResumedFromBackground.Returns(observable);

                DataSource.StartSyncing();

                observable.ReceivedWithAnyArgs().Subscribe(null);
            }

            [Fact, LogIfTooSlow]
            public void CallsForceFullSync()
            {
                DataSource.StartSyncing();

                SyncManager.Received().ForceFullSync();
            }

            [Fact, LogIfTooSlow]
            public void ReturnsAnObservableWhichEmitsWhenTheForceFullSyncObservableEmits()
            {
                bool emitted = false;
                var forceFullSyncSubject = new Subject<SyncState>();
                SyncManager.ForceFullSync().Returns(forceFullSyncSubject.AsObservable());

                var observable = DataSource.StartSyncing();
                observable.Subscribe(_ => emitted = true);
                forceFullSyncSubject.OnNext(SyncState.Pull);

                emitted.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void ReturnsAnObservableWhichDoesNotEmitWhenTheForceFullSyncObservableDoesNotEmit()
            {
                bool emitted = false;
                var forceFullSyncSubject = new Subject<SyncState>();
                SyncManager.ForceFullSync().Returns(forceFullSyncSubject.AsObservable());

                var observable = DataSource.StartSyncing();
                observable.Subscribe(_ => emitted = true);

                emitted.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void CallsForceFullSyncOnlyOnceWhenTheObservableDoesNotEmitAnyValues()
            {
                var observable = Observable.Never<TimeSpan>();
                BackgroundService.AppResumedFromBackground.Returns(observable);

                DataSource.StartSyncing();

                SyncManager.Received(1).ForceFullSync();
            }

            [Fact, LogIfTooSlow]
            public void CallsForceFullSyncWhenAValueIsEmitted()
            {
                var subject = new Subject<TimeSpan>();
                BackgroundService.AppResumedFromBackground.Returns(subject.AsObservable());

                DataSource.StartSyncing();
                subject.OnNext(MinimumTimeInBackgroundForFullSync + TimeSpan.FromSeconds(1));

                SyncManager.Received(2).ForceFullSync();
            }

            [Fact, LogIfTooSlow]
            public void ThrowsWhenCalledForTheSecondTime()
            {
                var observable = Observable.Never<TimeSpan>();
                BackgroundService.AppResumedFromBackground.Returns(observable);
                DataSource.StartSyncing();

                Action callForTheSecondTime = () => DataSource.StartSyncing();

                callForTheSecondTime.Should().Throw<InvalidOperationException>();
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask UnsubscribesFromTheSignalAfterLogout()
            {
                var subject = new Subject<TimeSpan>();
                BackgroundService.AppResumedFromBackground.Returns(subject.AsObservable());
                await DataSource.StartSyncing();
                SyncManager.ClearReceivedCalls();
                await DataSource.Logout();

                subject.OnNext(MinimumTimeInBackgroundForFullSync + TimeSpan.FromSeconds(1));

                await SyncManager.DidNotReceive().ForceFullSync();
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask ThrowsWhenStartSyncingIsCalledAfterLoggingOut()
            {
                await DataSource.Logout();

                Action startSyncing = () => DataSource.StartSyncing().Wait();

                startSyncing.Should().Throw<InvalidOperationException>();
            }
        }

        public sealed class TheHasUnsyncedDataMetod
        {
            public abstract class BaseHasUnsyncedDataTest<TModel, TDatabaseModel> : TogglDataSourceTest
                where TModel : class
                where TDatabaseModel : TModel, IDatabaseSyncable
            {
                private readonly IBaseStorage<TDatabaseModel> repository;
                private readonly Func<TModel, TDatabaseModel> dirty;
                private readonly Func<TModel, string, TDatabaseModel> unsyncable;

                public BaseHasUnsyncedDataTest(Func<ITogglDatabase, IBaseStorage<TDatabaseModel>> repository,
                    Func<TModel, TDatabaseModel> dirty, Func<TModel, string, TDatabaseModel> unsyncable)
                {
                    this.repository = repository(Database);
                    this.dirty = dirty;
                    this.unsyncable = unsyncable;
                }

                [Fact, LogIfTooSlow]
                public async ThreadingTask ReturnsTrueWhenThereIsAnEntityWhichNeedsSync()
                {
                    var dirtyEntity = dirty(Substitute.For<TModel>());
                    repository.GetAll(Arg.Any<Func<TDatabaseModel, bool>>())
                        .Returns(Observable.Return(new[] { dirtyEntity }));

                    var hasUnsyncedData = await DataSource.HasUnsyncedData();

                    hasUnsyncedData.Should().BeTrue();
                }

                [Fact, LogIfTooSlow]
                public async ThreadingTask ReturnsTrueWhenThereIsAnEntityWhichFailedToSync()
                {
                    var unsyncableEntity = unsyncable(Substitute.For<TModel>(), "Error message.");
                    repository.GetAll(Arg.Any<Func<TDatabaseModel, bool>>())
                        .Returns(Observable.Return(new[] { unsyncableEntity }));

                    var hasUnsyncedData = await DataSource.HasUnsyncedData();

                    hasUnsyncedData.Should().BeTrue();
                }

                [Fact, LogIfTooSlow]
                public async ThreadingTask ReturnsFalseWhenThereIsNoUnsyncedEntityAndAllOtherRepositoriesAreSyncedAsWell()
                {
                    repository.GetAll(Arg.Any<Func<TDatabaseModel, bool>>())
                        .Returns(Observable.Return(new TDatabaseModel[0]));

                    var hasUnsyncedData = await DataSource.HasUnsyncedData();

                    hasUnsyncedData.Should().BeFalse();
                }
            }

            public sealed class TimeEntriesTest : BaseHasUnsyncedDataTest<ITimeEntry, IDatabaseTimeEntry>
            {
                public TimeEntriesTest()
                    : base(database => database.TimeEntries, TimeEntry.Dirty, TimeEntry.Unsyncable)
                {
                }
            }

            public sealed class ProjectsTest : BaseHasUnsyncedDataTest<IProject, IDatabaseProject>
            {
                public ProjectsTest()
                    : base(database => database.Projects, Project.Dirty, Project.Unsyncable)
                {
                }
            }

            public sealed class UserTest : BaseHasUnsyncedDataTest<IUser, IDatabaseUser>
            {
                public UserTest()
                    : base(database => database.User, User.Dirty, User.Unsyncable)
                {
                }
            }

            public sealed class TasksTest : BaseHasUnsyncedDataTest<ITask, IDatabaseTask>
            {
                public TasksTest()
                    : base(database => database.Tasks, Task.Dirty, Task.Unsyncable)
                {
                }
            }

            public sealed class ClientsTest : BaseHasUnsyncedDataTest<IClient, IDatabaseClient>
            {
                public ClientsTest()
                    : base(database => database.Clients, Client.Dirty, Client.Unsyncable)
                {
                }
            }

            public sealed class TagsTest : BaseHasUnsyncedDataTest<ITag, IDatabaseTag>
            {
                public TagsTest()
                    : base(database => database.Tags, Tag.Dirty, Tag.Unsyncable)
                {
                }
            }

            public sealed class WorkspacesTest : BaseHasUnsyncedDataTest<IWorkspace, IDatabaseWorkspace>
            {
                public WorkspacesTest()
                    : base(database => database.Workspaces, Workspace.Dirty, Workspace.Unsyncable)
                {
                }
            }
        }

        public sealed class SyncErrorHandling : TogglDataSourceTest
        {
            private IRequest request => Substitute.For<IRequest>();
            private IResponse response => Substitute.For<IResponse>();

            [Fact, LogIfTooSlow]
            public void SetsTheOutdatedClientVersionFlag()
            {
                var exception = new ClientDeprecatedException(request, response);
                ErrorHandlingService.TryHandleDeprecationError(Arg.Any<ClientDeprecatedException>()).Returns(true);

                ProgressSubject.OnError(exception);

                ErrorHandlingService.Received().TryHandleDeprecationError(Arg.Is(exception));
                ErrorHandlingService.DidNotReceive().TryHandleUnauthorizedError(Arg.Is(exception));
            }

            [Fact, LogIfTooSlow]
            public void SetsTheOutdatedApiVersionFlag()
            {
                var exception = new ApiDeprecatedException(request, response);
                ErrorHandlingService.TryHandleDeprecationError(Arg.Any<ApiDeprecatedException>()).Returns(true);

                ProgressSubject.OnError(exception);

                ErrorHandlingService.Received().TryHandleDeprecationError(Arg.Is(exception));
                ErrorHandlingService.DidNotReceive().TryHandleUnauthorizedError(Arg.Is(exception));
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask SetsTheUnauthorizedAccessFlag()
            {
                var exception = new UnauthorizedException(request, response);
                ErrorHandlingService.TryHandleUnauthorizedError(Arg.Any<UnauthorizedException>()).Returns(true);

                ProgressSubject.OnError(exception);

                ErrorHandlingService.Received().TryHandleUnauthorizedError(Arg.Is(exception));
            }

            [Theory, LogIfTooSlow]
            [MemberData(nameof(SyncManagerTests.TheProgressObservable.ExceptionsRethrownByProgressObservableOnError), MemberType = typeof(SyncManagerTests.TheProgressObservable))]
            public void DoesNotThrowForAnyExceptionWhichCanBeThrownByTheProgressObservable(Exception exception)
            {
                ErrorHandlingService.TryHandleUnauthorizedError(Arg.Any<UnauthorizedException>()).Returns(true);
                ErrorHandlingService.TryHandleDeprecationError(Arg.Any<ClientDeprecatedException>()).Returns(true);
                ErrorHandlingService.TryHandleDeprecationError(Arg.Any<ApiDeprecatedException>()).Returns(true);

                Action processingError = () => ProgressSubject.OnError(exception);

                processingError.Should().NotThrow();
            }

            [Theory, LogIfTooSlow]
            [MemberData(nameof(ApiExceptionsWhichAreNotThrowByTheProgressObservable))]
            public void ThrowsForDifferentException(Exception exception)
            {
                Action handling = () => ProgressSubject.OnError(exception);

                handling.Should().Throw<ArgumentException>();
            }

            [Fact, LogIfTooSlow]
            public void UnsubscribesFromTheBackgroundServiceObservableWhenExceptionIsCaught()
            {
                var subject = new Subject<TimeSpan>();
                BackgroundService.AppResumedFromBackground.Returns(subject.AsObservable());
                DataSource.StartSyncing();
                SyncManager.ClearReceivedCalls();
                var exception = new UnauthorizedException(request, response);
                ErrorHandlingService.TryHandleUnauthorizedError(Arg.Any<UnauthorizedException>()).Returns(true);

                ProgressSubject.OnError(exception);
                subject.OnNext(MinimumTimeInBackgroundForFullSync + TimeSpan.FromSeconds(1));

                SyncManager.DidNotReceive().ForceFullSync();
            }


            public static IEnumerable<object[]> ApiExceptionsWhichAreNotThrowByTheProgressObservable()
                => ApiExceptions.ClientExceptions
                    .Concat(ApiExceptions.ServerExceptions)
                    .Where(args => SyncManagerTests.TheProgressObservable.ExceptionsRethrownByProgressObservableOnError()
                        .All(thrownByProgress => args[0].GetType() != thrownByProgress[0].GetType()));
        }
    }
}
