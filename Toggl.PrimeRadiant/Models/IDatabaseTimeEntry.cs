using System.Collections.Generic;
using Toggl.Multivac.Models;

namespace Toggl.PrimeRadiant.Models
{
    public interface IDatabaseTimeEntry : ITimeEntry, IDatabaseSyncable, IGhostable
    {
        IDatabaseTask Task { get; }

        IDatabaseUser User { get; }

        IDatabaseProject Project { get; }

        IDatabaseWorkspace Workspace { get; }

        IEnumerable<IDatabaseTag> Tags { get; }
    }
}
