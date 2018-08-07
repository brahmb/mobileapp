using MvvmCross.Droid.Support.V7.RecyclerView.ItemTemplates;

namespace Toggl.Giskard.TemplateSelectors
{
    public sealed class ReportsCalendarShortcutsTemplateSelector : MvxDefaultTemplateSelector
    {
        public ReportsCalendarShortcutsTemplateSelector()
            : base(Resource.Layout.ReportsCalendarShortcutCell) { }
    }
}
