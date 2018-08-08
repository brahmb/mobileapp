using System;

namespace Toggl.Foundation.Sync
{
    public interface ISyncManager
    {
        SyncState State { get; }
        IObservable<SyncProgress> ProgressObservable { get; }
        bool IsRunningSync { get; }

        IObservable<SyncState> PushSync();
        IObservable<SyncState> ForceFullSync();
        IObservable<SyncState> CleanUp();

        IObservable<SyncState> Freeze();
    }
}
