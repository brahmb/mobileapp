using System;
using System.Reactive.Linq;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using NSubstitute;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Tests.Generators;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Xunit;

namespace Toggl.Foundation.Tests.DataSources
{
    public sealed class TagsDataSourceTests
    {
        public abstract class TagsDataSourceTest : BaseDataSourceTests<TagsDataSource>
        {
            protected override TagsDataSource CreateDataSource()
                => new TagsDataSource(IdProvider, DataBase.Tags, TimeService);
        }

        public sealed class TheConstructor : TagsDataSourceTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useIdProvider, bool useRepository, bool useTimeService)
            {
                var idProvider = useIdProvider ? IdProvider : null;
                var repository = useRepository ? DataBase.Tags : null;
                var timeService = useTimeService ? TimeService : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new TagsDataSource(idProvider, repository, timeService);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheCreateMethod : TagsDataSourceTest
        {
            [Property]
            public void CreatesTagWithIdFromIdProvider(long nextId)
            {
                IdProvider.GetNextIdentifier().Returns(nextId);

                DataSource.Create("Some tag", 10).Wait();

                DataBase.Tags.Received().Create(
                    Arg.Is<IDatabaseTag>(tag => tag.Id == nextId)
                ).Wait();
            }

            [Property]
            public void CreatesTagWithPassedName(NonEmptyString nonEmptyString)
            {
                var tagName = nonEmptyString.Get;

                DataSource.Create(tagName, 10).Wait();

                DataBase.Tags.Received().Create(
                    Arg.Is<IDatabaseTag>(tag => tag.Name == tagName)
                ).Wait();
            }

            [Property]
            public void CreatesTagWithPassedWorkspaceId(NonZeroInt nonZeroint)
            {
                var workspaceId = nonZeroint.Get;
                DataSource.Create("Some tag", workspaceId).Wait();

                DataBase.Tags.Received().Create(
                    Arg.Is<IDatabaseTag>(tag => tag.WorkspaceId == workspaceId)
                ).Wait();
            }

            [Property]
            public void CreatesTagWithAtDateFromTimeServiceCurrentDateTime(
                DateTimeOffset currentTime)
            {
                TimeService.CurrentDateTime.Returns(currentTime);

                DataSource.Create("Some tag", 100).Wait();

                DataBase.Tags.Received().Create(
                    Arg.Is<IDatabaseTag>(tag => tag.At == currentTime)
                ).Wait();
            }

            [Property]
            public void CreatesTagWithSyncNeeded(
                NonEmptyString name, NonZeroInt workspaceId)
            {
                DataSource.Create(name.Get, workspaceId.Get).Wait();

                DataBase.Tags.Received().Create(
                    Arg.Is<IDatabaseTag>(tag => tag.SyncStatus == SyncStatus.SyncNeeded)
                ).Wait();
            }
        }
    }
}
