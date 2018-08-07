using MvvmCross.Droid.Support.V7.RecyclerView.ItemTemplates;

namespace Toggl.Giskard.TemplateSelectors
{
    public sealed class ReportsCalendarTemplateSelector : MvxDefaultTemplateSelector
    {
        public ReportsCalendarTemplateSelector()
            : base(Resource.Layout.ReportsCalendarFragmentDayCell) { }
    }
}
