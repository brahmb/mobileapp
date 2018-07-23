using System;
using MvvmCross.Droid.Support.V7.RecyclerView.ItemTemplates;
using Toggl.Foundation.Suggestions;

namespace Toggl.Giskard.TemplateSelectors
{
    public sealed class SuggestionsTemplateSelector : IMvxTemplateSelector
    {
        public const int SuggestionWithProject = 0;
        public const int SuggestionWithDescriptionOnly = 1;

        public int ItemTemplateId { get; set; }

        public int GetItemLayoutId(int fromViewType)
        {
            switch (fromViewType)
            {
                case SuggestionWithProject:
                    return Resource.Layout.MainSuggestionsCard;
                case SuggestionWithDescriptionOnly:
                    return Resource.Layout.MainSuggestionsCardDescriptionOnly;
            }
            throw new ArgumentException("Invalid suggestion type.");
        }

        public int GetItemViewType(object forItemObject)
        {
            if (forItemObject is Suggestion suggestion)
            {
                return suggestion.HasProject
                    ? SuggestionWithProject
                    : SuggestionWithDescriptionOnly;
            }

            throw new ArgumentException("All items in the suggestion recycler must be of a type Suggestion");
        }
    }
}
