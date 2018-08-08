﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.MvvmCross.Collections
{
    public class GroupedOrderedCollection<TItem> : IGroupOrderedCollection<TItem>
    {
        private List<List<TItem>> sections;
        private Func<TItem, IComparable> indexKey;
        private Func<TItem, IComparable> orderingKey;
        private Func<TItem, IComparable> groupingKey;
        private bool isDescending;

        public bool IsEmpty
            => sections.Count == 0;

        public int TotalCount
            => sections.Sum(section => section.Count);

        public int Count
            => sections.Count;

        public GroupedOrderedCollection(
            Func<TItem, IComparable> indexKey,
            Func<TItem, IComparable> orderingKey,
            Func<TItem, IComparable> groupingKey,
            bool isDescending = false)
        {
            this.indexKey = indexKey;
            this.orderingKey = orderingKey;
            this.groupingKey = groupingKey;
            this.isDescending = isDescending;

            sections = new List<List<TItem>> { };
        }

        public IEnumerator<IReadOnlyList<TItem>> GetEnumerator()
            => sections.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public IReadOnlyList<TItem> this[int index]
            => sections[index];

        public SectionedIndex? IndexOf(TItem item)
        {
            var sectionIndex = sections.GroupIndexOf(item, groupingKey);

            if (sectionIndex == -1)
                return null;

            var rowIndex = sections[sectionIndex].IndexOf(item, indexKey);
            if (rowIndex == -1)
                return null;

            return new SectionedIndex(sectionIndex, rowIndex);
        }

        public SectionedIndex? IndexOf(IComparable itemId)
        {
            for (int section = 0; section < sections.Count; section++)
            {
                var row = sections[section].IndexOf(item => indexKey(item).CompareTo(itemId) == 0);
                if (row != -1)
                {
                    return new SectionedIndex(section, row);
                }
            }

            return null;
        }

        public SectionedIndex InsertItem(TItem item)
        {
            var sectionIndex = sections.GroupIndexOf(item, groupingKey);

            if (sectionIndex == -1)
            {
                var insertionIndex = sections.FindLastIndex(g => areInOrder(g.First(), item, groupingKey));
                List<TItem> list = new List<TItem> { item };
                if (insertionIndex == -1)
                {
                    sections.Insert(0, list);
                    return new SectionedIndex(0, 0);
                }
                else
                {
                    sections.Insert(insertionIndex + 1, list);
                    return new SectionedIndex(insertionIndex + 1, 0);
                }
            }
            else
            {
                var rowIndex = sections[sectionIndex].FindLastIndex(i => areInOrder(i, item, orderingKey));
                if (rowIndex == -1)
                {
                    sections[sectionIndex].Insert(0, item);
                    return new SectionedIndex(sectionIndex, 0);
                }
                else
                {
                    sections[sectionIndex].Insert(rowIndex + 1, item);
                    return new SectionedIndex(sectionIndex, rowIndex + 1 );
                }
            }
        }

        public SectionedIndex? UpdateItem(IComparable key, TItem item)
        {
            var oldIndex = IndexOf(key);

            if (!oldIndex.HasValue)
                return null;

            RemoveItemAt(oldIndex.Value.Section, oldIndex.Value.Row);
            return InsertItem(item);
         }

        public void ReplaceWith(IEnumerable<TItem> items)
        {
            sections = items
                .GroupBy(groupingKey)
                .Select(g => g.OrderBy(orderingKey, isDescending).ToList())
                .OrderBy(g => groupingKey(g.First()), isDescending)
                .ToList();
        }

        public TItem RemoveItemAt(int section, int row)
        {
            var item = sections[section][row];
            removeItemFromSection(section, row);
            return item;
        }

        private bool areInOrder(TItem ob1, TItem ob2, Func<TItem, IComparable> key)
        {
            return isDescending
                ? key(ob1).CompareTo(key(ob2)) > 0
                : key(ob1).CompareTo(key(ob2)) < 0;
        }

        private void removeItemFromSection(int section, int row)
        {
            sections[section].RemoveAt(row);

            if (sections[section].Count == 0)
                sections.RemoveAt(section);
        }
    }
}
