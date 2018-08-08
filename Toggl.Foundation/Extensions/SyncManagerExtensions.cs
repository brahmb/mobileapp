using Toggl.Foundation.Sync;

namespace Toggl.Foundation.Extensions
{
    public static class SyncManagerExtensions
    {
        public static void InitiatePushSync(this ISyncManager syncManager)
        {
            var _ = syncManager.PushSync();
        }

        public static void InitiatePushSync<T>(this ISyncManager syncManager, T _)
        {
            syncManager.InitiatePushSync();
        }

        public static void InitiateFullSync(this ISyncManager syncManager)
        {
            var _ = syncManager.ForceFullSync();
        }

        public static void InitiateFullSync<T>(this ISyncManager syncManager, T _)
        {
            syncManager.ForceFullSync();
        }
    }
}
