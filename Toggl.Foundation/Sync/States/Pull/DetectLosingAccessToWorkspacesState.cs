using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Sync.States.Pull
{
    public sealed class DetectLosingAccessToWorkspacesState : ISyncState<IFetchObservables>
    {
        private readonly IDataSource<IThreadSafeWorkspace, IDatabaseWorkspace> dataSource;

        public StateResult<IEnumerable<IThreadSafeWorkspace>> LostAccessTo { get; } = new StateResult<IEnumerable<IThreadSafeWorkspace>>();

        public StateResult NoAccessLost { get; } = new StateResult();

        public DetectLosingAccessToWorkspacesState(IDataSource<IThreadSafeWorkspace, IDatabaseWorkspace> dataSource)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));

            this.dataSource = dataSource;
        }

        public IObservable<ITransition> Start(IFetchObservables fetchObservables)
            => fetchObservables.GetList<IWorkspace>()
                .SelectMany(workspacesWhichWereNotFetched)
                .Select(missingWorkspaces => missingWorkspaces.Any()
                    ? LostAccessTo.Transition(missingWorkspaces)
                    : (ITransition)NoAccessLost.Transition());

        private IObservable<IList<IThreadSafeWorkspace>> workspacesWhichWereNotFetched(List<IWorkspace> fetchedWorkspaces)
            => allStoredWorkspaces()
                .Where(stored => fetchedWorkspaces.None(fetched => fetched.Id == stored.Id))
                .ToList();

        private IObservable<IThreadSafeWorkspace> allStoredWorkspaces()
            => dataSource.GetAll(ws => ws.Id > 0 && ws.IsGhost == false)
                         .SelectMany(CommonFunctions.Identity);
    }
}
