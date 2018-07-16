using System.Collections.Generic;

namespace Toggl.Foundation.MvvmCross.Collections
{
    public interface IGroupOrderedCollection<TItem>
    {
        void Clear();
        void ReplaceWith(IEnumerable<TItem> items);
        TItem ItemAt(int section, int row);
        SectionedIndex? IndexOf(TItem item);
        SectionedIndex? RemoveItem(TItem item);
        TItem RemoveItemAt(int section, int row);
        SectionedIndex InsertItem(TItem item);
    }
}
