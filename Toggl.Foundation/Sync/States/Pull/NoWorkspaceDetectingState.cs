using System;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Foundation.Exceptions;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.DataSources;

namespace Toggl.Foundation.Sync.States.Pull
{
    internal sealed class NoWorkspaceDetectingState : ISyncState<IFetchObservables>
    {
        public StateResult<IFetchObservables> Continue { get; } = new StateResult<IFetchObservables>();

        private readonly ITogglDataSource dataSource;

        public NoWorkspaceDetectingState(ITogglDataSource dataSource)
        {
            this.dataSource = dataSource;
        }

        public IObservable<ITransition> Start(IFetchObservables fetch)
            => dataSource.Workspaces.GetAll()
                .Select(workspaces => workspaces.Any()
                    ? Continue.Transition(fetch)
                    : throw new NoWorkspaceException());
    }
}
