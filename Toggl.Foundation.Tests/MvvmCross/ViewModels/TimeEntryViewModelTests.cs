using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class TimeEntryViewModelTests
    {
        public abstract class TimeEntryViewModelTest : BaseMvvmCrossTests
        {
            protected IThreadSafeProject Project = Substitute.For<IThreadSafeProject>();
            protected IThreadSafeTimeEntry MockTimeEntry = Substitute.For<IThreadSafeTimeEntry>();

            protected Subject<DateTimeOffset> TickSubject = new Subject<DateTimeOffset>();

            protected TimeEntryViewModelTest()
            {
                var observable = TickSubject.AsObservable().Publish();
                observable.Connect();
                TimeService.CurrentDateTimeObservable.Returns(observable);
            }
        }

        public sealed class TheConstructor : TimeEntryViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void ThrowsIfTheArgumentIsNull()
            {
                Action tryingToConstructWithEmptyParameters =
                    () => new TimeEntryViewModel(null, DurationFormat.Improved);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheHasProjectProperty : TimeEntryViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(true)]
            [InlineData(false)]
            public void ChecksIfTheTimeEntryProvidedHasANonNullProject(bool hasProject)
            {
                MockTimeEntry.Duration.Returns((long)TimeSpan.FromHours(1).TotalSeconds);
                MockTimeEntry.Project.Returns(hasProject ? Project : null);

                var viewModel = new TimeEntryViewModel(MockTimeEntry, DurationFormat.Improved);

                viewModel.HasProject.Should().Be(hasProject);
            }
        }

        public sealed class TheDisplayName : TimeEntryViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(true)]
            [InlineData(false)]
            public void ChecksDisplayNameContainsArchived(bool active)
            {
                Project.Active.Returns(active);
                MockTimeEntry.Duration.Returns((long)TimeSpan.FromHours(1).TotalSeconds);
                MockTimeEntry.Project.Returns(Project);

                var viewModel = new TimeEntryViewModel(MockTimeEntry, DurationFormat.Improved);

                viewModel.ProjectName.Contains("(archived)").Should().Be(!active);
            }
        }
    }
}
