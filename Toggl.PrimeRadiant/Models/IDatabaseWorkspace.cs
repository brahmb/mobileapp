using Toggl.Multivac.Models;

namespace Toggl.PrimeRadiant.Models
{
    public interface IDatabaseWorkspace : IWorkspace, IDatabaseSyncable, IGhostable { }
}
