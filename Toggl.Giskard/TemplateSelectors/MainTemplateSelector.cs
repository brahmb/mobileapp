using MvvmCross.Droid.Support.V7.RecyclerView.ItemTemplates;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.ViewModels;

namespace Toggl.Giskard.TemplateSelectors
{
    public sealed class MainTemplateSelector : IMvxTemplateSelector
    {
        public const int Header = 0;
        public const int TimeEntry = 1;
        public const int Footer = 2;
        public const int Suggestions = 3;

        public int ItemTemplateId { get; set; }

        public int GetItemLayoutId(int fromViewType)
        {
            if (fromViewType == Header)
                return Resource.Layout.MainLogHeader;

            if (fromViewType == Footer)
                return Resource.Layout.MainLogFooter;

            if (fromViewType == TimeEntry)
                return Resource.Layout.MainLogCell;

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

            return TimeEntry;
        }
    }
}
