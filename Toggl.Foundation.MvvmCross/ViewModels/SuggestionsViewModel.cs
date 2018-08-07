using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Suggestions;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Settings;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class SuggestionsViewModel : MvxViewModel
    {
        private readonly ITogglDataSource dataSource;
        private readonly IInteractorFactory interactorFactory;
        private readonly IOnboardingStorage onboardingStorage;
        private readonly ISuggestionProviderContainer suggestionProviders;

        private bool areStartButtonsEnabled = true;
        private IDisposable emptyDatabaseDisposable;

        public MvxObservableCollection<Suggestion> Suggestions { get; }
            = new MvxObservableCollection<Suggestion>();

        public bool IsEmpty => Suggestions.None();

        public MvxAsyncCommand<Suggestion> StartTimeEntryCommand { get; set; }

        public SuggestionsViewModel(
            ITogglDataSource dataSource,
            IInteractorFactory interactorFactory,
            IOnboardingStorage onboardingStorage,
            ISuggestionProviderContainer suggestionProviders)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(suggestionProviders, nameof(suggestionProviders));
            Ensure.Argument.IsNotNull(onboardingStorage, nameof(onboardingStorage));

            this.dataSource = dataSource;
            this.interactorFactory = interactorFactory;
            this.onboardingStorage = onboardingStorage;
            this.suggestionProviders = suggestionProviders;

            StartTimeEntryCommand = new MvxAsyncCommand<Suggestion>(startTimeEntry, _ => areStartButtonsEnabled);
        }

        public async override Task Initialize()
        {
            await base.Initialize();

            emptyDatabaseDisposable = dataSource
                .TimeEntries
                .IsEmpty
                .FirstAsync()
                .Subscribe(fetchSuggestions);
        }

        private void fetchSuggestions(bool databaseIsEmpty)
        {
            Suggestions.Clear();

            if (databaseIsEmpty) return;

            suggestionProviders
                .Providers
                .Select(provider => provider.GetSuggestions())
                .Aggregate(Observable.Merge)
                .Subscribe(addSuggestion);
        }

        private void addSuggestion(Suggestion suggestion)
        {
            Suggestions.Add(suggestion);

            RaisePropertyChanged();
        }

        private async Task startTimeEntry(Suggestion suggestion)
        {
            areStartButtonsEnabled = false;
            StartTimeEntryCommand.RaiseCanExecuteChanged();

            onboardingStorage.SetTimeEntryContinued();

            await interactorFactory
                .StartSuggestion(suggestion)
                .Execute()
                .Do(_ =>
                {
                    areStartButtonsEnabled = true;
                    StartTimeEntryCommand.RaiseCanExecuteChanged();
                });
        }

        public override void ViewDestroy(bool viewFinishing = true)
        {
            base.ViewDestroy(viewFinishing);
            emptyDatabaseDisposable?.Dispose();
        }
    }
}
