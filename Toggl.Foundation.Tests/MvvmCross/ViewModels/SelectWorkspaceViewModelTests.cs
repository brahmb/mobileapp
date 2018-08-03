using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using NSubstitute;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Toggl.PrimeRadiant.Models;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class SelectWorkspaceViewModelTests
    {
        public abstract class SelectWorkspaceViewModelTest : BaseViewModelTests<SelectWorkspaceViewModel>
        {
            protected override SelectWorkspaceViewModel CreateViewModel()
                => new SelectWorkspaceViewModel(InteractorFactory, NavigationService);

            protected List<IThreadSafeWorkspace> GenerateWorkspaceList() =>
                Enumerable.Range(0, 10).Select(i =>
                {
                    var workspace = Substitute.For<IThreadSafeWorkspace>();
                    workspace.Id.Returns(i);
                    workspace.Name.Returns(i.ToString());
                    return workspace;
                }).ToList();
        }

        public sealed class TheConstructor : SelectWorkspaceViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(bool useInteractorFactory, bool useNavigationService)
            {
                var interactorFactory = useInteractorFactory ? InteractorFactory : null;
                var navigationService = useNavigationService ? NavigationService : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new SelectWorkspaceViewModel(interactorFactory, navigationService);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class ThePrepareMethod : SelectWorkspaceViewModelTest
        {
            public ThePrepareMethod()
            {
                var workspaces = GenerateWorkspaceList();
                InteractorFactory.GetAllWorkspaces().Execute().Returns(Observable.Return(workspaces));
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheDefaultWorkspaceId()
            {
                const long expectedId = 8;
                var parameters = WorkspaceParameters.Create(expectedId, "", true);

                ViewModel.Prepare(parameters);

                await ViewModel.Initialize();
                ViewModel.Suggestions.Single(x => x.Selected).WorkspaceId.Should().Be(expectedId);
            }

            [Theory, LogIfTooSlow]
            [InlineData(true)]
            [InlineData(false)]
            public void SetsTheAllowsQueryingProperty(bool allowsQuerying)
            {
                var parameters = WorkspaceParameters.Create(10, "", allowsQuerying);

                ViewModel.Prepare(parameters);

                ViewModel.AllowQuerying.Should().Be(allowsQuerying);
            }

            [Property]
            public void SetsTheTitle(NonEmptyString title)
            {
                var parameters = WorkspaceParameters.Create(10, title.Get, false);

                ViewModel.Prepare(parameters);

                ViewModel.Title.Should().Be(title.Get);
            }
        }

        public sealed class TheInitializeMethod : SelectWorkspaceViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task AddsAllWorkspacesToTheListOfSuggestions()
            {
                var workspaces = GenerateWorkspaceList();
                InteractorFactory.GetAllWorkspaces().Execute().Returns(Observable.Return(workspaces));

                await ViewModel.Initialize();

                ViewModel.Suggestions.Should().HaveCount(10);
            }
        }

        public sealed class TheCloseCommand : SelectWorkspaceViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModel()
            {
                await ViewModel.Initialize();

                await ViewModel.CloseCommand.ExecuteAsync();

                await NavigationService.Received()
                    .Close(Arg.Is(ViewModel), Arg.Any<long>());
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsTheWorkspacePassedOnPrepare()
            {
                const long expectedId = 10;
                var parameters = WorkspaceParameters.Create(expectedId, "", true);
                ViewModel.Prepare(parameters);
                await ViewModel.Initialize();

                ViewModel.CloseCommand.ExecuteAsync().Wait();

                await NavigationService.Received()
                    .Close(Arg.Is(ViewModel), expectedId);
            }
        }

        public sealed class TheSelectWorkspaceCommand : SelectWorkspaceViewModelTest
        {
            private readonly IThreadSafeWorkspace Workspace = Substitute.For<IThreadSafeWorkspace>();

            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModel()
            {
                var selectableWorkspace = new SelectableWorkspaceViewModel(Workspace, true);

                ViewModel.SelectWorkspaceCommand.Execute(selectableWorkspace);

                await NavigationService.Received()
                    .Close(Arg.Is(ViewModel), Arg.Any<long>());
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsTheSelectedWorkspaceId()
            {
                const long expectedId = 10;
                Workspace.Id.Returns(expectedId);
                var selectableWorkspace = new SelectableWorkspaceViewModel(Workspace, true);

                ViewModel.SelectWorkspaceCommand.Execute(selectableWorkspace);

                await NavigationService.Received().Close(
                    Arg.Is(ViewModel),
                    Arg.Is(expectedId)
                );
            }
        }

        public sealed class TheTextProperty : SelectWorkspaceViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task FiltersTheSuggestionsWhenItChanges()
            {
                var workspaces = GenerateWorkspaceList();
                InteractorFactory.GetAllWorkspaces().Execute().Returns(Observable.Return(workspaces));
                await ViewModel.Initialize();

                ViewModel.Text = "0";

                ViewModel.Suggestions.Should().HaveCount(1);
            }
        }
    }
}
