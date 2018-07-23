using MvvmCross.Droid.Support.V7.RecyclerView.ItemTemplates;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.ViewModels;

namespace Toggl.Giskard.TemplateSelectors
{
    public sealed class MainTemplateSelector : IMvxTemplateSelector
    {
        public const int Header = 0;
        public const int TimeEntry = 1;
        public const int TimeEntryDescriptionOnly = 2;
        public const int Footer = 3;
        public const int Suggestions = 4;

        public int ItemTemplateId { get; set; }

        public int GetItemLayoutId(int fromViewType)
        {
            if (fromViewType == Header)
                return Resource.Layout.MainLogHeader;

            if (fromViewType == Footer)
                return Resource.Layout.MainLogFooter;

            if (fromViewType == TimeEntry)
                return Resource.Layout.MainLogCell;

            if (fromViewType == TimeEntryDescriptionOnly)
                return Resource.Layout.MainLogCellDescriptionOnly;

            return Resource.Layout.MainSuggestions;
        }

        public int GetItemViewType(object forItemObject)
        {
            if (forItemObject is bool)
                return Footer;

            if (forItemObject is TimeEntryViewModelCollection)
                return Header;

            if (forItemObject is SuggestionsViewModel)
                return Suggestions;

            if (forItemObject is TimeEntryViewModel timeEntry && !timeEntry.HasProject)
                return TimeEntryDescriptionOnly;

            return TimeEntry;
        }
    }
}
