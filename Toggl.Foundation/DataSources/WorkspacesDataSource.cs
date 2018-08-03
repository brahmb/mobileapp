using System;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Models;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Sync.ConflictResolution;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.DataSources
{
    public sealed class WorkspacesDataSource
        : DataSource<IThreadSafeWorkspace, IDatabaseWorkspace>, IWorkspacesSource
    {
        private readonly IIdProvider idProvider;
        private readonly ITimeService timeService;

        public WorkspacesDataSource(IIdProvider idProvider, IRepository<IDatabaseWorkspace> repository, ITimeService timeService)
            : base(repository)
        {
            Ensure.Argument.IsNotNull(idProvider, nameof(idProvider));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));

            this.idProvider = idProvider;
            this.timeService = timeService;
        }

        public IObservable<IThreadSafeWorkspace> Create(string name)
            => idProvider.GetNextIdentifier()
                .Apply(Workspace.Builder.Create)
                .SetName(name)
                .SetAt(timeService.CurrentDateTime)
                .SetSyncStatus(SyncStatus.SyncNeeded)
                .Build()
                .Apply(Create);

        protected override IThreadSafeWorkspace Convert(IDatabaseWorkspace entity)
            => Workspace.From(entity);

        protected override ConflictResolutionMode ResolveConflicts(IDatabaseWorkspace first, IDatabaseWorkspace second)
            => Resolver.ForWorkspaces.Resolve(first, second);
    }
}
