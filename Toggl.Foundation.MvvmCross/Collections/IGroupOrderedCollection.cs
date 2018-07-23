using System;
using System.Collections.Generic;

namespace Toggl.Foundation.MvvmCross.Collections
{
    public interface IGroupOrderedCollection<TItem> : IReadOnlyList<IReadOnlyList<TItem>>
    {
        bool IsEmpty { get; }
        int Count { get; }

        SectionedIndex? IndexOf(TItem item);
        SectionedIndex? IndexOf(IComparable itemId);

        SectionedIndex InsertItem(TItem item);
        SectionedIndex? UpdateItem(TItem item);
        void ReplaceWith(IEnumerable<TItem> items);
        TItem RemoveItemAt(int section, int row);
    }
}
