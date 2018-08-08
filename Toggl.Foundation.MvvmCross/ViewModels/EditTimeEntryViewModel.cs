using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using PropertyChanged;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.DTOs;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Settings;
using static Toggl.Foundation.Helper.Constants;
using SelectTimeOrigin = Toggl.Foundation.MvvmCross.Parameters.SelectTimeParameters.Origin;
using System.Reactive.Subjects;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class EditTimeEntryViewModel : MvxViewModel<long>
    {
        private const int maxTagLength = 30;

        private readonly ITimeService timeService;
        private readonly ITogglDataSource dataSource;
        private readonly IDialogService dialogService;
        private readonly IInteractorFactory interactorFactory;
        private readonly IMvxNavigationService navigationService;
        private readonly IOnboardingStorage onboardingStorage;
        private readonly IAnalyticsService analyticsService;

        private readonly HashSet<long> tagIds = new HashSet<long>();
        private IDisposable tickingDisposable;
        private IDisposable confirmDisposable;
        private IDisposable preferencesDisposable;

        private IThreadSafeTimeEntry originalTimeEntry;

        private long? projectId;
        private long? taskId;
        private long workspaceId;
        private DurationFormat durationFormat;

        private BehaviorSubject<bool> hasProjectSubject = new BehaviorSubject<bool>(false);

        public IObservable<bool> HasProject { get; }

        private bool isDirty
            => originalTimeEntry.Description != Description
               || originalTimeEntry.WorkspaceId != workspaceId
               || originalTimeEntry.ProjectId != projectId
               || originalTimeEntry.TaskId != taskId
               || originalTimeEntry.Start != StartTime
               || originalTimeEntry.TagIds.SequenceEqual(tagIds) == false
               || originalTimeEntry.Duration.HasValue != !IsTimeEntryRunning
               || (originalTimeEntry.Duration.HasValue
                   && originalTimeEntry.Duration != (long)Duration.TotalSeconds)
               || originalTimeEntry.Billable != Billable;

        public IOnboardingStorage OnboardingStorage => onboardingStorage;

        public long Id { get; set; }

        public string Description { get; set; }

        [DependsOn(nameof(IsEditingDescription))]
        public string ConfirmButtonText => IsEditingDescription ? Resources.Done : Resources.Save;

        public bool IsEditingDescription { get; set; }

        [DependsOn(nameof(Description))]
        public int DescriptionRemainingLength
            => MaxTimeEntryDescriptionLengthInBytes - Description.LengthInBytes();

        [DependsOn(nameof(DescriptionRemainingLength))]
        public bool DescriptionLimitExceeded
            => DescriptionRemainingLength < 0;

        public string Project { get; set; }

        public string ProjectColor { get; set; }

        public string Client { get; set; }

        public string Task { get; set; }

        public bool IsBillableAvailable { get; private set; }

        [DependsOn(nameof(StartTime), nameof(StopTime))]
        public TimeSpan Duration
            => (StopTime ?? timeService.CurrentDateTime) - StartTime;

        public TimeSpan DisplayedDuration
        {
            get => Duration;
            set
            {
                if (stopTime.HasValue)
                {
                    StopTime = StartTime + value;
                }
                else
                {
                    StartTime = timeService.CurrentDateTime - value;
                }
            }
        }

        [DependsOn(nameof(IsTimeEntryRunning))]
        public DurationFormat DurationFormat => IsTimeEntryRunning ? DurationFormat.Improved : durationFormat;

        public DateFormat DateFormat { get; private set; }

        public TimeFormat TimeFormat { get; private set; }

        public DateTimeOffset StartTime { get; set; }

        [DependsOn(nameof(StopTime))]
        public bool IsTimeEntryRunning => !StopTime.HasValue;

        public DateTimeOffset StopTimeOrCurrent => StopTime ?? timeService.CurrentDateTime;

        private DateTimeOffset? stopTime;
        public DateTimeOffset? StopTime
        {
            get => stopTime;
            set
            {
                if (stopTime == value) return;

                stopTime = value;

                if (IsTimeEntryRunning)
                {
                    subscribeToTimeServiceTicks();
                }
                else
                {
                    tickingDisposable?.Dispose();
                    tickingDisposable = null;
                }

                RaisePropertyChanged(nameof(StopTime));
            }
        }

        public MvxObservableCollection<string> Tags { get; private set; } = new MvxObservableCollection<string>();

        [DependsOn(nameof(Tags))]
        public bool HasTags => Tags?.Any() ?? false;

        public bool Billable { get; set; }

        public string SyncErrorMessage { get; private set; }

        public bool SyncErrorMessageVisible { get; private set; }

        public IMvxCommand ConfirmCommand { get; }

        public IMvxCommand DismissSyncErrorMessageCommand { get; }

        public IMvxCommand StopCommand { get; }

        public IMvxAsyncCommand<SelectTimeOrigin> StopTimeEntryCommand { get; }

        public IMvxAsyncCommand DeleteCommand { get; }

        public IMvxAsyncCommand CloseCommand { get; }

        public IMvxAsyncCommand SelectDurationCommand { get; }

        public IMvxAsyncCommand<SelectTimeOrigin> SelectTimeCommand { get; }

        public IMvxAsyncCommand SelectStartTimeCommand { get; }

        public IMvxAsyncCommand SelectStopTimeCommand { get; }

        public IMvxAsyncCommand SelectStartDateCommand { get; }

        public IMvxAsyncCommand SelectProjectCommand { get; }

        public IMvxAsyncCommand SelectTagsCommand { get; }

        public IMvxCommand ToggleBillableCommand { get; }

        public IMvxCommand StartEditingDescriptionCommand { get; }

        public EditTimeEntryViewModel(
            ITimeService timeService,
            ITogglDataSource dataSource,
            IInteractorFactory interactorFactory,
            IMvxNavigationService navigationService,
            IOnboardingStorage onboardingStorage,
            IDialogService dialogService,
            IAnalyticsService analyticsService)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(dialogService, nameof(dialogService));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(onboardingStorage, nameof(onboardingStorage));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));

            this.dataSource = dataSource;
            this.timeService = timeService;
            this.dialogService = dialogService;
            this.interactorFactory = interactorFactory;
            this.navigationService = navigationService;
            this.onboardingStorage = onboardingStorage;
            this.analyticsService = analyticsService;

            DeleteCommand = new MvxAsyncCommand(delete);
            ConfirmCommand = new MvxCommand(confirm);
            CloseCommand = new MvxAsyncCommand(closeWithConfirmation);

            StopCommand = new MvxCommand(stopTimeEntry, () => IsTimeEntryRunning);
            StopTimeEntryCommand = new MvxAsyncCommand<SelectTimeOrigin>(onStopTimeEntryCommand);

            SelectStartTimeCommand = new MvxAsyncCommand(selectStartTime);
            SelectStopTimeCommand = new MvxAsyncCommand(selectStopTime);
            SelectStartDateCommand = new MvxAsyncCommand(selectStartDate);
            SelectDurationCommand = new MvxAsyncCommand(selectDuration);
            SelectTimeCommand = new MvxAsyncCommand<SelectTimeOrigin>(selectTime);

            SelectProjectCommand = new MvxAsyncCommand(selectProject);
            SelectTagsCommand = new MvxAsyncCommand(selectTags);
            DismissSyncErrorMessageCommand = new MvxCommand(dismissSyncErrorMessageCommand);
            ToggleBillableCommand = new MvxCommand(toggleBillable);
            StartEditingDescriptionCommand = new MvxCommand(startEditingDescriptionCommand);

            HasProject = hasProjectSubject.AsObservable();
        }

        public override void Prepare(long parameter)
        {
            Id = parameter;
        }

        public override async Task Initialize()
        {
            var timeEntry = await dataSource.TimeEntries.GetById(Id);
            originalTimeEntry = timeEntry;

            Description = timeEntry.Description;
            StartTime = timeEntry.Start;
            StopTime = timeEntry.IsRunning() ? (DateTimeOffset?)null : timeEntry.Start.AddSeconds(timeEntry.Duration.Value);
            Billable = timeEntry.Billable;
            Project = timeEntry.Project?.Name;
            ProjectColor = timeEntry.Project?.Color;
            Task = timeEntry.Task?.Name;
            Client = timeEntry.Project?.Client?.Name;
            projectId = timeEntry.Project?.Id;
            taskId = timeEntry.Task?.Id;
            SyncErrorMessage = timeEntry.LastSyncErrorMessage;
            workspaceId = timeEntry.WorkspaceId;
            SyncErrorMessageVisible = !string.IsNullOrEmpty(SyncErrorMessage);

            onTags(timeEntry.Tags);
            foreach (var tagId in timeEntry.TagIds)
                tagIds.Add(tagId);

            if (StopTime == null)
                subscribeToTimeServiceTicks();

            preferencesDisposable = dataSource.Preferences.Current
                .Subscribe(onPreferencesChanged);

            await updateFeaturesAvailability();
        }

        private void subscribeToTimeServiceTicks()
        {
            tickingDisposable = timeService
                .CurrentDateTimeObservable
                .Subscribe((DateTimeOffset _) => RaisePropertyChanged(nameof(Duration)));
        }

        private async Task delete()
        {
            var shouldDelete = await dialogService.ConfirmDestructiveAction(ActionType.DeleteExistingTimeEntry);
            if (!shouldDelete)
                return;

            try
            {
                await interactorFactory.DeleteTimeEntry(Id).Execute();

                analyticsService.DeleteTimeEntry.Track();
                dataSource.SyncManager.InitiatePushSync();
                await navigationService.Close(this);
            }
            catch
            {
                // Intentionally left blank
            }
        }

        private void confirm()
        {
            if (IsEditingDescription)
            {
                IsEditingDescription = false;
                return;
            }

            save();
        }

        private void save()
        {
            onboardingStorage.EditedTimeEntry();

            var dto = new EditTimeEntryDto
            {
                Id = Id,
                Description = Description?.Trim() ?? "",
                StartTime = StartTime,
                StopTime = StopTime,
                ProjectId = projectId,
                TaskId = taskId,
                Billable = Billable,
                WorkspaceId = workspaceId,
                TagIds = new List<long>(tagIds)
            };

            confirmDisposable = dataSource.TimeEntries
                                          .Update(dto)
                                          .Do(dataSource.SyncManager.InitiatePushSync)
                                          .Subscribe((Exception ex) => close(), () => close());
        }

        private async Task closeWithConfirmation()
        {
            if (isDirty)
            {
                var shouldDiscard = await dialogService.ConfirmDestructiveAction(ActionType.DiscardEditingChanges);
                if (!shouldDiscard)
                    return;
            }

            await close();
        }

        private Task close()
            => navigationService.Close(this);

        private async Task selectStartTime()
        {
            analyticsService.EditViewTapped.Track(EditViewTapSource.StartTime);
            await editDuration();
        }

        private async Task selectStopTime()
        {
            analyticsService.EditViewTapped.Track(EditViewTapSource.StopTime);
            await editDuration();
        }

        private async Task selectStartDate()
        {
            analyticsService.EditViewTapped.Track(EditViewTapSource.StartDate);

            var parameters = IsTimeEntryRunning
                ? DateTimePickerParameters.ForStartDateOfRunningTimeEntry(StartTime, timeService.CurrentDateTime)
                : DateTimePickerParameters.ForStartDateOfStoppedTimeEntry(StartTime);

            var duration = Duration;

            StartTime = await navigationService
                .Navigate<SelectDateTimeViewModel, DateTimePickerParameters, DateTimeOffset>(parameters)
                .ConfigureAwait(false);

            if (IsTimeEntryRunning == false)
            {
                StopTime = StartTime + duration;
            }
        }

        private async Task selectDuration()
        {
            analyticsService.EditViewTapped.Track(EditViewTapSource.Duration);
            await editDuration(true);
        }

        private async Task selectTime(SelectTimeOrigin origin)
        {
            var tapSource = getTapSourceFromSelectTimeOrigin(origin);
            analyticsService.EditViewTapped.Track(tapSource);

            var parameters = SelectTimeParameters
                .CreateFromOrigin(origin, StartTime, StopTime)
                .WithFormats(DateFormat, TimeFormat);

            var data = await navigationService
                .Navigate<SelectTimeViewModel, SelectTimeParameters, SelectTimeResultsParameters>(parameters)
                .ConfigureAwait(false);

            if (data == null)
                return;

            StartTime = data.Start;
            StopTime = data.Stop;
        }

        private EditViewTapSource getTapSourceFromSelectTimeOrigin(SelectTimeOrigin origin)
        {
            switch (origin)
            {
                case SelectTimeOrigin.StartTime:
                case SelectTimeOrigin.StartDate:
                    return EditViewTapSource.StartTime;
                case SelectTimeOrigin.StopTime:
                case SelectTimeOrigin.StopDate:
                    return EditViewTapSource.StopTime;
                case SelectTimeOrigin.Duration:
                    return EditViewTapSource.Duration;
                default:
                    throw new ArgumentException("Binding parameter is incorrect.");
            }
        }

        private void stopTimeEntry()
        {
            StopTime = timeService.CurrentDateTime;
        }

        private async Task onStopTimeEntryCommand(SelectTimeOrigin origin)
        {
            if (IsTimeEntryRunning)
            {
                StopTime = timeService.CurrentDateTime;
                return;
            }

            await SelectTimeCommand.ExecuteAsync(origin);
        }

        private async Task selectProject()
        {
            analyticsService.EditEntrySelectProject.Track();
            analyticsService.EditViewTapped.Track(EditViewTapSource.Project);

            onboardingStorage.SelectsProject();

            var returnParameter = await navigationService
                .Navigate<SelectProjectViewModel, SelectProjectParameter, SelectProjectParameter>(
                    SelectProjectParameter.WithIds(projectId, taskId, workspaceId));

            if (returnParameter.WorkspaceId == workspaceId
                && returnParameter.ProjectId == projectId
                && returnParameter.TaskId == taskId)
                return;

            projectId = returnParameter.ProjectId;
            taskId = returnParameter.TaskId;

            if (projectId == null)
            {
                Project = Task = Client = ProjectColor = "";
                clearTagsIfNeeded(workspaceId, returnParameter.WorkspaceId);
                workspaceId = returnParameter.WorkspaceId;
                await updateFeaturesAvailability();
                return;
            }

            var project = await dataSource.Projects.GetById(projectId.Value);
            clearTagsIfNeeded(workspaceId, project.WorkspaceId);
            Project = project.DisplayName();
            Client = project.Client?.Name;
            ProjectColor = project.DisplayColor();
            workspaceId = project.WorkspaceId;

            Task = taskId.HasValue ? (await dataSource.Tasks.GetById(taskId.Value)).Name : "";

            await updateFeaturesAvailability();
        }

        private async Task editDuration(bool isDurationInitiallyFocused = false)
        {
            var duration = StopTime.HasValue ? Duration : (TimeSpan?)null;
            var currentDuration = DurationParameter.WithStartAndDuration(StartTime, duration);
            var editDurationParam = new EditDurationParameters(currentDuration, false, isDurationInitiallyFocused);
            var selectedDuration = await navigationService
                .Navigate<EditDurationViewModel, EditDurationParameters, DurationParameter>(editDurationParam)
                .ConfigureAwait(false);

            StartTime = selectedDuration.Start;
            if (selectedDuration.Duration.HasValue)
            {
                StopTime = selectedDuration.Start + selectedDuration.Duration.Value;
            }
        }

        private async Task selectTags()
        {
            analyticsService.EditEntrySelectTag.Track();
            analyticsService.EditViewTapped.Track(EditViewTapSource.Tags);

            var tagsToPass = tagIds.ToArray();
            var returnedTags = await navigationService
                .Navigate<SelectTagsViewModel, (long[], long), long[]>(
                    (tagsToPass, workspaceId));

            if (returnedTags.SequenceEqual(tagsToPass))
                return;

            Tags.Clear();
            tagIds.Clear();

            foreach (var tagId in returnedTags)
                tagIds.Add(tagId);

            dataSource.Tags
                .GetAll(tag => tagIds.Contains(tag.Id))
                .Subscribe(onTags);
        }

        private void onTags(IEnumerable<IThreadSafeTag> tags)
        {
            if (tags == null)
                return;

            tags.Select(tag => tag.Name)
               .Select(trimTag)
               .ForEach(Tags.Add);
            RaisePropertyChanged(nameof(Tags));
            RaisePropertyChanged(nameof(HasTags));
        }

        private void dismissSyncErrorMessageCommand()
            => SyncErrorMessageVisible = false;

        private void toggleBillable()
        {
            analyticsService.EditViewTapped.Track(EditViewTapSource.Billable);
            Billable = !Billable;
        }

        private void startEditingDescriptionCommand()
        {
            analyticsService.EditViewTapped.Track(EditViewTapSource.Description);
            IsEditingDescription = true;
        }

        private void clearTagsIfNeeded(long currentWorkspaceId, long newWorkspaceId)
        {
            if (currentWorkspaceId == newWorkspaceId) return;

            Tags.Clear();
            tagIds.Clear();
            RaisePropertyChanged(nameof(Tags));
            RaisePropertyChanged(nameof(HasTags));
        }

        private string trimTag(string tag)
        {
            var tagLength = tag.LengthInGraphemes();
            if (tagLength <= maxTagLength)
                return tag;

            return $"{tag.UnicodeSafeSubstring(0, maxTagLength)}...";
        }

        private void onPreferencesChanged(IThreadSafePreferences preferences)
        {
            durationFormat = preferences.DurationFormat;
            DateFormat = preferences.DateFormat;
            TimeFormat = preferences.TimeOfDayFormat;

            RaisePropertyChanged(nameof(DurationFormat));
        }

        private async Task updateFeaturesAvailability()
        {
            IsBillableAvailable = await interactorFactory.IsBillableAvailableForWorkspace(workspaceId).Execute();
        }

        public override void ViewDestroy(bool viewFinishing)
        {
            base.ViewDestroy(viewFinishing);
            confirmDisposable?.Dispose();
            tickingDisposable?.Dispose();
            preferencesDisposable?.Dispose();
        }

        private void OnProjectChanged()
        {
            hasProjectSubject.OnNext(!string.IsNullOrWhiteSpace(Project));
        }
    }
}
