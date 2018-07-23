using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using Toggl.Foundation.MvvmCross.Collections;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.Collections
{
    public sealed class GroupedOrderedCollectionTests
    {
        public sealed class MockItem : IEquatable<MockItem>
        {
            private static int idCount = 0;

            public int Id { get; set; }
            public DateTimeOffset Start { get; set; }

            public static MockItem nextWithStart(DateTimeOffset start)
            {
                idCount += 1;
                return new MockItem { Id = idCount, Start = start };
            }

            public bool Equals(MockItem other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Id == other.Id && Start.Equals(other.Start);
            }
        }

        public sealed class TheConstructor
        {
            DateTimeOffset referenceDate = new DateTimeOffset(2018, 02, 13, 19, 00, 00, TimeSpan.Zero);
            private GroupedOrderedCollection<int> intCollection;
            private GroupedOrderedCollection<MockItem> mockCollection;

            public TheConstructor()
            {
                intCollection = new GroupedOrderedCollection<int>(i => i, i => i, i => i.ToString().Length);
                mockCollection = new GroupedOrderedCollection<MockItem>(d => d.Id, d => d.Start.TimeOfDay, d => d.Start.Date, isDescending: true);
            }

            [Fact, LogIfTooSlow]
            public void ListCanBeEmpty()
            {
                intCollection.IsEmpty.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void SetsTheCorrectOrderForInitialItems()
            {
                int[] array = { 4, 7, 8, 3, 1, 2 };

                intCollection.ReplaceWith(array);

                List<List<int>> expected = new List<List<int>>
                {
                    new List<int> { 1, 2, 3, 4, 7, 8 }
                };

                CollectionAssert.AreEqual(intCollection, expected);
            }

            [Fact, LogIfTooSlow]
            public void SetsTheCorrectDescendingOrderForInitialItems()
            {
                var collection = new GroupedOrderedCollection<int>(i => i, i => i, i => i.ToString().Length, isDescending: true);
                List<int> list = new List<int> { 4, 7, 8, 3, 1, 2 };
                collection.ReplaceWith(list);

                List<List<int>> expected = new List<List<int>>
                {
                    new List<int> { 8, 7, 4, 3, 2, 1 }
                };
                CollectionAssert.AreEqual(collection, expected);
            }

            [Fact, LogIfTooSlow]
            public void SetsTheCorrectOrderForInitialItemsWithDates()
            {

                List<MockItem> list = new List<MockItem>
                {
                    new MockItem { Id = 0, Start = referenceDate.AddHours(3) },
                    new MockItem { Id = 1, Start = referenceDate.AddHours(-10) },
                    new MockItem { Id = 2, Start = referenceDate.AddHours(1) }
                };
                mockCollection.ReplaceWith(list);


                List<List<MockItem>> expected = new List<List<MockItem>>
                {
                    new List<MockItem>
                    {
                        new MockItem { Id = 0, Start = referenceDate.AddHours(3) },
                        new MockItem { Id = 2, Start = referenceDate.AddHours(1) },
                        new MockItem { Id = 1, Start = referenceDate.AddHours(-10) }
                    }
                };
                CollectionAssert.AreEqual(mockCollection, expected);
            }

            [Fact, LogIfTooSlow]
            public void GroupsAndOrdersDates()
            {
                List<MockItem> list = new List<MockItem>
                {
                    new MockItem { Id = 0, Start = referenceDate.AddHours(3) },
                    new MockItem { Id = 1, Start = referenceDate.AddHours(1) },
                    new MockItem { Id = 2, Start = referenceDate.AddHours(-10) },
                    new MockItem { Id = 3, Start = referenceDate.AddDays(5) },
                    new MockItem { Id = 4, Start = referenceDate.AddDays(5).AddHours(2) }
                };

                mockCollection.ReplaceWith(list);

                List<List<MockItem>> expected = new List<List<MockItem>>
                {
                    new List<MockItem>
                    {
                        new MockItem { Id = 4, Start = referenceDate.AddDays(5).AddHours(2) },
                        new MockItem { Id = 3, Start = referenceDate.AddDays(5) }
                    },
                    new List<MockItem>
                    {
                        new MockItem { Id = 0, Start = referenceDate.AddHours(3) },
                        new MockItem { Id = 1, Start = referenceDate.AddHours(1) },
                        new MockItem { Id = 2, Start = referenceDate.AddHours(-10) }
                    }
                };
                CollectionAssert.AreEqual(mockCollection, expected);
            }
        }

        public sealed class TheIndexOfMethod
        {
            private GroupedOrderedCollection<int> intCollection;

            public TheIndexOfMethod()
            {
                intCollection = new GroupedOrderedCollection<int>(i => i, i => i, i => i.ToString().Length);
            }

            [Fact, LogIfTooSlow]
            public void ReturnsCorrectIndex()
            {
                List<int> list = new List<int> { 40, 70, 8, 3, 1, 2 };
                intCollection.ReplaceWith(list);

                var index = intCollection.IndexOf(70);

                var expected = new SectionedIndex(1, 1);
                index.Should().Be(expected);
            }

            [Fact, LogIfTooSlow]
            public void ReturnsNullIfNotExisting()
            {
                List<int> list = new List<int> { 40, 70, 8, 3, 1, 2 };
                intCollection.ReplaceWith(list);

                var index = intCollection.IndexOf(80);

                index.Should().BeNull();
            }

            [Fact, LogIfTooSlow]
            public void ReturnsNullIfEmpty()
            {
                var index = intCollection.IndexOf(80);

                index.Should().BeNull();
            }
        }

        public sealed class TheIndexOfWithIdMethod
        {
            private GroupedOrderedCollection<MockItem> mockCollection;
            private DateTimeOffset referenceDate = new DateTimeOffset(2018, 02, 13, 19, 00, 00, TimeSpan.Zero);

            public TheIndexOfWithIdMethod()
            {
                mockCollection = new GroupedOrderedCollection<MockItem>(d => d.Id, d => d.Start.TimeOfDay, d => d.Start.Date, isDescending: true);
                List<MockItem> list = new List<MockItem>
                {
                    new MockItem { Id = 0, Start = referenceDate.AddHours(3) },
                    new MockItem { Id = 1, Start = referenceDate.AddHours(1) },
                    new MockItem { Id = 2, Start = referenceDate.AddHours(-10) },
                    new MockItem { Id = 3, Start = referenceDate.AddDays(5) },
                    new MockItem { Id = 4, Start = referenceDate.AddDays(5).AddHours(2) }
                };
                mockCollection.ReplaceWith(list);
            }

            [Fact, LogIfTooSlow]
            public void ReturnsCorrectIndex()
            {
                var index = mockCollection.IndexOf(itemId: 2);

                var expected = new SectionedIndex(1, 2);
                index.Should().Be(expected);
            }

            [Fact, LogIfTooSlow]
            public void ReturnsNullIfNotExisting()
            {
                var index = mockCollection.IndexOf(itemId: 8);
                index.Should().BeNull();
            }

            [Fact, LogIfTooSlow]
            public void ReturnsNullIfEmpty()
            {
                var emptyCollection = new GroupedOrderedCollection<MockItem>(d => d.Id, d => d.Start.TimeOfDay, d => d.Start.Date, isDescending: true);
                var index = emptyCollection.IndexOf(itemId: 0);
                index.Should().BeNull();
            }
        }

        public sealed class TheRemoveItemAtMethod
        {
            private GroupedOrderedCollection<int> intCollection;

            public TheRemoveItemAtMethod()
            {
                intCollection = new GroupedOrderedCollection<int>(i => i, i => i, i => i.ToString().Length);
            }

            [Fact, LogIfTooSlow]
            public void ThrowsIfEmpty()
            {
                Action removeItemAt = () => intCollection.RemoveItemAt(0, 0);

                removeItemAt.Should().Throw<ArgumentOutOfRangeException>();
            }

            [Fact, LogIfTooSlow]
            public void ThrowsIfIndexOutOfRange()
            {
                List<int> list = new List<int> { 40, 70, 8, 3, 1, 2 };
                intCollection.ReplaceWith(list);

                Action removeItemAt = () => intCollection.RemoveItemAt(1, 4);

                removeItemAt.Should().Throw<ArgumentOutOfRangeException>();
            }

            [Fact, LogIfTooSlow]
            public void RemovesCorrectItem()
            {
                List<int> list = new List<int> { 40, 70, 8, 3, 1, 2 };
                intCollection.ReplaceWith(list);

                intCollection.RemoveItemAt(0, 3);

                List<List<int>> expected = new List<List<int>>
                {
                    new List<int> { 1, 2, 3 },
                    new List<int> { 40, 70 }
                };
                CollectionAssert.AreEqual(intCollection, expected);
            }

            [Fact, LogIfTooSlow]
            public void ReturnsCorrectItem()
            {
                List<int> list = new List<int> { 40, 70, 8, 3, 1, 2 };
                intCollection.ReplaceWith(list);

                var expected = new SectionedIndex(1, 0);
                intCollection.RemoveItemAt(1, 0).Should().Be(40);
            }

            [Fact, LogIfTooSlow]
            public void RemovesSectionIfEmpty()
            {
                List<int> list = new List<int> { 40, 3, 1, 2 };
                intCollection.ReplaceWith(list);

                intCollection.RemoveItemAt(1, 0);

                List<List<int>> expected = new List<List<int>>
                {
                    new List<int> { 1, 2, 3 }
                };
                CollectionAssert.AreEqual(intCollection, expected);
            }

            [Fact, LogIfTooSlow]
            public void CanRemoveLastItem()
            {
                List<int> list = new List<int> { 40, 70, 3, 1, 2 };
                intCollection.ReplaceWith(list);

                intCollection.RemoveItemAt(1, 1);

                List<List<int>> expected = new List<List<int>>
                {
                    new List<int> { 1, 2, 3 },
                    new List<int> { 40 }
                };
                CollectionAssert.AreEqual(intCollection, expected);
            }

            [Fact, LogIfTooSlow]
            public void CanRemoveFirstItem()
            {
                List<int> list = new List<int> { 40, 70, 3, 1, 2 };
                intCollection.ReplaceWith(list);

                intCollection.RemoveItemAt(0, 0);

                List<List<int>> expected = new List<List<int>>
                {
                    new List<int> { 2, 3 },
                    new List<int> { 40, 70 }
                };
                CollectionAssert.AreEqual(intCollection, expected);
            }
        }

        public sealed class TheInsertItemMethod
        {
            private GroupedOrderedCollection<int> intCollection;

            public TheInsertItemMethod()
            {
                intCollection = new GroupedOrderedCollection<int>(i => i, i => i, i => i.ToString().Length);
            }

            [Fact, LogIfTooSlow]
            public void InsertsElementInCorrectOrder()
            {
                List<int> list = new List<int> { 40, 70, 8, 3, 1, 2 };
                intCollection.ReplaceWith(list);

                intCollection.InsertItem(4);

                List<List<int>> expected = new List<List<int>>
                {
                    new List<int> { 1, 2, 3, 4, 8 },
                    new List<int> { 40, 70 }
                };
                CollectionAssert.AreEqual(intCollection, expected);
            }

            [Fact, LogIfTooSlow]
            public void InsertsElementInCorrectOrderWhenDescending()
            {
                List<int> list = new List<int> { 40, 70, 8, 3, 1, 2 };
                var collection = new GroupedOrderedCollection<int>(i => i, i => i, i => i.ToString().Length, true);
                collection.ReplaceWith(list);

                collection.InsertItem(4);

                List<List<int>> expected = new List<List<int>>
                {
                    new List<int> { 70, 40 },
                    new List<int> { 8, 4, 3, 2, 1 },
                };
                CollectionAssert.AreEqual(collection, expected);
            }

            [Fact, LogIfTooSlow]
            public void CreatesNewSectionIfNeeded()
            {
                List<int> list = new List<int> { 40, 70, 8, 3, 1, 2 };
                intCollection.ReplaceWith(list);

                intCollection.InsertItem(200);

                List<List<int>> expected = new List<List<int>>
                {
                    new List<int> { 1, 2, 3, 8 },
                    new List<int> { 40, 70 },
                    new List<int> { 200 }
                };
                CollectionAssert.AreEqual(intCollection, expected);
            }

            [Fact, LogIfTooSlow]
            public void ReturnsCorrectSectionIndex()
            {
                List<int> list = new List<int> { 40, 70, 8, 3, 1, 2 };
                intCollection.ReplaceWith(list);

                var expected = new SectionedIndex(0, 3);

                intCollection.InsertItem(4).Should().Be(expected);
            }

            [Fact, LogIfTooSlow]
            public void CanInsertLastItemInSection()
            {
                List<int> list = new List<int> { 40, 70, 8, 3, 1, 2 };
                intCollection.ReplaceWith(list);

                intCollection.InsertItem(9);

                List<List<int>> expected = new List<List<int>>
                {
                    new List<int> { 1, 2, 3, 8, 9 },
                    new List<int> { 40, 70 }
                };
                CollectionAssert.AreEqual(intCollection, expected);
            }

            [Fact, LogIfTooSlow]
            public void CanInsertFirstItemInSection()
            {
                List<int> list = new List<int> { 40, 70, 8, 3, 1, 2 };
                intCollection.ReplaceWith(list);

                intCollection.InsertItem(10);

                List<List<int>> expected = new List<List<int>>
                {
                    new List<int> { 1, 2, 3, 8 },
                    new List<int> { 10, 40, 70 }
                };
                CollectionAssert.AreEqual(intCollection, expected);
            }
        }

        public sealed class TheReplaceWithMethod
        {
            [Fact, LogIfTooSlow]
            public void ReplacesTheWholeCollection()
            {
                var collection = new GroupedOrderedCollection<int>(i => i, i => i, i => i.ToString().Length);
                List<int> list = new List<int> { 40, 70, 8, 3, 1, 2 };
                collection.ReplaceWith(list);

                List<int> newList = new List<int> { 300, 100, 10 };
                collection.ReplaceWith(newList);

                List<List<int>> expected = new List<List<int>>
                {
                    new List<int> { 10 },
                    new List<int> { 100, 300 }
                };
                CollectionAssert.AreEqual(collection, expected);
            }
        }
    }
}
