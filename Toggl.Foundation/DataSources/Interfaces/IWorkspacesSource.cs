using System;
using Toggl.Foundation.DTOs;
using Toggl.Foundation.Models.Interfaces;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.DataSources.Interfaces
{
    public interface IWorkspacesSource
        : IDataSource<IThreadSafeWorkspace, IDatabaseWorkspace>
    {
        IObservable<IThreadSafeWorkspace> Create(string name);
    }
}
