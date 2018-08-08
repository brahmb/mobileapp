namespace Toggl.Foundation.Sync
{
    public interface ISyncStateQueue
    {
        void QueuePushSync();
        void QueuePullSync();
        void QueueCleanUp();

        SyncState Dequeue();
        void Clear();
    }
}
