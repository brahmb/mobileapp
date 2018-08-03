using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.MvvmCross.Collections
{
    public class ObservableGroupedOrderedCollection<TItem> : IGroupOrderedCollection<TItem>
    {
        private ISubject<IEnumerable<CollectionChange>> collectionChangesSubject = new Subject<IEnumerable<CollectionChange>>();
        private GroupedOrderedCollection<TItem> collection;
        private Func<TItem, IComparable> indexKey;

        public IObservable<IEnumerable<CollectionChange>> CollectionChanges
            => collectionChangesSubject.AsObservable();

        public IObservable<bool> Empty
            => collectionChangesSubject
                .AsObservable()
                .Select(_ => IsEmpty)
                .StartWith(IsEmpty)
                .DistinctUntilChanged();

        public IObservable<int> TotalCount
            => collectionChangesSubject
                .AsObservable()
                .Select(_ => collection.TotalCount)
                .StartWith(0)
                .DistinctUntilChanged();

        public bool IsEmpty
            => collection.IsEmpty;

        public int Count
            => collection.Count;

        public ObservableGroupedOrderedCollection(Func<TItem, IComparable> indexKey, Func<TItem, IComparable> orderingKey, Func<TItem, IComparable> groupingKey, bool descending = false)
        {
            this.indexKey = indexKey;
            collection = new GroupedOrderedCollection<TItem>(indexKey, orderingKey, groupingKey, descending);
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

            var changes = new List<CollectionChange>();
            if (collection[index.Section].Count == 1)
                changes.Add(
                    new CollectionChange { Index = index, Type = CollectionChangeType.AddSection }
                );
            else
                changes.Add(
                    new CollectionChange { Index = index, Type = CollectionChangeType.AddRow }
                );

            collectionChangesSubject.OnNext(changes);

            return index;
        }

        public SectionedIndex? UpdateItem(TItem item)
        {
            var changes = new List<CollectionChange>();

            var oldIndex = collection.IndexOf(indexKey(item));
            var shouldDeleteSection = false;
            var shouldAddSection = false;

            if (oldIndex.HasValue)
            {
                shouldDeleteSection = collection[oldIndex.Value.Section].Count == 1;
                collection.RemoveItemAt(oldIndex.Value.Section, oldIndex.Value.Row);
            }

            var newIndex = collection.InsertItem(item);
            shouldAddSection = collection[newIndex.Section].Count == 1;


            if (oldIndex.HasValue)
            {
                if (oldIndex.Value.Equals(newIndex)) // Not moving, just updating item in place
                {
                    changes.Add(
                        new CollectionChange { Index = newIndex, Type = CollectionChangeType.UpdateRow }
                    );
                }
                else
                {
                    changes.Add(
                        new CollectionChange { Index = newIndex, OldIndex = oldIndex, Type = CollectionChangeType.MoveRow }
                    );
                    if (shouldDeleteSection)
                    {
                        changes.Add(
                            new CollectionChange { Index = oldIndex.Value, Type = CollectionChangeType.RemoveSection }
                        );
                    }
                    if (shouldAddSection)
                    {
                        changes.Add(
                            new CollectionChange { Index = newIndex, Type = CollectionChangeType.AddSection }
                        );
                    }
                }
            }
            else // Not updating, adding a new item
            {
                if (collection[newIndex.Section].Count == 1)
                    changes.Add(
                        new CollectionChange { Index = newIndex, Type = CollectionChangeType.AddSection }
                    );
                else
                    changes.Add(
                        new CollectionChange { Index = newIndex, Type = CollectionChangeType.AddRow }
                    );
            }

            collectionChangesSubject.OnNext(changes);
            return newIndex;
        }

        public void ReplaceWith(IEnumerable<TItem> items)
        {
            collection.ReplaceWith(items);

            var changes = new List<CollectionChange>();
            changes.Add(
                new CollectionChange { Type = CollectionChangeType.Reload }
            );
            collectionChangesSubject.OnNext(changes);
        }

        public TItem RemoveItemAt(int section, int row)
        {
            var index = new SectionedIndex(section, row);

            var changes = new List<CollectionChange>();
            if (collection[section].Count == 1)
                changes.Add(
                    new CollectionChange { Index = index, Type = CollectionChangeType.RemoveSection }
                );
            else
                changes.Add(
                    new CollectionChange { Index = index, Type = CollectionChangeType.RemoveRow }
                );

            var item = collection.RemoveItemAt(section, row);

            collectionChangesSubject.OnNext(changes);

            return item;
        }
    }
}
