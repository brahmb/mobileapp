using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using NSubstitute;
using Toggl.Foundation.Interactors.Settings;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Tests.Generators;
using Toggl.Foundation.Tests.Mocks;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave.ApiClients;
using Toggl.Ultrawave.Network;
using Xunit;
using static Toggl.Foundation.Interactors.Settings.SendFeedbackInteractor;

namespace Toggl.Foundation.Tests.Interactors.Settings
{
    public sealed class SendFeedbackInteractorTests
    {
        public sealed class TheConstructor : BaseInteractorTests
        {
            [Theory, LogIfTooSlow]
            [ClassData(typeof(TenParameterConstructorTestData))]
            public void ThrowsIfAnyOfTheParametersIsNullOrInvalid(
                bool useFeedbackApi,
                bool useUserDataSource,
                bool useWorkspacesDataSource,
                bool useTimeEntriesDataSource,
                bool usePlatformConstants,
                bool useUserPreferences,
                bool useLastTimeUsageStorage,
                bool useTimeService,
                bool useUserAgent,
                bool useMessage)
            {
                // ReSharper disable once ObjectCreationAsStatement
                Action createInstance = () => new SendFeedbackInteractor(
                    useFeedbackApi ? Substitute.For<IFeedbackApi>() : null,
                    useUserDataSource ? DataSource.User : null,
                    useWorkspacesDataSource ? DataSource.Workspaces : null,
                    useTimeEntriesDataSource ? DataSource.TimeEntries : null,
                    usePlatformConstants ? Substitute.For<IPlatformConstants>() : null,
                    useUserPreferences ? UserPreferences : null,
                    useLastTimeUsageStorage ? Substitute.For<ILastTimeUsageStorage>() : null,
                    useTimeService ? TimeService : null,
                    useUserAgent ? new UserAgent("a", "b") : null,
                    useMessage ? "some message" : null);

                createInstance.Should().Throw<ArgumentException>();
            }
        }

        public sealed class TheSendMethod : BaseInteractorTests
        {
            private readonly IEnumerable<IThreadSafeTimeEntry> timeEntries = new[]
            {
                new MockTimeEntry { SyncStatus = SyncStatus.InSync },
                new MockTimeEntry { SyncStatus = SyncStatus.SyncNeeded },
                new MockTimeEntry { SyncStatus = SyncStatus.InSync },
                new MockTimeEntry { SyncStatus = SyncStatus.SyncFailed },
                new MockTimeEntry { SyncStatus = SyncStatus.SyncFailed },
                new MockTimeEntry { SyncStatus = SyncStatus.InSync },
            };

            private readonly IEnumerable<IThreadSafeWorkspace> workspaces = new[]
            {
                new MockWorkspace(), new MockWorkspace(), new MockWorkspace(), new MockWorkspace()
            };

            private readonly IFeedbackApi feedbackApi = Substitute.For<IFeedbackApi>();

            private readonly IThreadSafeUser user = Substitute.For<IThreadSafeUser>();

            public TheSendMethod()
            {
                DataSource.User.Get().Returns(Observable.Return(user));
                DataSource.Workspaces.GetAll().Returns(Observable.Return(workspaces));
                DataSource.TimeEntries.GetAll().Returns(Observable.Return(timeEntries));
                DataSource.TimeEntries.GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>())
                    .Returns(callInfo => Observable.Return(timeEntries.Where<IThreadSafeTimeEntry>(callInfo.Arg<Func<IDatabaseTimeEntry, bool>>())));
            }

            [Property]
            public void SendsUsersMessage(NonNull<string> message)
            {
                var email = $"{Guid.NewGuid().ToString()}@randomdomain.com".ToEmail();
                user.Email.Returns(email);

                executeInteractor(message: message.Get).Wait();

                feedbackApi.Received().Send(Arg.Is(email), Arg.Is(message.Get), Arg.Any<Dictionary<string, string>>());
            }

            [Fact, LogIfTooSlow]
            public async Task SendsCorrectPlatformConstants()
            {
                var operatingSystem = "TogglOS";
                var phoneModel = "TogglPhone";
                PlatformConstants.OperatingSystem.Returns(operatingSystem);
                PlatformConstants.PhoneModel.Returns(phoneModel);

                await executeInteractor();

                await feedbackApi.Received().Send(Arg.Any<Email>(), Arg.Any<string>(), Arg.Is<Dictionary<string, string>>(
                    data => data[SendFeedbackInteractor.OperatingSystem] == operatingSystem
                        && data[PhoneModel] == phoneModel));
            }

            [Fact, LogIfTooSlow]
            public async Task SendsTheAppPlatformSlashAppVersion()
            {
                var agent = "Eliah";
                var version = "42.2";
                var userAgent = new UserAgent(agent, version);
                var formattedUserAgent = $"{agent}/{version}";

                await executeInteractor(userAgent: userAgent);

                await feedbackApi.Received().Send(Arg.Any<Email>(), Arg.Any<string>(), Arg.Is<Dictionary<string, string>>(
                    data => data[AppNameAndVersion] == formattedUserAgent));
            }

            [Property]
            public void SendsTimesOfLastUsage(
                DateTimeOffset? login,
                DateTimeOffset? syncAttempt,
                DateTimeOffset? successfulSync)
            {
                LastTimeUsageStorage.LastLogin.Returns(login);
                LastTimeUsageStorage.LastSyncAttempt.Returns(syncAttempt);
                LastTimeUsageStorage.LastSuccessfulSync.Returns(successfulSync);

                executeInteractor().Wait();

                feedbackApi.Received().Send(Arg.Any<Email>(), Arg.Any<string>(), Arg.Is<Dictionary<string, string>>(
                    data => data[LastLogin] == (login.HasValue ? login.ToString() : "never")
                        && data[LastSyncAttempt] == (syncAttempt.HasValue ? syncAttempt.ToString() : "never")
                        && data[LastSuccessfulSync] == (successfulSync.HasValue ? successfulSync.ToString() : "never")));
            }

            [Property]
            public void SendsCurrentDeviceTime(DateTimeOffset now)
            {
                TimeService.CurrentDateTime.Returns(now);

                executeInteractor().Wait();

                feedbackApi.Received().Send(Arg.Any<Email>(), Arg.Any<string>(), Arg.Is<Dictionary<string, string>>(
                    data => data[DeviceTime] == now.ToString()));
            }

            [Property]
            public void SendsUsersPreferences(bool isManualModeEnabled)
            {
                UserPreferences.IsManualModeEnabled.Returns(isManualModeEnabled);

                executeInteractor().Wait();

                feedbackApi.Received().Send(Arg.Any<Email>(), Arg.Any<string>(), Arg.Is<Dictionary<string, string>>(
                    data => data[ManualModeIsOn] == (isManualModeEnabled ? "yes" : "no")));
            }

            [Fact, LogIfTooSlow]
            public async Task CountsAllWorkspaces()
            {
                await executeInteractor();

                feedbackApi.Received().Send(Arg.Any<Email>(), Arg.Any<string>(), Arg.Is<Dictionary<string, string>>(
                    data => data[NumberOfWorkspaces] == "4"));
            }

            [Fact, LogIfTooSlow]
            public async Task CountsTimeEntries()
            {
                await executeInteractor();

                feedbackApi.Received().Send(Arg.Any<Email>(), Arg.Any<string>(), Arg.Is<Dictionary<string, string>>(
                    data => data[NumberOfTimeEntries] == "6"
                        && data[NumberOfUnsyncedTimeEntries] == "1"
                        && data[NumberOfUnsyncableTimeEntries] == "2"));
            }

            private async Task executeInteractor(
                UserAgent userAgent = null,
                string message = "")
            {
                var interactor = new SendFeedbackInteractor(
                    feedbackApi,
                    DataSource.User,
                    DataSource.Workspaces,
                    DataSource.TimeEntries,
                    PlatformConstants,
                    UserPreferences,
                    LastTimeUsageStorage,
                    TimeService,
                    userAgent ?? new UserAgent("agent", "version"),
                    message);

                await interactor.Execute();
            }
        }
    }
}
