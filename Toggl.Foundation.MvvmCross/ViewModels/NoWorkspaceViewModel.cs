using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Sync;
using Toggl.Multivac;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class NoWorkspaceViewModel : MvxViewModel
    {
        private readonly IMvxNavigationService navigationService;
        private readonly ITogglDataSource dataSource;
        private readonly Subject<bool> isLoading = new Subject<bool>();

        public IObservable<bool> IsLoading => isLoading.AsObservable();

        public NoWorkspaceViewModel(
            IMvxNavigationService navigationService,
            ITogglDataSource dataSource)
        {
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));

            this.navigationService = navigationService;
            this.dataSource = dataSource;
        }

        public async Task TryAgain()
        {
            isLoading.OnNext(true);

            var workspaces = await dataSource
                .SyncManager
                .ForceFullSync()
                .Where(state => state == SyncState.Sleep)
                .SelectMany(dataSource.Workspaces.GetAll());

            isLoading.OnNext(false);

            if (workspaces.Any())
            {
                close();
            }
        }

        public async Task CreateWorkspaceWithDefaultName()
        {
            isLoading.OnNext(true);

            await dataSource
                .User
                .Current
                .FirstAsync()
                .Select(user => $"{user.Fullname}'s Workspace")
                .SelectMany(dataSource.Workspaces.Create)
                .SelectMany(workspace => dataSource.User.UpdateWorkspace(workspace.Id));

            isLoading.OnNext(false);
            close();
        }

        private void close()
        {
            navigationService.Close(this);
        }
    }
}
