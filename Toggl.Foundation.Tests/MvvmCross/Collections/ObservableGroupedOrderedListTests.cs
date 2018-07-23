using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using FluentAssertions;
using Xunit;
using Microsoft.Reactive.Testing;
using Toggl.Foundation.MvvmCross.Collections;

namespace Toggl.Foundation.Tests.MvvmCross.Collections
{
    public sealed class MockItem
    {
        public int Id { get; set; }
        public string Description { get; set; }
    }

    public class ObservableGroupedOrderedListTests
    {
        public sealed class TheCollectionChangesProperty : ReactiveTest
        {
            [Fact, LogIfTooSlow]
            public void SendsEventWhenItemRemoved()
            {
                List<int> list = new List<int> { 40, 70, 8, 3, 1, 2 };
                var collection = new ObservableGroupedOrderedCollection<int>(i => i, i => i, i => i.ToString().Length, list);

                var scheduler = new TestScheduler();
                var observer = scheduler.CreateObserver<CollectionChange>();

                collection.CollectionChanges.Subscribe(observer);

                collection.RemoveItemAt(0, 2);

                var change = new CollectionChange
                {
                    Index = new SectionedIndex(0, 2),
                    Action = NotifyCollectionChangedAction.Remove
                };

                observer.Messages.AssertEqual(
                    OnNext(0, change)
                );
            }

            [Fact, LogIfTooSlow]
            public void SendsEventWhenItemAdded()
            {
                List<int> list = new List<int> { 40, 70, 8, 3, 1, 2 };
                var collection = new ObservableGroupedOrderedCollection<int>(i => i, i => i, i => i.ToString().Length, list);

                var scheduler = new TestScheduler();
                var observer = scheduler.CreateObserver<CollectionChange>();

                collection.CollectionChanges.Subscribe(observer);

                collection.InsertItem(20);

                var change = new CollectionChange
                {
                    Index = new SectionedIndex(1, 0),
                    Action = NotifyCollectionChangedAction.Add
                };

                observer.Messages.AssertEqual(
                    OnNext(0, change)
                );
            }

            [Fact, LogIfTooSlow]
            public void SendsEventWhenReplaced()
            {
                List<int> list = new List<int> { 40, 70, 8, 3, 1, 2 };
                var collection = new ObservableGroupedOrderedCollection<int>(i => i, i => i, i => i.ToString().Length, list);

                var scheduler = new TestScheduler();
                var observer = scheduler.CreateObserver<CollectionChange>();

                collection.CollectionChanges.Subscribe(observer);

                int[] newItems = { 0, 10, 100, 1000 };
                collection.ReplaceWith(newItems);

                var change = new CollectionChange
                {
                    Action = NotifyCollectionChangedAction.Reset
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
                    new MockItem { Id = 2, Description = "C" },
                    new MockItem { Id = 3, Description = "D" }
                };
                var collection = new ObservableGroupedOrderedCollection<MockItem>(i => i.Id, i => i.Description, i => i.Description.Length, list);

                var scheduler = new TestScheduler();
                var observer = scheduler.CreateObserver<CollectionChange>();

                collection.CollectionChanges.Subscribe(observer);

                var updated = new MockItem { Id = 1, Description = "B2" };
                collection.UpdateItem(updated);

                var change = new CollectionChange
                {
                    Action = NotifyCollectionChangedAction.Replace,
                    OldIndex = new SectionedIndex(0, 1),
                    Index = new SectionedIndex(1, 0)
                };

                observer.Messages.AssertEqual(
                    OnNext(0, change)
                );
            }

            [Fact, LogIfTooSlow]
            public void DoesntSendEventIfUpdateCantFindItem()
            {
                List<MockItem> list = new List<MockItem>
                {
                    new MockItem { Id = 0, Description = "A" },
                    new MockItem { Id = 1, Description = "B" },
                    new MockItem { Id = 2, Description = "C" },
                    new MockItem { Id = 3, Description = "D" }
                };
                var collection = new ObservableGroupedOrderedCollection<MockItem>(i => i.Id, i => i.Description, i => i.Description.Length, list);

                var scheduler = new TestScheduler();
                var observer = scheduler.CreateObserver<CollectionChange>();

                collection.CollectionChanges.Subscribe(observer);

                var updated = new MockItem { Id = 5, Description = "B2" };
                collection.UpdateItem(updated);

                observer.Messages.Should().BeEmpty();
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
                var collection = new ObservableGroupedOrderedCollection<MockItem>(i => i.Id, i => i.Description, i => i.Description.Length, list);

                var scheduler = new TestScheduler();
                var observer = scheduler.CreateObserver<CollectionChange>();

                collection.CollectionChanges.Subscribe(observer);

                var updated = new MockItem { Id = 1, Description = "B2" };
                var index = collection.UpdateItem(updated);

                index.HasValue.Should().BeTrue();
                index.Value.Section.Should().Be(1);
                index.Value.Row.Should().Be(0);
            }

            [Fact, LogIfTooSlow]
            public void ReturnsNullIfCantFindItem()
            {
                List<MockItem> list = new List<MockItem>
                {
                    new MockItem { Id = 0, Description = "A" },
                    new MockItem { Id = 1, Description = "B" },
                    new MockItem { Id = 2, Description = "C" },
                    new MockItem { Id = 3, Description = "D" }
                };
                var collection = new ObservableGroupedOrderedCollection<MockItem>(i => i.Id, i => i.Description, i => i.Description.Length, list);

                var scheduler = new TestScheduler();
                var observer = scheduler.CreateObserver<CollectionChange>();

                collection.CollectionChanges.Subscribe(observer);

                var updated = new MockItem { Id = 5, Description = "B2" };
                var index = collection.UpdateItem(updated);

                index.HasValue.Should().BeFalse();
            }
        }
    }
}
