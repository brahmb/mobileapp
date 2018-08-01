using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using MvvmCross.ViewModels;
using NSubstitute;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac;
using Toggl.Foundation.Tests.Generators;
using Toggl.Foundation.Helper;
using Xunit;
using static Toggl.Foundation.MvvmCross.Parameters.SelectTimeParameters.Origin;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    using static Constants;

    public sealed class SelectTimeViewModelTests
    {
        public abstract class SelectTimeViewModelTest : BaseViewModelTests<SelectTimeViewModel>
        {
            protected override SelectTimeViewModel CreateViewModel()
                => new SelectTimeViewModel(DataSource, NavigationService, InteractorFactory, TimeService);

            protected SelectTimeParameters CreateParameter(DateTimeOffset start, DateTimeOffset? stop)
            {
                var dateFormat = DateFormat.FromLocalizedDateFormat("MM.DD.YYYY");
                var timeFormat = TimeFormat.FromLocalizedTimeFormat("H:mm");

                return SelectTimeParameters
                    .CreateFromOrigin(StartTime, start, stop)
                    .WithFormats(dateFormat, timeFormat);
            }
        }

        public sealed class TheIncreaseDurationCommand : SelectTimeViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void IncreasesTheStartTimeWhenIsRunning()
            {
                var minutes = 5;
                var startTime = DateTimeOffset.Now - TimeSpan.FromHours(10);

                ViewModel.StartTime = startTime;
                ViewModel.StopTime = null;

                var duration = ViewModel.Duration;

                ViewModel.IncreaseDurationCommand.Execute(minutes);

                ViewModel.StartTime.Should().Be(startTime - TimeSpan.FromMinutes(minutes));
                ViewModel.StopTime.Should().Be(null);
                ViewModel.Duration.Should().Be(duration + TimeSpan.FromMinutes(minutes));
            }

            [Fact, LogIfTooSlow]
            public void IncreasesTheStopTimeWhenIsNotRunning()
            {
                var minutes = 5;
                var startTime = DateTimeOffset.Now;
                var stopTime = DateTimeOffset.Now + TimeSpan.FromHours(1);

                ViewModel.StartTime = startTime;
                ViewModel.StopTime = stopTime;

                var duration = ViewModel.Duration;

                ViewModel.IncreaseDurationCommand.Execute(minutes);

                ViewModel.StartTime.Should().Be(startTime);
                ViewModel.StopTime.Should().Be(stopTime + TimeSpan.FromMinutes(minutes));
                ViewModel.Duration.Should().Be(duration + TimeSpan.FromMinutes(minutes));
            }

            [Theory, LogIfTooSlow]
            [InlineData(5)]
            [InlineData(10)]
            [InlineData(30)]
            public void IncreasesTheDurationForCorrectAmountOfTime(int minutes)
            {
                var startTime = DateTimeOffset.Now;
                var stopTime = DateTimeOffset.Now + TimeSpan.FromHours(1);

                ViewModel.StartTime = startTime;
                ViewModel.StopTime = stopTime;

                var duration = ViewModel.Duration;

                ViewModel.IncreaseDurationCommand.Execute(minutes);

                ViewModel.Duration.Should().Be(duration + TimeSpan.FromMinutes(minutes));
            }
        }

        public sealed class TheConstructor : SelectTimeViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(bool useDataSource, bool useNavigationService, bool useInteractorFactory, bool useTimeService)
            {
                var dataSource = useDataSource ? DataSource : null;
                var navigationService = useNavigationService ? NavigationService : null;
                var interactorFactory = useInteractorFactory ? InteractorFactory : null;
                var timeService = useTimeService ? TimeService : null;

                Action constructingWithEmptyParameters =
                    () => new SelectTimeViewModel(dataSource, navigationService, interactorFactory, timeService);

                constructingWithEmptyParameters.Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheStartTimeBoundariesProperty : SelectTimeViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void IsCorrectlyInitializedOnPrepareForStoppedEntry()
            {
                var start = DateTimeOffset.Now;
                var stop = DateTimeOffset.Now + TimeSpan.FromHours(1);
                var parameter = CreateParameter(start, stop);

                ViewModel.Prepare(parameter);

                ViewModel.StartTimeBoundaries.Should().Be(
                    new DateTimeOffsetRange(stop - MaxTimeEntryDuration, stop));
            }

            [Fact, LogIfTooSlow]
            public void IsCorrectlyInitializedOnPrepareForRunningEntry()
            {
                var start = DateTimeOffset.Now;
                var parameter = CreateParameter(start, null);

                ViewModel.Prepare(parameter);

                ViewModel.StartTimeBoundaries.Should().Be(
                    new DateTimeOffsetRange(EarliestAllowedStartTime, LatestAllowedStartTime));
            }

            [Fact, LogIfTooSlow]
            public void ChangingStartAndStopTimeDoesNotChangeBoundaryBeforePrepareHasRun()
            {
                var start = DateTimeOffset.Now;
                var stop = DateTimeOffset.Now.AddHours(1);
                var oldBoundaries = ViewModel.StartTimeBoundaries;

                ViewModel.StartTime = start;
                ViewModel.StopTime = stop;

                ViewModel.StartTimeBoundaries.Should().Be(oldBoundaries);
            }
        }

        public sealed class TheStopTimeBoundariesProperty : SelectTimeViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void IsCorrectlyInitializedOnPrepareForStoppedEntry()
            {
                var start = DateTimeOffset.Now;
                var stop = DateTimeOffset.Now + TimeSpan.FromHours(1);
                var parameter = CreateParameter(start, stop);

                ViewModel.Prepare(parameter);

                ViewModel.StopTimeBoundaries.Should().Be(
                    new DateTimeOffsetRange(start, start + MaxTimeEntryDuration));
            }

            [Fact, LogIfTooSlow]
            public void IsCorrectlyInitializedOnPrepareForRunningEntry()
            {
                var start = DateTimeOffset.Now;
                var parameter = CreateParameter(start, null);

                ViewModel.Prepare(parameter);

                ViewModel.StopTimeBoundaries.Should().Be(
                    new DateTimeOffsetRange(start, start + MaxTimeEntryDuration));
            }

            [Fact, LogIfTooSlow]
            public void ChangingStartAndStopTimeDoesNotChangeBoundaryBeforePrepareHasRun()
            {
                var start = DateTimeOffset.Now;
                var stop = DateTimeOffset.Now.AddHours(1);
                var oldBoundaries = ViewModel.StopTimeBoundaries;

                ViewModel.StartTime = start;
                ViewModel.StopTime = stop;

                ViewModel.StopTimeBoundaries.Should().Be(oldBoundaries);
            }
        }

        public sealed class ThePrepareMethod : SelectTimeViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void ConvertsInputTimesFromUtcToLocalTimeForUseInUI()
            {
                var startTime = DateTimeOffset.UtcNow;
                var stopTime = DateTimeOffset.UtcNow.AddHours(1);
                var localStartTime = startTime.ToLocalTime();
                var localStopTime = stopTime.ToLocalTime();

                var parameter = CreateParameter(startTime, stopTime);
                ViewModel.Prepare(parameter);

                ViewModel.StartTime.Should().Be(localStartTime);
                ViewModel.StopTime.Should().Be(localStopTime);
            }

            [Theory, LogIfTooSlow]
            [MemberData(nameof(TimeZoneOffsets))]
            public void ConvertsInputTimesFromZonesToLocalTimeForUseInUI(int offset)
            {
                var zoneOffset = TimeSpan.FromHours(offset);
                var startTime = new DateTimeOffset(2018, 1, 1, 13, 15, 22, zoneOffset);
                var stopTime = startTime.AddHours(1);
                var localStartTime = startTime.ToLocalTime();
                var localStopTime = stopTime.ToLocalTime();

                var parameter = CreateParameter(startTime, stopTime);
                ViewModel.Prepare(parameter);

                ViewModel.StartTime.Should().Be(localStartTime);
                ViewModel.StopTime.Should().Be(localStopTime);
            }

            [Theory, LogIfTooSlow]
            [MemberData(nameof(TimeZoneOffsets))]
            public async Task ReturnsUtcTimeAsSaveResult(int offset)
            {
                var zoneOffset = TimeSpan.FromHours(offset);
                var startTime = new DateTimeOffset(2018, 1, 1, 13, 15, 22, zoneOffset);
                var stopTime = startTime.AddHours(1);
                var universalStartTime = startTime.ToUniversalTime();
                var universalStopTime = stopTime.ToUniversalTime();

                var parameter = CreateParameter(startTime, stopTime);
                ViewModel.Prepare(parameter);

                ViewModel.SaveCommand.ExecuteAsync().Wait();

                await NavigationService
                   .Received()
                   .Close(
                        Arg.Is(ViewModel),
                        Arg.Is<SelectTimeResultsParameters>(param => ensureResultTimesAreInUtc(
                            param, universalStartTime, universalStopTime))
                   );
            }

            private bool ensureResultTimesAreInUtc(SelectTimeResultsParameters param,
                                                   DateTimeOffset expectedStart,
                                                   DateTimeOffset expectedStop)
            {
                return param.Start == expectedStart && param.Stop == expectedStop;
            }

            public static IEnumerable<object[]> TimeZoneOffsets()
                => Enumerable.Range(-11, 26).Select(i => new object[] { i });
        }
    }
}
