namespace Toggl.Foundation.Sync
{
    public sealed class StateMachineEntryPoints
    {
        public StateResult StartPullSync { get; } = new StateResult();
        public StateResult StartPushSync { get; } = new StateResult();
        public StateResult StartCleanUp { get; } = new StateResult();
    }
}
