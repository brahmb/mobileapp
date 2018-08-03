using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class NoWorkspaceViewModelTests
    {
        public abstract class NoWorkspaceViewModelTest : BaseViewModelTests<NoWorkspaceViewModel>
        {
            protected TestScheduler TestScheduler { get; } = new TestScheduler();

            protected override NoWorkspaceViewModel CreateViewModel()
                => new NoWorkspaceViewModel(NavigationService, DataSource);
        }

        public sealed class TheConstructor : NoWorkspaceViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(bool useNavigationService, bool useDataSource)
            {
                var navigationService = useNavigationService ? NavigationService : null;
                var dataSource = useDataSource ? DataSource : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new NoWorkspaceViewModel(navigationService, dataSource);

                tryingToConstructWithEmptyParameters.Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheTryAgainCommand : NoWorkspaceViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task ClosesWhenAnotherWorkspaceIsFetched()
            {
                var workspace = Substitute.For<IThreadSafeWorkspace>();
                DataSource.Workspaces.GetAll().Returns(Observable.Return(new List<IThreadSafeWorkspace>() { workspace }));

                await ViewModel.TryAgain();

                await NavigationService.Received().Close(Arg.Is(ViewModel));
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNothingWhenNoWorkspacesAreFetched()
            {
                DataSource.Workspaces.GetAll().Returns(Observable.Return(new List<IThreadSafeWorkspace>()));

                await ViewModel.TryAgain();

                await NavigationService.DidNotReceive().Close(Arg.Is(ViewModel));
            }

            [Fact, LogIfTooSlow]
            public async Task StartsAndStopsLoading()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.IsLoading.Subscribe(observer);

                var workspace = Substitute.For<IThreadSafeWorkspace>();
                DataSource.Workspaces.GetAll().Returns(Observable.Return(new List<IThreadSafeWorkspace>() { workspace }));

                await ViewModel.TryAgain();

                observer.Messages.Count.Should().Be(2);
                observer.Messages[0].Value.Value.Should().BeTrue();
                observer.Messages[1].Value.Value.Should().BeFalse();
            }
        }

        public sealed class TheCreateWorkspaceCommand : NoWorkspaceViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task CreatesNewWorkspaceWithDefaultName()
            {
                var name = "Rick Sanchez";
                var user = Substitute.For<IThreadSafeUser>();
                user.Fullname.Returns(name);
                DataSource.User.Current.Returns(Observable.Return(user));

                var workspacesDataSource = Substitute.For<IWorkspacesSource>();
                DataSource.Workspaces.Returns(workspacesDataSource);

                await ViewModel.CreateWorkspaceWithDefaultName();

                workspacesDataSource.Received().Create(Arg.Is($"{name}'s Workspace"));
            }

            [Fact, LogIfTooSlow]
            public async Task ClosesAfterNewWorkspaceIsCreated()
            {
                var workspace = Substitute.For<IThreadSafeWorkspace>();
                DataSource.Workspaces.Create(Arg.Any<string>()).Returns(Observable.Return(workspace));
                DataSource.Workspaces.GetAll().Returns(Observable.Return(new List<IThreadSafeWorkspace>() { workspace }));

                await ViewModel.CreateWorkspaceWithDefaultName();

                await NavigationService.Received().Close(Arg.Is(ViewModel));
            }

            [Fact, LogIfTooSlow]
            public async Task StartsAndStopsLoading()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.IsLoading.Subscribe(observer);

                var name = "Rick Sanchez";
                var user = Substitute.For<IThreadSafeUser>();
                user.Fullname.Returns(name);
                DataSource.User.Current.Returns(Observable.Return(user));

                var workspacesDataSource = Substitute.For<IWorkspacesSource>();
                DataSource.Workspaces.Returns(workspacesDataSource);

                await ViewModel.CreateWorkspaceWithDefaultName();

                observer.Messages.Count.Should().Be(2);
                observer.Messages[0].Value.Value.Should().BeTrue();
                observer.Messages[1].Value.Value.Should().BeFalse();
            }
        }
    }
}
