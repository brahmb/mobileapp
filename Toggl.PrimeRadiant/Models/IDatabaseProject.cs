using System.Collections.Generic;
using Toggl.Multivac.Models;

namespace Toggl.PrimeRadiant.Models
{
    public interface IDatabaseProject : IProject, IDatabaseSyncable, IGhostable
    {
        IDatabaseClient Client { get; }

        IDatabaseWorkspace Workspace { get; }

        IEnumerable<IDatabaseTask> Tasks { get; }
    }
}
