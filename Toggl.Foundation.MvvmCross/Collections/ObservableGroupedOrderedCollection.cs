using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.MvvmCross.Collections
{
    public struct CollectionChange
    {
        public SectionedIndex Index;
        public SectionedIndex OldIndex;
        public NotifyCollectionChangedAction Action;
    }

    public class ObservableGroupedOrderedCollection<TItem> : IGroupOrderedCollection<TItem>
    {
        private ISubject<CollectionChange> collectionChangesSubject = new Subject<CollectionChange>();
        private GroupedOrderedCollection<TItem> collection;
        private Func<TItem, IComparable> indexKey;

        public IObservable<CollectionChange> CollectionChanges
            => collectionChangesSubject.AsObservable();

        public bool IsEmpty
            => collection.IsEmpty;

        public int Count
            => collection.Count;

        public ObservableGroupedOrderedCollection(Func<TItem, IComparable> indexKey, Func<TItem, IComparable> orderingKey, Func<TItem, IComparable> groupingKey, IList<TItem> initialItems = null, bool descending = false)
        {
            this.indexKey = indexKey;
            collection = new GroupedOrderedCollection<TItem>(indexKey, orderingKey, groupingKey, descending);
            collection.ReplaceWith(initialItems);
        }

        public IEnumerator<IReadOnlyList<TItem>> GetEnumerator()
            => collection.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public IReadOnlyList<TItem> this[int index]
            => collection[index];

        public SectionedIndex? IndexOf(TItem item)
        {
            return collection.IndexOf(item);
        }

        public SectionedIndex? IndexOf(IComparable itemId)
        {
            return collection.IndexOf(itemId);
        }

        public SectionedIndex InsertItem(TItem item)
        {
            var index = collection.InsertItem(item);

            var change = new CollectionChange { Index = index, Action = NotifyCollectionChangedAction.Add };
            collectionChangesSubject.OnNext(change);

            return index;
        }

        public SectionedIndex? UpdateItem(TItem item)
        {
            var oldIndex = collection.IndexOf(indexKey(item));

            if (!oldIndex.HasValue)
                return null;

            collection.RemoveItemAt(oldIndex.Value.Section, oldIndex.Value.Row);
            var newIndex = collection.InsertItem(item);

            var change = new CollectionChange { Index = newIndex, OldIndex = oldIndex.Value, Action = NotifyCollectionChangedAction.Replace };
            collectionChangesSubject.OnNext(change);

            return newIndex;
        }

        public void ReplaceWith(IEnumerable<TItem> items)
        {
            collection.ReplaceWith(items);

            var change = new CollectionChange { Action = NotifyCollectionChangedAction.Reset };
            collectionChangesSubject.OnNext(change);
        }

        public TItem RemoveItemAt(int section, int row)
        {
            var item = collection.RemoveItemAt(section, row);

            var change = new CollectionChange { Index = new SectionedIndex(section, row), Action = NotifyCollectionChangedAction.Remove };
            collectionChangesSubject.OnNext(change);

            return item;
        }
    }
}
