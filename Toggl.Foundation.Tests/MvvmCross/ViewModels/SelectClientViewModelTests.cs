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
    public sealed class SelectClientViewModelTests
    {
        public abstract class SelectClientViewModelTest : BaseViewModelTests<SelectClientViewModel>
        {
            protected SelectClientParameters Parameters { get; }
                = SelectClientParameters.WithIds(10, null);

            protected override SelectClientViewModel CreateViewModel()
               => new SelectClientViewModel(DataSource, NavigationService);

            protected List<IThreadSafeClient> GenerateClientList() =>
                Enumerable.Range(1, 10).Select(i =>
                {
                    var client = Substitute.For<IThreadSafeClient>();
                    client.Id.Returns(i);
                    client.Name.Returns(i.ToString());
                    return client;
                }).ToList();
        }

        public sealed class TheConstructor : SelectClientViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(bool useDataSource, bool useNavigationService)
            {
                var dataSource = useDataSource ? DataSource : null;
                var navigationService = useNavigationService ? NavigationService : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new SelectClientViewModel(dataSource, navigationService);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheInitializeMethod : SelectClientViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task AddsAllClientsToTheListOfSuggestions()
            {
                var clients = GenerateClientList();
                DataSource.Clients.GetAllInWorkspace(Arg.Any<long>())
                    .Returns(Observable.Return(clients));
                ViewModel.Prepare(Parameters);

                await ViewModel.Initialize();

                ViewModel.Suggestions.Should().HaveCount(11);
            }

            [Fact, LogIfTooSlow]
            public async Task AddsANoClientSuggestion()
            {
                var clients = GenerateClientList();
                DataSource.Clients.GetAllInWorkspace(Arg.Any<long>())
                    .Returns(Observable.Return(clients));
                ViewModel.Prepare(Parameters);

                await ViewModel.Initialize();

                ViewModel.Suggestions.First().Name.Should().Be(Resources.NoClient);
            }

            [Fact, LogIfTooSlow]
            public async Task SetsNoClientAsSelectedIfTheParameterDoesNotSpecifyTheCurrentClient()
            {
                var clients = GenerateClientList();
                DataSource.Clients.GetAllInWorkspace(Arg.Any<long>())
                    .Returns(Observable.Return(clients));
                ViewModel.Prepare(Parameters);

                await ViewModel.Initialize();

                ViewModel.Suggestions.Single(c => c.Selected).Name.Should().Be(Resources.NoClient);
            }

            [Theory, LogIfTooSlow]
            [InlineData(1)]
            [InlineData(2)]
            [InlineData(3)]
            [InlineData(4)]
            [InlineData(5)]
            [InlineData(6)]
            [InlineData(7)]
            [InlineData(8)]
            [InlineData(9)]
            public async Task SetsTheAppropriateClientAsTheCurrentlySelectedOne(int id)
            {
                var parameter = SelectClientParameters.WithIds(10, id);
                var clients = GenerateClientList();
                DataSource.Clients.GetAllInWorkspace(Arg.Any<long>())
                    .Returns(Observable.Return(clients));
                ViewModel.Prepare(parameter);

                await ViewModel.Initialize();

                ViewModel.Suggestions.Single(c => c.Selected).Name.Should().Be(id.ToString());
            }
        }

        public sealed class TheCloseCommand : SelectClientViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModel()
            {
                await ViewModel.Initialize();

                await ViewModel.CloseCommand.ExecuteAsync();

                await NavigationService.Received()
                    .Close(Arg.Is(ViewModel), Arg.Any<long?>());
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsNull()
            {
                await ViewModel.Initialize();

                ViewModel.CloseCommand.ExecuteAsync().Wait();

                await NavigationService.Received()
                    .Close(Arg.Is(ViewModel), null);
            }
        }

        public sealed class TheSelectClientCommand : SelectClientViewModelTest
        {
            private const string clientName = "9";
            private readonly IDatabaseClient Client = Substitute.For<IDatabaseClient>();

            public TheSelectClientCommand()
            {
                var clients = GenerateClientList();
                DataSource.Clients.GetAllInWorkspace(Arg.Any<long>())
                    .Returns(Observable.Return(clients));
                ViewModel.Prepare(Parameters);
            }

            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModel()
            {
                await ViewModel.Initialize();

                ViewModel.SelectClientCommand.Execute(clientName);

                await NavigationService.Received()
                    .Close(Arg.Is(ViewModel), Arg.Any<long?>());
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsTheSelectedClientId()
            {
                const long expectedId = 9;
                await ViewModel.Initialize();

                ViewModel.SelectClientCommand.Execute(clientName);

                await NavigationService.Received().Close(
                    Arg.Is(ViewModel),
                    Arg.Is<long?>(expectedId)
                );
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsZeroWhenNoClientIsSelected()
            {
                await ViewModel.Initialize();

                ViewModel.SelectClientCommand.Execute(Resources.NoClient);

                await NavigationService.Received().Close(
                    Arg.Is(ViewModel),
                    Arg.Is<long?>(0)
                );
            }
        }

        public sealed class TheTextProperty : SelectClientViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task FiltersTheSuggestionsWhenItChanges()
            {
                var clients = GenerateClientList();
                DataSource.Clients.GetAllInWorkspace(Arg.Any<long>())
                    .Returns(Observable.Return(clients));
                ViewModel.Prepare(Parameters);
                await ViewModel.Initialize();

                ViewModel.Text = "0";

                ViewModel.Suggestions.Should().HaveCount(1);
            }
        }

        public sealed class TheSuggestCreationProperty : SelectClientViewModelTest
        {
            private const string name = "My client";

            public TheSuggestCreationProperty()
            {
                var client = Substitute.For<IThreadSafeClient>();
                client.Name.Returns(name);
                DataSource.Clients
                    .GetAllInWorkspace(Arg.Any<long>())
                    .Returns(Observable.Return(new List<IThreadSafeClient> { client }));
                ViewModel.Prepare(Parameters);
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsFalseIfTheTextIsEmpty()
            {
                await ViewModel.Initialize();

                ViewModel.Text = "";

                ViewModel.SuggestCreation.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsFalseIfTheTextIsOnlyWhitespace()
            {
                await ViewModel.Initialize();

                ViewModel.Text = "       ";

                ViewModel.SuggestCreation.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsFalseIfTheTextMatchesTheNameOfAnExistingProject()
            {
                await ViewModel.Initialize();

                ViewModel.Text = name;

                ViewModel.SuggestCreation.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsFalseIfTheTextIsLongerThanTwoHundredAndFiftyCharacters()
            {
                await ViewModel.Initialize();

                ViewModel.Text = "Some absurdly long project name created solely for making sure that the SuggestCreation property returns false when the project name is longer than the previously specified threshold so that the mobile apps behave and avoid crashes in backend and even bigger problems.";

                ViewModel.SuggestCreation.Should().BeFalse();
            }
        }

        public sealed class TheCreateClientCommand : SelectClientViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task CreatesANewClientWithTheGivenNameInTheCurrentWorkspace()
            {
                long workspaceId = 10;
                await ViewModel.Initialize();
                ViewModel.Prepare(Parameters);
                ViewModel.Text = "Some name of the client";

                await ViewModel.CreateClientCommand.ExecuteAsync();

                await DataSource.Clients.Received().Create(Arg.Is(ViewModel.Text), Arg.Is(workspaceId));
            }

            [Theory, LogIfTooSlow]
            [InlineData("   abcde", "abcde")]
            [InlineData("abcde     ", "abcde")]
            [InlineData("  abcde ", "abcde")]
            [InlineData("abcde  fgh", "abcde  fgh")]
            [InlineData("      abcd\nefgh     ", "abcd\nefgh")]
            public async Task TrimsNameFromTheStartAndTheEndBeforeSaving(string name, string trimmed)
            {
                await ViewModel.Initialize();
                ViewModel.Text = name;

                await ViewModel.CreateClientCommand.ExecuteAsync();

                await DataSource.Clients.Received().Create(Arg.Is(trimmed), Arg.Any<long>());
            }

            [Theory, LogIfTooSlow]
            [InlineData(" ")]
            [InlineData("\t")]
            [InlineData("\n")]
            [InlineData("               ")]
            [InlineData("      \t  \n     ")]
            public async Task DoesNotSuggestCreatingClientsWhenTheDescriptionConsistsOfOnlyWhiteCharacters(string name)
            {
                await ViewModel.Initialize();

                ViewModel.Text = name;

                ViewModel.SuggestCreation.Should().BeFalse();
            }

            [Theory, LogIfTooSlow]
            [InlineData(" ")]
            [InlineData("\t")]
            [InlineData("\n")]
            [InlineData("               ")]
            [InlineData("      \t  \n     ")]
            public async Task DoesNotAllowCreatingClientsWhenTheDescriptionConsistsOfOnlyWhiteCharacters(string name)
            {
                await ViewModel.Initialize();
                ViewModel.Text = name;

                await ViewModel.CreateClientCommand.ExecuteAsync();

                await DataSource.Clients.DidNotReceiveWithAnyArgs().Create(null, 0);
            }
        }
    }
}