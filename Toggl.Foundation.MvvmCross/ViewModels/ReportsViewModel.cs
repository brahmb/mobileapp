using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using PropertyChanged;
using Toggl.Foundation;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels.Hints;
using Toggl.Foundation.Reports;
using Toggl.Multivac;
using CommonFunctions = Toggl.Multivac.Extensions.CommonFunctions;
using static Toggl.Multivac.Extensions.EnumerableExtensions;

[assembly: MvxNavigation(typeof(ReportsViewModel), ApplicationUrls.Reports)]
namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class ReportsViewModel : MvxViewModel
    {


        private readonly ITimeService timeService;
        private readonly ITogglDataSource dataSource;
        private readonly IMvxNavigationService navigationService;
        private readonly IInteractorFactory interactorFactory;
        private readonly IAnalyticsService analyticsService;
        private readonly IDialogService dialogService;
        private readonly ReportsCalendarViewModel calendarViewModel;
        private readonly Subject<Unit> reportSubject = new Subject<Unit>();
        private readonly CompositeDisposable disposeBag = new CompositeDisposable();

        private DateTimeOffset startDate;
        private DateTimeOffset endDate;
        private int totalDays => (endDate - startDate).Days + 1;
        private ReportsSource source;
        private int projectsNotSyncedCount;
        private DateTime reportSubjectStartTime;
        private long workspaceId;
        private DateFormat dateFormat;
        private IReadOnlyList<ChartSegment> segments = new ChartSegment[0];
        private IReadOnlyList<ChartSegment> groupedSegments = new ChartSegment[0];

        public bool IsLoading { get; private set; }

        public TimeSpan TotalTime { get; private set; } = TimeSpan.Zero;

        public DurationFormat DurationFormat { get; private set; }

        public bool TotalTimeIsZero => TotalTime.Ticks == 0;

        public float? BillablePercentage { get; private set; } = null;

        public IReadOnlyList<ChartSegment> Segments
        {
            get => segments;
            private set
            {
                segments = value;
                groupedSegments = null;
            }
        }

        [DependsOn(nameof(Segments))]
        public IReadOnlyList<ChartSegment> GroupedSegments
            => groupedSegments ?? (groupedSegments = groupSegments());

        public bool ShowEmptyState => segments.None() && !IsLoading;

        public string CurrentDateRangeString { get; private set; }

        public bool IsCurrentWeek
        {
            get
            {
                var currentDate = timeService.CurrentDateTime.Date;
                var startOfWeek = currentDate.AddDays(1 - (int)currentDate.DayOfWeek);
                var endOfWeek = startOfWeek.AddDays(6);

                return startDate.Date == startOfWeek
                       && endDate.Date == endOfWeek;
            }
        }

        public string WorkspaceName { get; private set; }

        public IDictionary<string, IThreadSafeWorkspace> Workspaces { get; private set; }

        public IMvxCommand HideCalendarCommand { get; }

        public IMvxCommand ToggleCalendarCommand { get; }

        public IMvxCommand<DateRangeParameter> ChangeDateRangeCommand { get; }

        public IMvxAsyncCommand SelectWorkspace { get; }

        public ReportsViewModel(ITogglDataSource dataSource,
                                ITimeService timeService,
                                IMvxNavigationService navigationService,
                                IInteractorFactory interactorFactory,
                                IAnalyticsService analyticsService,
                                IDialogService dialogService)
        {
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(dialogService, nameof(dialogService));

            this.timeService = timeService;
            this.navigationService = navigationService;
            this.analyticsService = analyticsService;
            this.dataSource = dataSource;
            this.interactorFactory = interactorFactory;
            this.dialogService = dialogService;

            calendarViewModel = new ReportsCalendarViewModel(timeService, dataSource);

            HideCalendarCommand = new MvxCommand(hideCalendar);
            ToggleCalendarCommand = new MvxCommand(toggleCalendar);
            ChangeDateRangeCommand = new MvxCommand<DateRangeParameter>(changeDateRange);
            SelectWorkspace = new MvxAsyncCommand(selectWorkspace);
        }

        public override async Task Initialize()
        {
            Workspaces = await dataSource.Workspaces
                .GetAll()
                .SelectMany(CommonFunctions.Identity)
                .ToDictionary(ws => ws.Name, ws => ws);

            var workspace = await interactorFactory.GetDefaultWorkspace().Execute();

            workspaceId = workspace.Id;
            WorkspaceName = workspace.Name;

            disposeBag.Add(
                reportSubject
                    .AsObservable()
                    .Do(setLoadingState)
                    .SelectMany(_ => dataSource.ReportsProvider.GetProjectSummary(workspaceId, startDate, endDate))
                    .Subscribe(onReport, onError)
            );

            disposeBag.Add(
                calendarViewModel.SelectedDateRangeObservable.Subscribe(
                    newDateRange => ChangeDateRangeCommand.Execute(newDateRange)
                )
            );

            var preferencesDisposable = dataSource.Preferences.Current
                .Subscribe(onPreferencesChanged);

            disposeBag.Add(preferencesDisposable);

            IsLoading = true;
        }

        public override void ViewAppeared()
        {
            base.ViewAppeared();
            navigationService.Navigate(calendarViewModel);
        }

        private void setLoadingState(Unit obj)
        {
            reportSubjectStartTime = timeService.CurrentDateTime.UtcDateTime;
            IsLoading = true;
            Segments = new ChartSegment[0];
        }

        private void onReport(ProjectSummaryReport report)
        {
            TotalTime = TimeSpan.FromSeconds(report.TotalSeconds);
            BillablePercentage = report.TotalSeconds == 0 ? null : (float?)report.BillablePercentage;

            Segments = report.Segments
                             .Select(segment => segment.WithDurationFormat(DurationFormat))
                             .ToList()
                             .AsReadOnly();

            IsLoading = false;

            trackReportsEvent(true);
        }

        private void onError(Exception ex)
        {
            RaisePropertyChanged(nameof(Segments));
            IsLoading = false;
            trackReportsEvent(false);
        }

        private void trackReportsEvent(bool success)
        {
            var loadingTime = timeService.CurrentDateTime.UtcDateTime - reportSubjectStartTime;

            if (success)
            {
                analyticsService.ReportsSuccess.Track(source, totalDays, projectsNotSyncedCount, loadingTime.TotalMilliseconds);
            }
            else
            {
                analyticsService.ReportsFailure.Track(source, totalDays, loadingTime.TotalMilliseconds);
            }
        }

        private void toggleCalendar()
        {
            navigationService.ChangePresentation(new ToggleCalendarVisibilityHint());
            calendarViewModel.OnToggleCalendar();
        }

        private void hideCalendar()
        {
            navigationService.ChangePresentation(new ToggleCalendarVisibilityHint(forceHide: true));
            calendarViewModel.OnHideCalendar();
        }

        private void changeDateRange(DateRangeParameter dateRange)
        {
            startDate = dateRange.StartDate;
            endDate = dateRange.EndDate;
            source = dateRange.Source;
            updateCurrentDateRangeString();
            reportSubject.OnNext(Unit.Default);
        }

        private void updateCurrentDateRangeString()
        {
            if (startDate == default(DateTimeOffset) || endDate == default(DateTimeOffset))
                return;

            if (startDate == endDate)
            {
                CurrentDateRangeString = $"{startDate.ToString(dateFormat.Short)} ▾";
                return;
            }

            CurrentDateRangeString = IsCurrentWeek
                ? $"{Resources.ThisWeek} ▾"
                : $"{startDate.ToString(dateFormat.Short)} - {endDate.ToString(dateFormat.Short)} ▾";
        }

        private void onPreferencesChanged(IThreadSafePreferences preferences)
        {
            DurationFormat = preferences.DurationFormat;
            dateFormat = preferences.DateFormat;

            Segments = segments.Select(segment => segment.WithDurationFormat(DurationFormat))
                               .ToList()
                               .AsReadOnly();

            updateCurrentDateRangeString();
        }

        private const float minimumSegmentPercentageToBeOnItsOwn = 5f;
        private const float maximumSegmentPercentageToEndUpInOther = 1f;
        private const float minimumOtherSegmentDisplayPercentage = 1f;
        private const float maximumOtherProjectPercentageWithSegmentsBetweenOneAndFivePercent = 5f;

        private IReadOnlyList<ChartSegment> groupSegments()
        {
            var groupedData = segments.GroupBy(segment => segment.Percentage >= minimumSegmentPercentageToBeOnItsOwn);

            var aboveStandAloneThresholdSegments = groupedData
                .Where(group => group.Key)
                .SelectMany(CommonFunctions.Identity)
                .ToList();

            var otherProjectsCandidates = groupedData
                .Where(group => !group.Key)
                .SelectMany(CommonFunctions.Identity)
                .ToList();

            var finalOtherProjects = otherProjectsCandidates
                .Where(segment => segment.Percentage < maximumSegmentPercentageToEndUpInOther)
                .ToList();

            var remainingOtherProjectCandidates = otherProjectsCandidates
                .Except(finalOtherProjects)
                .OrderBy(segment => segment.Percentage)
                .ToList();

            foreach (var segment in remainingOtherProjectCandidates)
            {
                finalOtherProjects.Add(segment);

                if (percentageOf(finalOtherProjects) + segment.Percentage > maximumOtherProjectPercentageWithSegmentsBetweenOneAndFivePercent)
                {
                    break;
                }
            }

            if (!finalOtherProjects.Any())
            {
                return segments;
            }

            var leftOutOfOther = remainingOtherProjectCandidates.Except(finalOtherProjects).ToList();
            aboveStandAloneThresholdSegments.AddRange(leftOutOfOther);
            var onTheirOwnSegments = aboveStandAloneThresholdSegments.OrderBy(segment => segment.Percentage).ToList();

            ChartSegment lastSegment;

            if (finalOtherProjects.Count == 1)
            {
                var singleSmallSegment = finalOtherProjects.First();
                lastSegment = new ChartSegment(
                    singleSmallSegment.ProjectName,
                    string.Empty,
                    singleSmallSegment.Percentage >= minimumOtherSegmentDisplayPercentage ? singleSmallSegment.Percentage : minimumOtherSegmentDisplayPercentage,
                    finalOtherProjects.Sum(segment => (float)segment.TrackedTime.TotalSeconds),
                    finalOtherProjects.Sum(segment => segment.BillableSeconds),
                    singleSmallSegment.Color,
                    DurationFormat);
            }
            else
            {
                var otherPercentage = percentageOf(finalOtherProjects);
                lastSegment = new ChartSegment(
                    Resources.Other,
                    string.Empty,
                    otherPercentage >= minimumOtherSegmentDisplayPercentage ? otherPercentage : minimumOtherSegmentDisplayPercentage,
                    finalOtherProjects.Sum(segment => (float)segment.TrackedTime.TotalSeconds),
                    finalOtherProjects.Sum(segment => segment.BillableSeconds),
                    Color.Reports.OtherProjectsSegmentBackground.ToHexString(),
                    DurationFormat);
            }

            return onTheirOwnSegments
                .Append(lastSegment)
                .ToList()
                .AsReadOnly();
        }

        private async Task selectWorkspace()
        {
            var workspace = await dialogService.Select(Resources.SelectWorkspace, Workspaces);

            if (workspace == null || workspace.Id == workspaceId) return;

            workspaceId = workspace.Id;
            WorkspaceName = workspace.Name;
            reportSubject.OnNext(Unit.Default);
        }

        private float percentageOf(List<ChartSegment> list)
            => list.Sum(segment => segment.Percentage);
    }
}
