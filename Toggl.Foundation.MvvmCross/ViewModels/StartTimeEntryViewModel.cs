using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using Toggl.Foundation;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Autocomplete;
using Toggl.Foundation.Autocomplete.Span;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Settings;
using static Toggl.Foundation.Helper.Constants;
using static Toggl.Multivac.Extensions.CommonFunctions;
using SelectTimeOrigin = Toggl.Foundation.MvvmCross.Parameters.SelectTimeParameters.Origin;

[assembly: MvxNavigation(typeof(StartTimeEntryViewModel), ApplicationUrls.StartTimeEntry)]
namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class StartTimeEntryViewModel : MvxViewModel<StartTimeEntryParameters>, ITimeEntryPrototype
    {
        //Fields
        private readonly ITimeService timeService;
        private readonly ITogglDataSource dataSource;
        private readonly IDialogService dialogService;
        private readonly IUserPreferences userPreferences;
        private readonly IInteractorFactory interactorFactory;
        private readonly IMvxNavigationService navigationService;
        private readonly IAnalyticsService analyticsService;
        private readonly IAutocompleteProvider autocompleteProvider;

        private readonly CompositeDisposable disposeBag = new CompositeDisposable();
        private readonly ISubject<TextFieldInfo> uiSubject = new Subject<TextFieldInfo>();
        private readonly ISubject<TextFieldInfo> querySubject = new Subject<TextFieldInfo>();
        private readonly ISubject<AutocompleteSuggestionType> queryByTypeSubject = new Subject<AutocompleteSuggestionType>();

        private bool hasAnyTags;
        private bool hasAnyProjects;
        private long defaultWorkspaceId;
        private StartTimeEntryParameters parameter;
        private TextFieldInfo textFieldInfo = TextFieldInfo.Empty(0);

        //Properties
        public IObservable<TextFieldInfo> TextFieldInfoObservable { get; }

        private bool isRunning => !Duration.HasValue;

        private int DescriptionByteCount
            => textFieldInfo.Description.LengthInBytes();

        public int DescriptionRemainingBytes
            => MaxTimeEntryDescriptionLengthInBytes - DescriptionByteCount;

        public bool DescriptionLengthExceeded
            => DescriptionByteCount > MaxTimeEntryDescriptionLengthInBytes;

        public bool SuggestCreation
        {
            get
            {
                if (IsSuggestingProjects && textFieldInfo.HasProject) return false;

                if (string.IsNullOrEmpty(CurrentQuery))
                    return false;

                if (IsSuggestingProjects)
                    return Suggestions.None(c => c.Any(s => s is ProjectSuggestion pS && pS.ProjectName == CurrentQuery))
                           && CurrentQuery.LengthInBytes() <= MaxProjectNameLengthInBytes;

                if (IsSuggestingTags)
                    return Suggestions.None(c => c.Any(s => s is TagSuggestion tS && tS.Name == CurrentQuery))
                           && CurrentQuery.LengthInBytes() <= MaxTagNameLengthInBytes;

                return false;
            }
        }

        public long[] TagIds => textFieldInfo.Spans.OfType<TagSpan>().Select(span => span.TagId).Distinct().ToArray();

        public long? ProjectId => textFieldInfo.Spans.OfType<ProjectSpan>().SingleOrDefault()?.ProjectId;

        public long? TaskId => textFieldInfo.Spans.OfType<ProjectSpan>().SingleOrDefault()?.TaskId;

        public string Description => textFieldInfo.Description;

        public long WorkspaceId => textFieldInfo.WorkspaceId;

        public bool IsDirty
            => !string.IsNullOrEmpty(textFieldInfo.Description)
                || textFieldInfo.Spans.Any(s => s is ProjectSpan || s is TagSpan)
                || IsBillable
                || StartTime != parameter.StartTime
                || Duration != parameter.Duration;

        public bool UseGrouping { get; set; }

        public string CurrentQuery { get; private set; }

        public bool IsEditingTime { get; private set; }

        public bool IsSuggestingTags { get; private set; }

        public bool IsSuggestingProjects { get; private set; }

        private TimeSpan displayedTime = TimeSpan.Zero;

        public TimeSpan DisplayedTime
        {
            get => displayedTime;
            set
            {
                if (isRunning)
                {
                    StartTime = timeService.CurrentDateTime - value;
                }
                else
                {
                    Duration = value;
                }

                displayedTime = value;

                RaisePropertyChanged();
            }
        }

        public DurationFormat DisplayedTimeFormat { get; } = DurationFormat.Improved;

        public bool IsBillable { get; private set; } = false;

        public bool IsBillableAvailable { get; private set; } = false;

        public bool IsEditingProjects { get; private set; } = false;

        public bool IsEditingTags { get; private set; } = false;

        public string PlaceholderText { get; private set; }

        public bool ShouldShowNoTagsInfoMessage
            => IsSuggestingTags && !hasAnyTags;

        public bool ShouldShowNoProjectsInfoMessage
            => IsSuggestingProjects && !hasAnyProjects;

        public DateTimeOffset StartTime { get; private set; }

        public TimeSpan? Duration { get; private set; }

        public NestableObservableCollection<WorkspaceGroupedCollection<AutocompleteSuggestion>, AutocompleteSuggestion> Suggestions { get; }
            = new NestableObservableCollection<WorkspaceGroupedCollection<AutocompleteSuggestion>, AutocompleteSuggestion>();

        public ITogglDataSource DataSource => dataSource;

        public IMvxAsyncCommand BackCommand { get; }

        public IMvxAsyncCommand DoneCommand { get; }

        public IMvxAsyncCommand<SelectTimeOrigin> SelectTimeCommand { get; }

        public IMvxAsyncCommand SetStartDateCommand { get; }

        public IMvxAsyncCommand ChangeTimeCommand { get; }

        public IMvxCommand ToggleBillableCommand { get; }

        public IMvxCommand DurationTapped { get; }

        public IMvxAsyncCommand CreateCommand { get; }

        public IMvxCommand ToggleTagSuggestionsCommand { get; }

        public IMvxCommand ToggleProjectSuggestionsCommand { get; }

        public IMvxAsyncCommand<AutocompleteSuggestion> SelectSuggestionCommand { get; }

        public IMvxCommand<ProjectSuggestion> ToggleTaskSuggestionsCommand { get; }

        public IOnboardingStorage OnboardingStorage { get; }

        public StartTimeEntryViewModel(
            ITimeService timeService,
            ITogglDataSource dataSource,
            IDialogService dialogService,
            IUserPreferences userPreferences,
            IOnboardingStorage onboardingStorage,
            IInteractorFactory interactorFactory,
            IMvxNavigationService navigationService,
            IAnalyticsService analyticsService,
            IAutocompleteProvider autocompleteProvider
        )
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(dialogService, nameof(dialogService));
            Ensure.Argument.IsNotNull(userPreferences, nameof(userPreferences));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(onboardingStorage, nameof(onboardingStorage));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(autocompleteProvider, nameof(autocompleteProvider));

            this.dataSource = dataSource;
            this.timeService = timeService;
            this.dialogService = dialogService;
            this.userPreferences = userPreferences;
            this.navigationService = navigationService;
            this.interactorFactory = interactorFactory;
            this.analyticsService = analyticsService;
            this.autocompleteProvider = autocompleteProvider;

            OnboardingStorage = onboardingStorage;

            TextFieldInfoObservable = uiSubject.AsObservable();

            BackCommand = new MvxAsyncCommand(back);
            DoneCommand = new MvxAsyncCommand(done);
            CreateCommand = new MvxAsyncCommand(create);
            DurationTapped = new MvxCommand(durationTapped);
            ChangeTimeCommand = new MvxAsyncCommand(changeTime);
            ToggleBillableCommand = new MvxCommand(toggleBillable);
            SetStartDateCommand = new MvxAsyncCommand(setStartDate);
            SelectTimeCommand = new MvxAsyncCommand<SelectTimeOrigin>(selectTime);
            ToggleTagSuggestionsCommand = new MvxCommand(toggleTagSuggestions);
            ToggleProjectSuggestionsCommand = new MvxCommand(toggleProjectSuggestions);
            SelectSuggestionCommand = new MvxAsyncCommand<AutocompleteSuggestion>(selectSuggestion);
            ToggleTaskSuggestionsCommand = new MvxCommand<ProjectSuggestion>(toggleTaskSuggestions);
        }

        public void Init()
        {
            var now = timeService.CurrentDateTime;
            var startTimeEntryParameters = userPreferences.IsManualModeEnabled
                ? StartTimeEntryParameters.ForManualMode(now)
                : StartTimeEntryParameters.ForTimerMode(now);
            Prepare(startTimeEntryParameters);
        }

        public override void Prepare()
        {
            var queryByTypeObservable = queryByTypeSubject
                .AsObservable()
                .SelectMany(type => autocompleteProvider.Query(new QueryInfo("", type)));

            querySubject.AsObservable()
                .StartWith(textFieldInfo)
                .Select(QueryInfo.ParseFieldInfo)
                .Do(onParsedQuery)
                .SelectMany(autocompleteProvider.Query)
                .Merge(queryByTypeObservable)
                .Subscribe(onSuggestions)
                .DisposedBy(disposeBag);
        }

        public override void Prepare(StartTimeEntryParameters parameter)
        {
            this.parameter = parameter;
            StartTime = parameter.StartTime;
            Duration = parameter.Duration;

            PlaceholderText = parameter.PlaceholderText;

            timeService.CurrentDateTimeObservable
                .Where(_ => isRunning)
                .Subscribe(currentTime => DisplayedTime = currentTime - StartTime)
                .DisposedBy(disposeBag);
        }

        public override async Task Initialize()
        {
            await base.Initialize();

            var workspace = await interactorFactory.GetDefaultWorkspace().Execute();
            defaultWorkspaceId = workspace.Id;

            textFieldInfo = TextFieldInfo.Empty(workspace.Id);

            await setBillableValues(textFieldInfo.ProjectId);

            hasAnyTags = (await dataSource.Tags.GetAll()).Any();
            hasAnyProjects = (await dataSource.Projects.GetAll()).Any();
        }

        public override void ViewDestroy(bool viewFinishing)
        {
            base.ViewDestroy(viewFinishing);
            disposeBag?.Dispose();
        }

        public async Task OnTextFieldInfoFromView(IImmutableList<ISpan> spans)
        {
            queryWith(textFieldInfo.ReplaceSpans(spans));
            await setBillableValues(textFieldInfo.ProjectId);
        }

        private async Task selectSuggestion(AutocompleteSuggestion suggestion)
        {
            switch (suggestion)
            {
                case QuerySymbolSuggestion querySymbolSuggestion:
                    
                    if (querySymbolSuggestion.Symbol == QuerySymbols.ProjectsString)
                    {
                        analyticsService.StartViewTapped.Track(StartViewTapSource.PickEmptyStateProjectSuggestion);
                        analyticsService.StartEntrySelectProject.Track(ProjectTagSuggestionSource.TableCellButton);
                    }
                    else if (querySymbolSuggestion.Symbol == QuerySymbols.TagsString)
                    {
                        analyticsService.StartViewTapped.Track(StartViewTapSource.PickEmptyStateTagSuggestion);
                        analyticsService.StartEntrySelectTag.Track(ProjectTagSuggestionSource.TableCellButton);
                    }

                    updateUiWith(textFieldInfo.FromQuerySymbolSuggestion(querySymbolSuggestion));
                    break;

                case TimeEntrySuggestion timeEntrySuggestion:
                    analyticsService.StartViewTapped.Track(StartViewTapSource.PickTimeEntrySuggestion);
                    updateUiWith(textFieldInfo.FromTimeEntrySuggestion(timeEntrySuggestion));
                    await setBillableValues(timeEntrySuggestion.ProjectId);
                    break;

                case ProjectSuggestion projectSuggestion:
                    analyticsService.StartViewTapped.Track(StartViewTapSource.PickProjectSuggestion);

                    if (textFieldInfo.WorkspaceId != projectSuggestion.WorkspaceId
                        && await workspaceChangeDenied())
                        return;

                    IsSuggestingProjects = false;
                    updateUiWith(textFieldInfo.FromProjectSuggestion(projectSuggestion));
                    await setBillableValues(projectSuggestion.ProjectId);
                    queryByTypeSubject.OnNext(AutocompleteSuggestionType.None);

                    break;

                case TaskSuggestion taskSuggestion:
                    analyticsService.StartViewTapped.Track(StartViewTapSource.PickTaskSuggestion);

                    if (textFieldInfo.WorkspaceId != taskSuggestion.WorkspaceId
                        && await workspaceChangeDenied())
                        return;

                    IsSuggestingProjects = false;
                    updateUiWith(textFieldInfo.FromTaskSuggestion(taskSuggestion));
                    await setBillableValues(taskSuggestion.ProjectId);
                    queryByTypeSubject.OnNext(AutocompleteSuggestionType.None);
                    break;

                case TagSuggestion tagSuggestion:
                    analyticsService.StartViewTapped.Track(StartViewTapSource.PickTagSuggestion);
                    updateUiWith(textFieldInfo.FromTagSuggestion(tagSuggestion));
                    break;

                default:
                    return;
            }

            IObservable<bool> workspaceChangeDenied()
                => dialogService.Confirm(
                    Resources.DifferentWorkspaceAlertTitle,
                    Resources.DifferentWorkspaceAlertMessage,
                    Resources.Ok,
                    Resources.Cancel
                ).Select(Invert);
        }


        private Task create()
            => IsSuggestingProjects ? createProject() : createTag();

        private async Task createProject()
        {
            var projectId = await navigationService.Navigate<EditProjectViewModel, string, long?>(CurrentQuery);
            if (projectId == null) return;

            var project = await dataSource.Projects.GetById(projectId.Value);
            var projectSuggestion = new ProjectSuggestion(project);

            updateUiWith(textFieldInfo.FromProjectSuggestion(projectSuggestion));
            IsSuggestingProjects = false;
            queryByTypeSubject.OnNext(AutocompleteSuggestionType.None);
            hasAnyProjects = true;
        }

        private async Task createTag()
        {
            var createdTag = await dataSource.Tags.Create(CurrentQuery, textFieldInfo.WorkspaceId);
            var tagSuggestion = new TagSuggestion(createdTag);
            await SelectSuggestionCommand.ExecuteAsync(tagSuggestion);
            hasAnyTags = true;
        }

        private void OnDurationChanged()
        {
            if (Duration == null)
                return;

            DisplayedTime = Duration.Value;
        }

        private void durationTapped()
        {
            analyticsService.StartViewTapped.Track(StartViewTapSource.Duration);
        }

        private void toggleTagSuggestions()
        {
            if (IsSuggestingTags)
            {
                updateUiWith(textFieldInfo.RemoveTagQueryIfNeeded());
                IsSuggestingTags = false;
                return;
            }

            analyticsService.StartViewTapped.Track(StartViewTapSource.Tags);
            analyticsService.StartEntrySelectTag.Track(ProjectTagSuggestionSource.ButtonOverKeyboard);
            OnboardingStorage.ProjectOrTagWasAdded();

            queryAndUpdateUiWith(textFieldInfo.AddQuerySymbol(QuerySymbols.TagsString));
        }

        private void toggleProjectSuggestions()
        {
            if (IsSuggestingProjects)
            {
                IsSuggestingProjects = false;
                updateUiWith(textFieldInfo.RemoveProjectQueryIfNeeded());
                queryByTypeSubject.OnNext(AutocompleteSuggestionType.None);
                return;
            }

            analyticsService.StartViewTapped.Track(StartViewTapSource.Project);
            analyticsService.StartEntrySelectProject.Track(ProjectTagSuggestionSource.ButtonOverKeyboard);
            OnboardingStorage.ProjectOrTagWasAdded();

            if (textFieldInfo.HasProject)
            {
                IsSuggestingProjects = true;
                queryByTypeSubject.OnNext(AutocompleteSuggestionType.Projects);
                return;
            }

            queryAndUpdateUiWith(
                textFieldInfo.AddQuerySymbol(QuerySymbols.ProjectsString)
            );
        }

        private void toggleTaskSuggestions(ProjectSuggestion projectSuggestion)
        {
            var grouping = Suggestions.FirstOrDefault(s => s.WorkspaceId == projectSuggestion.WorkspaceId);
            if (grouping == null) return;

            var suggestionIndex = grouping.IndexOf(projectSuggestion);
            if (suggestionIndex < 0) return;

            projectSuggestion.TasksVisible = !projectSuggestion.TasksVisible;

            var groupingIndex = Suggestions.IndexOf(grouping);
            Suggestions.Remove(grouping);
            Suggestions.Insert(groupingIndex,
                new WorkspaceGroupedCollection<AutocompleteSuggestion>(
                    grouping.WorkspaceName, grouping.WorkspaceId, getSuggestionsWithTasks(grouping)
                )
            );
        }

        private void toggleBillable()
        {
            analyticsService.StartViewTapped.Track(StartViewTapSource.Billable);
            IsBillable = !IsBillable;
        }

        private StartViewTapSource? getTapSourceFromBindingParameter(SelectTimeOrigin origin)
        {
            switch (origin)
            {
                case SelectTimeOrigin.StartTime:
                    return StartViewTapSource.StartTime;
                case SelectTimeOrigin.StartDate:
                    return StartViewTapSource.StartDate;
                case SelectTimeOrigin.Duration:
                    return StartViewTapSource.Duration;
                default:
                    return null;
            }
        }

        private async Task selectTime(SelectTimeOrigin origin)
        {
            if (getTapSourceFromBindingParameter(origin) is StartViewTapSource tapSource)
                analyticsService.StartViewTapped.Track(tapSource);

            IsEditingTime = true;

            var stopTime = Duration.HasValue ? (DateTimeOffset?)StartTime + Duration.Value : null;

            var preferences = await dataSource.Preferences.Current.FirstAsync();

            var parameters = SelectTimeParameters.CreateFromOrigin(origin, StartTime, stopTime)
                .WithFormats(preferences.DateFormat, preferences.TimeOfDayFormat);

            var result = await navigationService
                .Navigate<SelectTimeViewModel, SelectTimeParameters, SelectTimeResultsParameters>(parameters)
                .ConfigureAwait(false);

            if (result == null)
                return;

            StartTime = result.Start;

            if (result.Stop.HasValue)
            {
                Duration = result.Stop - result.Start;
            }

            IsEditingTime = false;
        }

        private async Task changeTime()
        {
            analyticsService.StartViewTapped.Track(StartViewTapSource.StartTime);

            IsEditingTime = true;

            var currentDuration = DurationParameter.WithStartAndDuration(StartTime, Duration);

            var selectedDuration = await navigationService
                .Navigate<EditDurationViewModel, EditDurationParameters, DurationParameter>(new EditDurationParameters(currentDuration, isStartingNewEntry: true))
                .ConfigureAwait(false);

            StartTime = selectedDuration.Start;
            Duration = selectedDuration.Duration ?? Duration;

            IsEditingTime = false;
        }

        private async Task setStartDate()
        {
            analyticsService.StartViewTapped.Track(StartViewTapSource.StartDate);

            var parameters = isRunning
                ? DateTimePickerParameters.ForStartDateOfRunningTimeEntry(StartTime, timeService.CurrentDateTime)
                : DateTimePickerParameters.ForStartDateOfStoppedTimeEntry(StartTime);

            var duration = Duration;

            StartTime = await navigationService
                .Navigate<SelectDateTimeViewModel, DateTimePickerParameters, DateTimeOffset>(parameters)
                .ConfigureAwait(false);

            if (isRunning == false)
            {
                Duration = duration;
            }
        }

        private async Task back()
        {
            if (IsDirty)
            {
                var shouldDiscard = await dialogService.ConfirmDestructiveAction(ActionType.DiscardNewTimeEntry);
                if (!shouldDiscard)
                    return;
            }

            await navigationService.Close(this);
        }

        private async Task done()
        {
            await interactorFactory.CreateTimeEntry(this).Execute();
            await navigationService.Close(this);
        }

        private void onParsedQuery(QueryInfo parsedQuery)
        {
            CurrentQuery = parsedQuery.Text?.Trim() ?? "";
            bool suggestsTags = parsedQuery.SuggestionType == AutocompleteSuggestionType.Tags;
            bool suggestsProjects = parsedQuery.SuggestionType == AutocompleteSuggestionType.Projects;

            if (!IsSuggestingTags && suggestsTags)
            {
                analyticsService.StartEntrySelectTag.Track(ProjectTagSuggestionSource.TextField);
            }

            if (!IsSuggestingProjects && suggestsProjects)
            {
                analyticsService.StartEntrySelectProject.Track(ProjectTagSuggestionSource.TextField);
            }

            IsSuggestingTags = suggestsTags;
            IsSuggestingProjects = suggestsProjects;
        }

        private void onSuggestions(IEnumerable<AutocompleteSuggestion> suggestions)
        {
            Suggestions.Clear();

            var filteredSuggestions = filterSuggestions(suggestions);
            var groupedSuggestions = groupSuggestions(filteredSuggestions).ToList();

            UseGrouping = groupedSuggestions.Count > 1;
            Suggestions.AddRange(groupedSuggestions);

            RaisePropertyChanged(nameof(SuggestCreation));
        }

        private IEnumerable<AutocompleteSuggestion> filterSuggestions(IEnumerable<AutocompleteSuggestion> suggestions)
        {
            if (textFieldInfo.HasProject && !IsSuggestingProjects && !IsSuggestingTags)
            {
                var projectId = textFieldInfo.Spans.OfType<ProjectSpan>().Single().ProjectId;
                
                return suggestions.OfType<TimeEntrySuggestion>()
                    .Where(suggestion => suggestion.ProjectId == projectId);
            }

            return suggestions;
        }

        private IEnumerable<WorkspaceGroupedCollection<AutocompleteSuggestion>> groupSuggestions(
            IEnumerable<AutocompleteSuggestion> suggestions)
        {
            var firstSuggestion = suggestions.FirstOrDefault();
            if (firstSuggestion is ProjectSuggestion)
                return suggestions
                    .GroupByWorkspaceAddingNoProject()
                    .OrderByDefaultWorkspaceAndName(defaultWorkspaceId);

            if (IsSuggestingTags)
                suggestions = suggestions.Where(suggestion => suggestion.WorkspaceId == textFieldInfo.WorkspaceId);

            return suggestions
                .GroupBy(suggestion => new { suggestion.WorkspaceName, suggestion.WorkspaceId })
                .Select(grouping => new WorkspaceGroupedCollection<AutocompleteSuggestion>(
                    grouping.Key.WorkspaceName, grouping.Key.WorkspaceId, grouping.Distinct(AutocompleteSuggestionComparer.Instance)));
        }

        private async Task setBillableValues(long? currentProjectId)
        {
            var hasProject = currentProjectId.HasValue && currentProjectId.Value != ProjectSuggestion.NoProjectId;
            if (hasProject)
            {
                var projectId = currentProjectId.Value;
                IsBillableAvailable =
                    await interactorFactory.IsBillableAvailableForProject(projectId).Execute();

                IsBillable = IsBillableAvailable && await interactorFactory.ProjectDefaultsToBillable(projectId).Execute();
            }
            else
            {
                IsBillable = false;
                IsBillableAvailable = await interactorFactory.IsBillableAvailableForWorkspace(WorkspaceId).Execute();
            }
        }

        private IEnumerable<AutocompleteSuggestion> getSuggestionsWithTasks(
            IEnumerable<AutocompleteSuggestion> suggestions)
        {
            foreach (var suggestion in suggestions)
            {
                if (suggestion is TaskSuggestion) continue;

                yield return suggestion;

                if (suggestion is ProjectSuggestion projectSuggestion && projectSuggestion.TasksVisible)
                    foreach (var taskSuggestion in projectSuggestion.Tasks)
                        yield return taskSuggestion;
            }
        }

        private void queryWith(TextFieldInfo newTextFieldinfo)
        {
            textFieldInfo = newTextFieldinfo;
            querySubject.OnNext(textFieldInfo);
        }

        private void updateUiWith(TextFieldInfo newTextFieldinfo)
        {
            textFieldInfo = newTextFieldinfo;
            uiSubject.OnNext(textFieldInfo);
        }

        private void queryAndUpdateUiWith(TextFieldInfo newTextFieldinfo)
        {
            textFieldInfo = newTextFieldinfo;
            uiSubject.OnNext(textFieldInfo);
            querySubject.OnNext(textFieldInfo);
        }
    }
}
