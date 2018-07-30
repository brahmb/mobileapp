using System;
using System.Reactive;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Android.Runtime;
using MvvmCross.Base;
using MvvmCross.Droid.Support.V7.RecyclerView;
using MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.Collections;

namespace Toggl.Giskard.Adapters
{
    public abstract class SegmentedRecyclerAdapter<TCollection, TItem> : MvxRecyclerAdapter
        where TCollection : MvxObservableCollection<TItem>
    {
        private BehaviorSubject<Unit> collectionChangedSubject = new BehaviorSubject<Unit>(Unit.Default);

        private readonly object headerListLock = new object();
        private readonly List<int> headerIndexes = new List<int>();

        private int? cachedItemCount;

        protected NestableObservableCollection<TCollection, TItem> AnimatableCollection
            => ItemsSource as NestableObservableCollection<TCollection, TItem>;

        protected abstract MvxObservableCollection<TCollection> Collection { get; }

        public IObservable<Unit> CollectionChange { get; }

        public override IEnumerable ItemsSource
        {
            get => base.ItemsSource;
            set
            {
                if (AnimatableCollection != null)
                {
                    AnimatableCollection.OnChildCollectionChanged -= OnChildCollectionChanged;
                }

                base.ItemsSource = value;

                if (AnimatableCollection != null)
                {
                    AnimatableCollection.OnChildCollectionChanged += OnChildCollectionChanged;
                }
            }
        }

        protected SegmentedRecyclerAdapter()
        {
            CollectionChange = collectionChangedSubject.AsObservable();
        }

        protected SegmentedRecyclerAdapter(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        protected override void SetItemsSource(IEnumerable value)
        {
            base.SetItemsSource(value);

            calculateHeaderIndexes();
        }

        protected override void OnItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsSourceCollectionChanged(sender, e);

            calculateHeaderIndexes();

            collectionChangedSubject.OnNext(Unit.Default);
        }

        protected void OnChildCollectionChanged(object sender, ChildCollectionChangedEventArgs args)
        {
            calculateHeaderIndexes();

            MvxSingleton<IMvxMainThreadDispatcher>
                .Instance
                .RequestMainThreadAction(() => notifyForChanges(args));
                
            collectionChangedSubject.OnNext(Unit.Default);
        }

        private void notifyForChanges(ChildCollectionChangedEventArgs args)
        {
            var groupHeaderIndex = default(int);

            lock (headerListLock)
            {
                groupHeaderIndex = headerIndexes[args.CollectionIndex];
            }

            NotifyItemChanged(groupHeaderIndex + HeaderOffsetForAnimation);

            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var index in args.Indexes)
                    {
                        var itemIndex = groupHeaderIndex + index + 1 + HeaderOffsetForAnimation;
                        NotifyItemInserted(itemIndex);
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (var index in args.Indexes)
                    {
                        var itemIndex = groupHeaderIndex + index + 1 + HeaderOffsetForAnimation;
                        NotifyItemRemoved(itemIndex);
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    foreach (var index in args.Indexes)
                    {
                        var itemIndex = groupHeaderIndex + index + 1 + HeaderOffsetForAnimation;
                        NotifyItemChanged(itemIndex);
                    }
                    break;
            }
        }

        protected virtual int HeaderOffsetForAnimation => 0;

        public override object GetItem(int viewPosition)
        {
            try
            {
                var item = tryGetItem(viewPosition);
                return item;
            }
            catch
            {
                calculateHeaderIndexes();

                if (viewPosition > ItemCount) return null;

                var item = tryGetItem(viewPosition);
                return item;
            }
        }

        public override int ItemCount
        {
            get
            {
                if (cachedItemCount == null)
                {
                    var collection = Collection;
                    if (collection == null) return 0;

                    var itemCount = collection.Count + collection.Sum(g => g.Count);
                    cachedItemCount = itemCount;
                }

                return cachedItemCount.Value;
            }
        }

        private void calculateHeaderIndexes()
        {
            lock (headerListLock)
            {
                cachedItemCount = null;
                headerIndexes.Clear();

                var collection = Collection;
                if (collection == null) return;

                var index = 0;
                foreach (var nestedCollection in collection)
                {
                    headerIndexes.Add(index);
                    index += nestedCollection.Count + 1;
                }
            }
        }

        private object tryGetItem(int viewPosition)
        {
            var collection = Collection;
            if (collection == null)
                return null;

            if (collection.Count == 0)
                return null;

            lock (headerListLock)
            {
                var groupIndex = headerIndexes.IndexOf(viewPosition);
                if (groupIndex >= 0)
                    return collection[groupIndex];

                var currentGroupIndex = headerIndexes.FindLastIndex(index => index < viewPosition);
                var offset = headerIndexes[currentGroupIndex] + 1;
                var indexInGroup = viewPosition - offset;
                return collection[currentGroupIndex][indexInGroup];
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing)
                return;

            collectionChangedSubject?.Dispose();

            if (AnimatableCollection == null)
                return;

            AnimatableCollection.OnChildCollectionChanged -= OnChildCollectionChanged;
        }
    }
}
