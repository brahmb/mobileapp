using static Toggl.Foundation.Sync.SyncState;

namespace Toggl.Foundation.Sync
{
    internal sealed class SyncStateQueue : ISyncStateQueue
    {
        private bool pulledLast;
        private bool pullSyncQueued;
        private bool pushSyncQueued;
        private bool cleanUp;

        public void QueuePushSync()
        {
            pushSyncQueued = true;
        }

        public void QueuePullSync()
        {
            pullSyncQueued = true;
        }

        public void QueueCleanUp()
        {
            cleanUp = true;
        }

        public SyncState Dequeue()
        {
            if (pulledLast)
                return push();

            if (pullSyncQueued)
                return pull();

            if (pushSyncQueued)
                return push();

            if (cleanUp)
            {
                cleanUp = false;
                return CleanUp;
            }

            return Sleep;
        }

        public void Clear()
        {
            pulledLast = false;
            pullSyncQueued = false;
            pushSyncQueued = false;
            cleanUp = false;
        }

        private SyncState pull()
        {
            pullSyncQueued = false;
            pulledLast = true;
            return Pull;
        }

        private SyncState push()
        {
            pushSyncQueued = false;
            pulledLast = false;
            return Push;
        }
    }
}
