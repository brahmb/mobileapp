using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.MvvmCross.Collections
{
    public struct SectionedIndex
    {
        public int Section { get; set; }
        public int Row { get; set; }
    }

    public class GroupedOrderedCollection<TItem> : IGroupOrderedCollection<TItem>
    {
        private List<List<TItem>> sections;
        private Func<TItem, IComparable> indexKey;
        private Func<TItem, IComparable> orderingKey;
        private Func<TItem, IComparable> groupingKey;
        private bool isDescending;

        public IReadOnlyList<IReadOnlyList<TItem>> Items
            => sections;

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

        public void Clear()
        {
            sections = new List<List<TItem>> { };
        }

        public void ReplaceWith(IEnumerable<TItem> items)
        {
            sections = items
                .GroupBy(groupingKey)
                .Select(g => g.OrderBy(orderingKey, isDescending).ToList())
                .OrderBy( g => groupingKey(g.First()), isDescending)
                .ToList();
        }

        public TItem ItemAt(int section, int row)
        {
            return sections[section][row];
        }

        public SectionedIndex? IndexOf(TItem item)
        {
            var sectionIndex = sections.GroupIndexOf(item, groupingKey);

            if (sectionIndex == -1)
                return null;

            var rowIndex = sections[sectionIndex].IndexOf(item, indexKey);
            if (rowIndex == -1)
                return null;

            return new SectionedIndex { Section = sectionIndex, Row = rowIndex };
        }

        public SectionedIndex? RemoveItem(TItem item)
        {
            var index = IndexOf(item);

            if (!index.HasValue)
                return null;

            removeItemFromSection(index.Value.Section, index.Value.Row);

            return index.Value;
        }

        public TItem RemoveItemAt(int section, int row)
        {
            var item = sections[section][row];
            removeItemFromSection(section, row);
            return item;
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
                    return new SectionedIndex { Section = 0, Row = 0 };
                }
                else
                {
                    sections.Insert(insertionIndex + 1, list);
                    return new SectionedIndex { Section = insertionIndex + 1, Row = 0 };
                }
            }
            else
            {
                var rowIndex = sections[sectionIndex].FindLastIndex(i => areInOrder(i, item, orderingKey));
                if (rowIndex == -1)
                {
                    sections[sectionIndex].Insert(0, item);
                    return new SectionedIndex { Section = sectionIndex, Row = 0 };
                }
                else
                {
                    sections[sectionIndex].Insert(rowIndex + 1, item);
                    return new SectionedIndex { Section = sectionIndex, Row = rowIndex + 1 };
                }
            }
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
