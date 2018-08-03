using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Xml.Xsl;
using FluentAssertions;
using Xunit;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using Toggl.Foundation.MvvmCross.Collections;

namespace Toggl.Foundation.Tests.MvvmCross.Collections
{
    public sealed class MockItem : IEquatable<MockItem>
    {
        public int Id { get; set; }
        public string Description { get; set; }

        public override string ToString()
        {
            return $"{Id}: {Description}";
        }

        public bool Equals(MockItem other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id && string.Equals(Description, other.Description);
        }
    }

    public class ObservableGroupedOrderedListTests
    {
        public sealed class TheUpdateMethod : ReactiveTest
        {
            [Fact, LogIfTooSlow]
            public void CanCreateNewSectionWhenMovingItem()
            {
                List<MockItem> list = new List<MockItem>
                {
                    new MockItem { Id = 0, Description = "A" },
                    new MockItem { Id = 1, Description = "B" },
                    new MockItem { Id = 3, Description = "D" },
                    new MockItem { Id = 8, Description = "DFE" }
                };
                var collection = new ObservableGroupedOrderedCollection<MockItem>(i => i.Id, i => i.Description, i => i.Description.Length);
                collection.ReplaceWith(list);

                var scheduler = new TestScheduler();
                var observer = scheduler.CreateObserver<CollectionChange>();

                collection.CollectionChanges.SelectMany( l => l.ToObservable()).Subscribe(observer);

                var updated = new MockItem { Id = 1, Description = "ED" };
                collection.UpdateItem(updated);

                List<List<MockItem>> expected = new List<List<MockItem>>
                {
                    new List<MockItem>
                    {
                        new MockItem { Id = 0, Description = "A" },
                        new MockItem { Id = 3, Description = "D" },
                    },
                    new List<MockItem>
                    {
                        new MockItem { Id = 1, Description = "ED" }
                    },
                    new List<MockItem>
                    {
                        new MockItem { Id = 8, Description = "DFE" }
                    }
                };
                CollectionAssert.AreEqual(collection, expected);
            }

            [Fact, LogIfTooSlow]
            public void CanRemoveASectionWhenMovingItem()
            {
                List<MockItem> list = new List<MockItem>
                {
                    new MockItem { Id = 0, Description = "A" },
                    new MockItem { Id = 1, Description = "B" },
                    new MockItem { Id = 3, Description = "D" },
                    new MockItem { Id = 8, Description = "DFE" }
                };
                var collection = new ObservableGroupedOrderedCollection<MockItem>(i => i.Id, i => i.Description, i => i.Description.Length);
                collection.ReplaceWith(list);

                var scheduler = new TestScheduler();
                var observer = scheduler.CreateObserver<CollectionChange>();

                collection.CollectionChanges.SelectMany( l => l.ToObservable()).Subscribe(observer);

                var updated = new MockItem { Id = 8, Description = "C" };
                collection.UpdateItem(updated);

                List<List<MockItem>> expected = new List<List<MockItem>>
                {
                    new List<MockItem>
                    {
                        new MockItem { Id = 0, Description = "A" },
                        new MockItem { Id = 1, Description = "B" },
                        new MockItem { Id = 8, Description = "C" },
                        new MockItem { Id = 3, Description = "D" }
                    }
                };
                CollectionAssert.AreEqual(collection, expected);
            }
        }

        public sealed class TheCollectionChangesProperty : ReactiveTest
        {
            [Fact, LogIfTooSlow]
            public void SendsEventWhenItemRemoved()
            {
                List<int> list = new List<int> { 40, 70, 8, 3, 1, 2 };
                var collection = new ObservableGroupedOrderedCollection<int>(i => i, i => i, i => i.ToString().Length);
                collection.ReplaceWith(list);

                var scheduler = new TestScheduler();
                var observer = scheduler.CreateObserver<CollectionChange>();

                collection.CollectionChanges.SelectMany( l => l.ToObservable()).Subscribe(observer);

                collection.RemoveItemAt(0, 2);

                var change = new CollectionChange
                {
                    Index = new SectionedIndex(0, 2),
                    Type = CollectionChangeType.RemoveRow
                };

                observer.Messages.AssertEqual(
                    OnNext(0, change)
                );
            }

            [Fact, LogIfTooSlow]
            public void SendsSectionEventWhenLastItemFromSectionRemoved()
            {
                List<int> list = new List<int> { 70, 8, 3, 1, 2 };
                var collection = new ObservableGroupedOrderedCollection<int>(i => i, i => i, i => i.ToString().Length);
                collection.ReplaceWith(list);

                var scheduler = new TestScheduler();
                var observer = scheduler.CreateObserver<CollectionChange>();

                collection.CollectionChanges.SelectMany( l => l.ToObservable()).Subscribe(observer);

                collection.RemoveItemAt(1, 0);

                var change = new CollectionChange
                {
                    Index = new SectionedIndex(1, 0),
                    Type = CollectionChangeType.RemoveSection
                };

                observer.Messages.AssertEqual(
                    OnNext(0, change)
                );
            }

            [Fact, LogIfTooSlow]
            public void SendsEventWhenItemAdded()
            {
                List<int> list = new List<int> { 40, 70, 8, 3, 1, 2 };
                var collection = new ObservableGroupedOrderedCollection<int>(i => i, i => i, i => i.ToString().Length);
                collection.ReplaceWith(list);

                var scheduler = new TestScheduler();
                var observer = scheduler.CreateObserver<CollectionChange>();

                collection.CollectionChanges.SelectMany( l => l.ToObservable()).Subscribe(observer);

                collection.InsertItem(20);

                var change = new CollectionChange
                {
                    Index = new SectionedIndex(1, 0),
                    Type = CollectionChangeType.AddRow
                };

                observer.Messages.AssertEqual(
                    OnNext(0, change)
                );
            }

            [Fact, LogIfTooSlow]
            public void SendsEventWhenFirstItemOfSectionAdded()
            {
                List<int> list = new List<int> { 8, 3, 1, 2 };
                var collection = new ObservableGroupedOrderedCollection<int>(i => i, i => i, i => i.ToString().Length);
                collection.ReplaceWith(list);

                var scheduler = new TestScheduler();
                var observer = scheduler.CreateObserver<CollectionChange>();

                collection.CollectionChanges.SelectMany( l => l.ToObservable()).Subscribe(observer);

                collection.InsertItem(20);

                var change = new CollectionChange
                {
                    Index = new SectionedIndex(1, 0),
                    Type = CollectionChangeType.AddSection
                };

                observer.Messages.AssertEqual(
                    OnNext(0, change)
                );
            }

            [Fact, LogIfTooSlow]
            public void SendsEventWhenReplaced()
            {
                List<int> list = new List<int> { 40, 70, 8, 3, 1, 2 };
                var collection = new ObservableGroupedOrderedCollection<int>(i => i, i => i, i => i.ToString().Length);
                collection.ReplaceWith(list);

                var scheduler = new TestScheduler();
                var observer = scheduler.CreateObserver<CollectionChange>();

                collection.CollectionChanges.SelectMany( l => l.ToObservable()).Subscribe(observer);

                int[] newItems = { 0, 10, 100, 1000 };
                collection.ReplaceWith(newItems);

                var change = new CollectionChange
                {
                    Type = CollectionChangeType.Reload
                };

                observer.Messages.AssertEqual(
                    OnNext(0, change)
                );
            }

            [Fact, LogIfTooSlow]
            public void SendsEventWhenUpdated()
            {
                List<MockItem> list = new List<MockItem>
                {
                    new MockItem { Id = 0, Description = "A" },
                    new MockItem { Id = 1, Description = "B" },
                    new MockItem { Id = 3, Description = "D" }
                };
                var collection = new ObservableGroupedOrderedCollection<MockItem>(i => i.Id, i => i.Description, i => i.Description.Length);
                collection.ReplaceWith(list);

                var scheduler = new TestScheduler();
                var observer = scheduler.CreateObserver<CollectionChange>();

                collection.CollectionChanges.SelectMany( l => l.ToObservable()).Subscribe(observer);

                var updated = new MockItem { Id = 1, Description = "C" };
                collection.UpdateItem(updated);

                var change = new CollectionChange
                {
                    Type = CollectionChangeType.UpdateRow,
                    Index = new SectionedIndex(0, 1)
                };

                observer.Messages.AssertEqual(
                    OnNext(0, change)
                );
            }

            [Fact, LogIfTooSlow]
            public void SendsEventWhenMoved()
            {
                List<MockItem> list = new List<MockItem>
                {
                    new MockItem { Id = 0, Description = "A" },
                    new MockItem { Id = 1, Description = "B" },
                    new MockItem { Id = 3, Description = "D" }
                };
                var collection = new ObservableGroupedOrderedCollection<MockItem>(i => i.Id, i => i.Description, i => i.Description.Length);
                collection.ReplaceWith(list);

                var scheduler = new TestScheduler();
                var observer = scheduler.CreateObserver<CollectionChange>();

                collection.CollectionChanges.SelectMany( l => l.ToObservable()).Subscribe(observer);

                var updated = new MockItem { Id = 1, Description = "E" };
                collection.UpdateItem(updated);

                var change = new CollectionChange
                {
                    Type = CollectionChangeType.MoveRow,
                    Index = new SectionedIndex(0, 2),
                    OldIndex = new SectionedIndex(0, 1)
                };

                observer.Messages.AssertEqual(
                    OnNext(0, change)
                );
            }

            [Fact, LogIfTooSlow]
            public void SendsSectionCreationEventWhenMovedToNewSection()
            {
                List<MockItem> list = new List<MockItem>
                {
                    new MockItem { Id = 0, Description = "A" },
                    new MockItem { Id = 1, Description = "B" },
                    new MockItem { Id = 3, Description = "D" },
                    new MockItem { Id = 8, Description = "DFE" }
                };
                var collection = new ObservableGroupedOrderedCollection<MockItem>(i => i.Id, i => i.Description, i => i.Description.Length);
                collection.ReplaceWith(list);

                var scheduler = new TestScheduler();
                var observer = scheduler.CreateObserver<CollectionChange>();

                collection.CollectionChanges.SelectMany( l => l.ToObservable()).Subscribe(observer);

                var updated = new MockItem { Id = 1, Description = "ED" };
                collection.UpdateItem(updated);

                var change = new CollectionChange
                {
                    Type = CollectionChangeType.MoveRow,
                    Index = new SectionedIndex(1, 0),
                    OldIndex = new SectionedIndex(0, 1)
                };

                var sectionChange = new CollectionChange
                {
                    Type = CollectionChangeType.AddSection,
                    Index = new SectionedIndex(1, 0)
                };

                observer.Messages.AssertEqual(
                    OnNext(0, change),
                    OnNext(0, sectionChange)
                );
            }

            [Fact, LogIfTooSlow]
            public void SendsCreationEventIfUpdateCantFindItem()
            {
                List<MockItem> list = new List<MockItem>
                {
                    new MockItem { Id = 0, Description = "A" },
                    new MockItem { Id = 1, Description = "B" },
                    new MockItem { Id = 2, Description = "C" },
                    new MockItem { Id = 3, Description = "D" }
                };
                var collection = new ObservableGroupedOrderedCollection<MockItem>(i => i.Id, i => i.Description, i => i.Description.Length);
                collection.ReplaceWith(list);

                var scheduler = new TestScheduler();
                var observer = scheduler.CreateObserver<CollectionChange>();

                collection.CollectionChanges.SelectMany( l => l.ToObservable()).Subscribe(observer);

                var updated = new MockItem { Id = 5, Description = "E" };
                collection.UpdateItem(updated);

                var change = new CollectionChange
                {
                    Type = CollectionChangeType.AddRow,
                    Index = new SectionedIndex(0, 4)
                };

                observer.Messages.AssertEqual(
                    OnNext(0, change)
                );
            }

            [Fact, LogIfTooSlow]
            public void SendsSectionCreationEventIfNewItemIsInAnotherSection()
            {
                List<MockItem> list = new List<MockItem>
                {
                    new MockItem { Id = 0, Description = "A" },
                    new MockItem { Id = 1, Description = "B" },
                    new MockItem { Id = 2, Description = "C" },
                    new MockItem { Id = 3, Description = "D" }
                };
                var collection = new ObservableGroupedOrderedCollection<MockItem>(i => i.Id, i => i.Description, i => i.Description.Length);
                collection.ReplaceWith(list);

                var scheduler = new TestScheduler();
                var observer = scheduler.CreateObserver<CollectionChange>();

                collection.CollectionChanges.SelectMany( l => l.ToObservable()).Subscribe(observer);

                var updated = new MockItem { Id = 5, Description = "DE" };
                collection.UpdateItem(updated);

                var change = new CollectionChange
                {
                    Type = CollectionChangeType.AddSection,
                    Index = new SectionedIndex(1, 0)
                };

                observer.Messages.AssertEqual(
                    OnNext(0, change)
                );
            }
        }

        public sealed class TheUpdateItemMethod : ReactiveTest
        {
            [Fact, LogIfTooSlow]
            public void ReturnsTheNewIndex()
            {
                List<MockItem> list = new List<MockItem>
                {
                    new MockItem { Id = 0, Description = "A" },
                    new MockItem { Id = 1, Description = "B" },
                    new MockItem { Id = 2, Description = "C" },
                    new MockItem { Id = 3, Description = "D" }
                };
                var collection = new ObservableGroupedOrderedCollection<MockItem>(i => i.Id, i => i.Description, i => i.Description.Length);
                collection.ReplaceWith(list);

                var scheduler = new TestScheduler();
                var observer = scheduler.CreateObserver<CollectionChange>();

                collection.CollectionChanges.SelectMany( l => l.ToObservable()).Subscribe(observer);

                var updated = new MockItem { Id = 1, Description = "B2" };
                var index = collection.UpdateItem(updated);

                index.HasValue.Should().BeTrue();
                index.Value.Section.Should().Be(1);
                index.Value.Row.Should().Be(0);
            }
        }

        public sealed class TheEmptyObservableProperty : ReactiveTest
        {
            [Fact, LogIfTooSlow]
            public void UpdatesAccordingly()
            {
                List<int> list = new List<int>();
                var collection = new ObservableGroupedOrderedCollection<int>(i => i, i => i, i => i.ToString().Length);
                collection.ReplaceWith(list);

                var scheduler = new TestScheduler();
                var observer = scheduler.CreateObserver<bool>();

                collection.Empty.Subscribe(observer);

                collection.InsertItem(20);
                collection.InsertItem(2);
                collection.RemoveItemAt(0, 0);
                collection.RemoveItemAt(0, 0);

                observer.Messages.AssertEqual(
                    OnNext(0, true),
                    OnNext(0, false),
                    OnNext(0, true)
                );
            }
        }

        public sealed class TheTotalCountObservableProperty : ReactiveTest
        {
            [Fact, LogIfTooSlow]
            public void UpdatesAccordingly()
            {
                List<int> list = new List<int>();
                var collection = new ObservableGroupedOrderedCollection<int>(i => i, i => i, i => i.ToString().Length);
                collection.ReplaceWith(list);

                var scheduler = new TestScheduler();
                var observer = scheduler.CreateObserver<int>();

                collection.TotalCount.Subscribe(observer);

                collection.InsertItem(20);
                collection.InsertItem(2);
                collection.RemoveItemAt(0, 0);
                collection.RemoveItemAt(0, 0);

                observer.Messages.AssertEqual(
                    OnNext(0, 0),
                    OnNext(0, 1),
                    OnNext(0, 2),
                    OnNext(0, 1),
                    OnNext(0, 0)
                );
            }
        }
    }
}
