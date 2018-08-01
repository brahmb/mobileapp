using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Suggestions;
using Toggl.Foundation.Tests.Generators;
using Toggl.PrimeRadiant.Models;
using Xunit;
using TimeEntry = Toggl.Foundation.Models.TimeEntry;
using ITimeEntryPrototype = Toggl.Foundation.Models.ITimeEntryPrototype;
using Toggl.Foundation.Models.Interfaces;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class SuggestionsViewModelTests
    {
        public abstract class SuggestionsViewModelTest : BaseViewModelTests<SuggestionsViewModel>
        {
            protected TestScheduler Scheduler { get; } = new TestScheduler();

            protected ISuggestionProviderContainer Container { get; } = Substitute.For<ISuggestionProviderContainer>();

            protected override SuggestionsViewModel CreateViewModel()
                => new SuggestionsViewModel(DataSource, InteractorFactory, OnboardingStorage, Container);

            protected void SetProviders(params ISuggestionProvider[] providers)
            {
                Container.Providers.Returns(providers.ToList().AsReadOnly());
            }
        }

        public sealed class TheConstructor : SuggestionsViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useDataSource,
                bool useContainer,
                bool useOnboardingStorage,
                bool useInteractorFactory)
            {
                var container = useContainer ? Container : null;
                var dataSource = useDataSource ? DataSource : null;
                var onboardingStorage = useOnboardingStorage ? OnboardingStorage : null;
                var interactorFactory = useInteractorFactory ? InteractorFactory : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new SuggestionsViewModel(dataSource, interactorFactory, onboardingStorage, container);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheSuggestionsProperty : SuggestionsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task WorksWithSeveralProviders()
            {
                var provider1 = Substitute.For<ISuggestionProvider>();
                var provider2 = Substitute.For<ISuggestionProvider>();
                var suggestion1 = createSuggestion("t1", 12, 9);
                var suggestion2 = createSuggestion("t2", 9, 12);
                var observable1 = Scheduler.CreateColdObservable(createRecorded(0, suggestion1));
                var observable2 = Scheduler.CreateColdObservable(createRecorded(1, suggestion2));
                provider1.GetSuggestions().Returns(observable1);
                provider2.GetSuggestions().Returns(observable2);
                SetProviders(provider1, provider2);

                await ViewModel.Initialize();
                Scheduler.AdvanceTo(1);

                ViewModel.Suggestions.Should().HaveCount(2)
                         .And.Contain(new[] { suggestion1, suggestion2 });
            }

            [Fact, LogIfTooSlow]
            public async Task WorksIfProviderHasMultipleSuggestions()
            {
                var scheduler = new TestScheduler();
                var provider = Substitute.For<ISuggestionProvider>();
                var suggestions = Enumerable.Range(1, 3).Select(createSuggestion).ToArray();
                var observableContent = suggestions
                    .Select(suggestion => createRecorded(1, suggestion))
                    .ToArray();
                var observable = scheduler.CreateColdObservable(observableContent);
                provider.GetSuggestions().Returns(observable);
                SetProviders(provider);

                await ViewModel.Initialize();
                scheduler.AdvanceTo(1);

                ViewModel.Suggestions.Should().HaveCount(suggestions.Length)
                         .And.Contain(suggestions);
            }

            [Fact, LogIfTooSlow]
            public async Task WorksIfProvidersAreEmpty()
            {
                var providers = Enumerable.Range(0, 3)
                    .Select(_ => Substitute.For<ISuggestionProvider>()).ToArray();

                foreach (var provider in providers)
                    provider.GetSuggestions().Returns(Observable.Empty<Suggestion>());

                SetProviders(providers);

                await ViewModel.Initialize();

                ViewModel.Suggestions.Should().HaveCount(0);
            }

            private Suggestion createSuggestion(int index) => createSuggestion($"te{index}", 0, 0);

            private Suggestion createSuggestion(string description, long taskId, long projectId) => new Suggestion(
                TimeEntry.Builder.Create(0)
                    .SetDescription(description)
                    .SetStart(DateTimeOffset.UtcNow)
                    .SetAt(DateTimeOffset.UtcNow)
                    .SetTaskId(taskId)
                    .SetProjectId(projectId)
                    .SetWorkspaceId(11)
                    .SetUserId(12)
                    .Build()
            );

            private Recorded<Notification<Suggestion>> createRecorded(int ticks, Suggestion suggestion)
                => new Recorded<Notification<Suggestion>>(ticks, Notification.CreateOnNext(suggestion));
        }

        public sealed class TheStartTimeEntryCommand : SuggestionsViewModelTest
        {
            public TheStartTimeEntryCommand()
            {
                var user = Substitute.For<IThreadSafeUser>();
                user.Id.Returns(10);
                DataSource.User.Current.Returns(Observable.Return(user));

                TimeService.CurrentDateTime.Returns(DateTimeOffset.Now);
            }

            [Fact, LogIfTooSlow]
            public async Task CallsTheCreateTimeEntryInteractor()
            {
                var suggestion = createSuggestion();

                await ViewModel.StartTimeEntryCommand.ExecuteAsync(suggestion);

                InteractorFactory.Received().StartSuggestion(suggestion);
            }

            [Fact, LogIfTooSlow]
            public async Task ExecutesTheContinueTimeEntryInteractor()
            {
                var suggestion = createSuggestion();
                var mockedInteractor = Substitute.For<IInteractor<IObservable<IThreadSafeTimeEntry>>>();
                InteractorFactory.StartSuggestion(Arg.Any<Suggestion>()).Returns(mockedInteractor);

                await ViewModel.StartTimeEntryCommand.ExecuteAsync(suggestion);

                await mockedInteractor.Received().Execute();
            }

            [Fact, LogIfTooSlow]
            public void CannotBeExecutedTwiceInARow()
            {
                var suggestion = createSuggestion();
                var mockedInteractor = Substitute.For<IInteractor<IObservable<IThreadSafeTimeEntry>>>();
                InteractorFactory.StartSuggestion(Arg.Any<Suggestion>()).Returns(mockedInteractor);
                mockedInteractor.Execute()
                    .Returns(Observable.Never<IThreadSafeTimeEntry>());

                ViewModel.StartTimeEntryCommand.ExecuteAsync(suggestion);
                ViewModel.StartTimeEntryCommand.ExecuteAsync(suggestion);

                InteractorFactory.Received(1).StartSuggestion(suggestion);
            }

            [Fact, LogIfTooSlow]
            public async Task CanBeExecutedForTheSecondTimeIfStartingTheFirstOneFinishesSuccessfully()
            {
                var suggestion = createSuggestion();
                var timeEntry = Substitute.For<IThreadSafeTimeEntry>();
                var mockedInteractor = Substitute.For<IInteractor<IObservable<IThreadSafeTimeEntry>>>();
                InteractorFactory.StartSuggestion(Arg.Any<Suggestion>()).Returns(mockedInteractor);
                mockedInteractor.Execute()
                    .Returns(Observable.Return(timeEntry));

                await ViewModel.StartTimeEntryCommand.ExecuteAsync(suggestion);
                await ViewModel.StartTimeEntryCommand.ExecuteAsync(suggestion);

                InteractorFactory.Received(2).StartSuggestion(suggestion);
            }

            [Fact, LogIfTooSlow]
            public async Task MarksTheActionForOnboardingPurposes()
            {
                var suggestion = createSuggestion();

                await ViewModel.StartTimeEntryCommand.ExecuteAsync(suggestion);

                OnboardingStorage.Received().SetTimeEntryContinued();
            }

            private Suggestion createSuggestion()
            {
                var timeEntry = Substitute.For<IThreadSafeTimeEntry>();
                timeEntry.Duration.Returns((long)TimeSpan.FromMinutes(30).TotalSeconds);
                timeEntry.Description.Returns("Testing");
                timeEntry.WorkspaceId.Returns(10);
                return new Suggestion(timeEntry);
            }
        }
    }
}
