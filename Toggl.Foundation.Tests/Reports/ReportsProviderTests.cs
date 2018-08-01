using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using NSubstitute;
using Toggl.Foundation.Helper;
using Toggl.Foundation.Reports;
using Toggl.Foundation.Tests.Generators;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Models;
using Toggl.Multivac.Models.Reports;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Toggl.Ultrawave;
using Toggl.Ultrawave.ApiClients;
using Toggl.Ultrawave.Models.Reports;
using Xunit;

namespace Toggl.Foundation.Tests.Reports
{
    public sealed class ReportsProviderTests
    {
        public abstract class ReportsProviderTest
        {
            protected ITogglApi Api { get; } = Substitute.For<ITogglApi>();
            protected ITogglDatabase Database { get; } = Substitute.For<ITogglDatabase>();

            protected IProjectsApi ProjectsApi { get; } = Substitute.For<IProjectsApi>();
            protected IProjectsSummaryApi ProjectsSummaryApi { get; } = Substitute.For<IProjectsSummaryApi>();
            protected IRepository<IDatabaseProject> ProjectsRepository { get; } =
                Substitute.For<IRepository<IDatabaseProject>>();
            protected IRepository<IDatabaseClient> ClientsRepository { get; } =
                Substitute.For<IRepository<IDatabaseClient>>();

            protected IReportsProvider ReportsProvider { get; }

            protected ReportsProviderTest()
            {
                Api.Projects.Returns(ProjectsApi);
                Api.ProjectsSummary.Returns(ProjectsSummaryApi);

                Database.Projects.Returns(ProjectsRepository);
                Database.Clients.Returns(ClientsRepository);

                ReportsProvider = new ReportsProvider(Api, Database);
            }
        }

        public sealed class Constructor : ReportsProviderTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(bool useApi, bool useDatabase)
            {
                var api = useApi ? Api : null;
                var database = useDatabase ? Database : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new ReportsProvider(api, database);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheGetProjectSummaryMethod : ReportsProviderTest
        {
            private const long workspaceId = 10;

            private readonly IProjectsSummary apiProjectsSummary = Substitute.For<IProjectsSummary>();

            public TheGetProjectSummaryMethod()
            {
                ProjectsSummaryApi
                    .GetByWorkspace(Arg.Any<long>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                    .Returns(Observable.Return(apiProjectsSummary)); 
            }

            [Property]
            public void QueriesTheDatabaseForFindingProjects(NonEmptyArray<NonNegativeInt> projectIds)
            {
                var actualProjectIds = projectIds.Get.Select(i => (long)i.Get).Distinct().ToArray();
                var summaries = getSummaryList(actualProjectIds);
                apiProjectsSummary.ProjectsSummaries.Returns(summaries);
                configureRepositoryToReturn(actualProjectIds);

                ReportsProvider.GetProjectSummary(workspaceId, DateTimeOffset.Now.AddDays(-7), DateTimeOffset.Now).Wait();

                ProjectsRepository.Received()
                    .GetById(Arg.Is<long>(id => Array.IndexOf(actualProjectIds, id) >= 0));
            }

            [Property(MaxTest = 1)]
            public void QueriesTheApiIfAnyProjectIsNotFoundInTheDatabase(NonEmptyArray<NonNegativeInt> projectIds)
            {
                var actualProjectIds = projectIds.Get.Select(i => (long)i.Get).Distinct().ToArray();
                if (actualProjectIds.Length < 2) return;

                var projectsInDb = actualProjectIds.Where((id, i) => i % 2 == 0).ToArray();
                var projectsInApi = actualProjectIds.Where((id, i) => i % 2 != 0).ToArray();
                var summaries = getSummaryList(actualProjectIds);
                apiProjectsSummary.ProjectsSummaries.Returns(summaries);
                configureRepositoryToReturn(projectsInDb, projectsInApi);
                configureApiToReturn(projectsInApi);

                ReportsProvider.GetProjectSummary(workspaceId, DateTimeOffset.Now.AddDays(-7), DateTimeOffset.Now).Wait();

                ProjectsApi.Received()
                    .Search(workspaceId, Arg.Is<long[]>(
                        calledIds => ensureExpectedIdsAreReturned(calledIds, projectsInApi)));
            }

            [Property(MaxTest = 10, StartSize = 10, EndSize = 20)]
            public void ReturnsOnlyOneListIfItQueriesTheApi(NonEmptyArray<NonNegativeInt> projectIds)
            {
                var actualProjectIds = projectIds.Get.Select(i => (long)i.Get).Distinct().ToArray();
                if (actualProjectIds.Length < 2) return;
                
                var projectsInDb = actualProjectIds.Where((i, id) => i % 2 == 0).ToArray();
                var projectsInApi = actualProjectIds.Where((i, id) => i % 2 != 0).ToArray();
                var summaries = getSummaryList(actualProjectIds);
                apiProjectsSummary.ProjectsSummaries.Returns(summaries);
                configureRepositoryToReturn(projectsInDb, projectsInApi);
                configureApiToReturn(projectsInApi);

                var lists = ReportsProvider.GetProjectSummary(workspaceId, DateTimeOffset.Now.AddDays(-7), DateTimeOffset.Now).ToList().Wait();

                lists.Should().HaveCount(1);
            }
            
            [Property(MaxTest = 10, StartSize = 10, EndSize = 20)]
            public void ReturnsOnlyOneListIfItUsesTheMemoryCache(NonEmptyArray<NonNegativeInt> projectIds)
            {
                var actualProjectIds = projectIds.Get.Select(i => (long)i.Get).Distinct().ToArray();
                if (actualProjectIds.Length < 2) return;
                
                var projectsInDb = actualProjectIds.Where((i, id) => i % 2 == 0).ToArray();
                var projectsInApi = actualProjectIds.Where((i, id) => i % 2 != 0).ToArray();
                var summaries = getSummaryList(actualProjectIds);
                apiProjectsSummary.ProjectsSummaries.Returns(summaries);
                configureRepositoryToReturn(projectsInDb, projectsInApi);
                configureApiToReturn(projectsInApi);

                ReportsProvider.GetProjectSummary(workspaceId, DateTimeOffset.Now.AddDays(-7), DateTimeOffset.Now).Wait();
                var lists = ReportsProvider.GetProjectSummary(workspaceId, DateTimeOffset.Now.AddDays(-7), DateTimeOffset.Now).ToList().Wait();

                lists.Should().HaveCount(1);
            }

            [Property(MaxTest = 1)]
            public void CachesTheApiResultsInMemorySoTheApiIsNotCalledTwiceForTheSameProjects(
                NonEmptyArray<NonNegativeInt> projectIds)
            {
                var actualProjectIds = projectIds.Get.Select(i => (long)i.Get).Distinct().ToArray();
                var idCount = actualProjectIds.Length;
                if (idCount < 2) return;
                var dbProjectCount = (int)Math.Floor((float)idCount / 2);
                var apiProjectCount = idCount - dbProjectCount;
                var projectsInDb = actualProjectIds.Take(dbProjectCount).ToArray();
                var projectsInApi = actualProjectIds.TakeLast(apiProjectCount).ToArray();
                var summaries = getSummaryList(actualProjectIds);
                apiProjectsSummary.ProjectsSummaries.Returns(summaries);
                configureRepositoryToReturn(projectsInDb, projectsInApi);
                configureApiToReturn(projectsInApi);

                ReportsProvider.GetProjectSummary(workspaceId, DateTimeOffset.Now.AddDays(-7), DateTimeOffset.Now).Wait();
                ReportsProvider.GetProjectSummary(workspaceId, DateTimeOffset.Now.AddDays(-7), DateTimeOffset.Now).Wait();

                ProjectsApi.Received(1)
                    .Search(workspaceId, Arg.Is<long[]>(
                        calledIds => ensureExpectedIdsAreReturned(calledIds, projectsInApi)));
            }

            [Property]
            public void DoesNotCallTheApiIfAllProjectsAreInTheDatabase(NonEmptyArray<NonNegativeInt> projectIds)
            {
                var actualProjectIds = projectIds.Get.Select(i => (long)i.Get).Distinct().ToArray();
                var summaries = getSummaryList(actualProjectIds);
                apiProjectsSummary.ProjectsSummaries.Returns(summaries);
                configureRepositoryToReturn(actualProjectIds);

                ReportsProvider.GetProjectSummary(workspaceId, DateTimeOffset.Now.AddDays(-7), DateTimeOffset.Now).Wait();

                ProjectsApi.DidNotReceive().Search(Arg.Any<long>(), Arg.Any<long[]>());
            }

            [Property]
            public void CreatesAChartSegmentWithNoProjectIfThereAreNullProjectIdsAmongTheApiResults(
                NonNegativeInt[] projectIds)
            {
                var actualProjectIds = projectIds.Select(i => (long)i.Get).Distinct().ToArray();
                var summaries = getSummaryList(actualProjectIds);
                summaries.Add(new ProjectSummary());
                apiProjectsSummary.ProjectsSummaries.Returns(summaries);
                configureRepositoryToReturn(actualProjectIds);

                var report = ReportsProvider
                    .GetProjectSummary(workspaceId, DateTimeOffset.Now.AddDays(-7), DateTimeOffset.Now).Wait();

                report.Segments.Single(s => s.Color == Color.NoProject).ProjectName.Should().Be(Resources.NoProject);
            }

            [Property]
            public void ReturnsTheProjectOrderedByTotalTimeTracked(NonNegativeInt[] projectIds)
            {
                var actualProjectIds = projectIds.Select(i => (long)i.Get).Distinct().ToArray();
                var summaries = getSummaryList(actualProjectIds);
                summaries.Add(new ProjectSummary());
                var summaryCount = summaries.Count;
                for (int i = 0; i < summaryCount; i++)
                {
                    var summary = (ProjectSummary)summaries[i];
                    summary.TrackedSeconds = i;
                }
                apiProjectsSummary.ProjectsSummaries.Returns(summaries);
                configureRepositoryToReturn(actualProjectIds);

                var report = ReportsProvider
                    .GetProjectSummary(workspaceId, DateTimeOffset.Now.AddDays(-7), DateTimeOffset.Now).Wait();

                report.Segments.Should().BeInDescendingOrder(s => s.Percentage);
            }

            private IList<IProjectSummary> getSummaryList(long[] projectIds)
                => projectIds
                    .Select(id => new ProjectSummary { ProjectId = id })
                    .Cast<IProjectSummary>()
                    .ToList();

            private void configureRepositoryToReturn(long[] actualProjectIds, long[] throwIds = null)
            {
                foreach (var projectId in actualProjectIds)
                {
                    var project = Substitute.For<IDatabaseProject>();
                    project.Id.Returns(projectId);
                    project.Name.Returns(projectId.ToString());

                    ProjectsRepository
                            .GetById(Arg.Is(projectId))
                            .Returns(Observable.Return(project));
                }

                if (throwIds == null) return;

                foreach (var id in throwIds)
                {
                    ProjectsRepository
                        .GetById(id)
                        .Returns(Observable.Throw<IDatabaseProject>(new Exception()));
                }
            }

            private void configureApiToReturn(long[] projectIds)
            {
                var projects = projectIds.Select(projectId =>
                {
                    var project = Substitute.For<IProject>();
                    project.Id.Returns(projectId);
                    project.Name.Returns(projectId.ToString());
                    return project;
                }).ToList();

                ProjectsApi
                    .Search(Arg.Any<long>(), Arg.Any<long[]>())
                    .Returns(Observable.Return(projects));
            }
            
            private bool ensureExpectedIdsAreReturned(long[] actual, long[] expected)
            {
                if (actual.Length != expected.Length) return false;

                foreach (var actualTag in actual)
                {
                    if (!expected.Contains(actualTag))
                        return false;
                }
                return true;
            }
        }
    }
}
