using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android.Content;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Support.V7.Widget.Helper;
using Android.Util;
using Android.Views;
using MvvmCross.Droid.Support.V7.RecyclerView;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Giskard.Adapters;
using System.Reactive;

namespace Toggl.Giskard.Views
{
    [Register("toggl.giskard.views.TimeEntriesLogRecyclerView")]
    public sealed class MainRecyclerView : MvxRecyclerView
    {
        private BehaviorSubject<MainRecyclerViewLogViewHolder> firstTimeEntryViewHolderSubject =
            new BehaviorSubject<MainRecyclerViewLogViewHolder>(null);

        private BehaviorSubject<MainRecyclerViewLogViewHolder> lastTimeEntryViewHolderSubject =
            new BehaviorSubject<MainRecyclerViewLogViewHolder>(null);

        private IDisposable firstTimeEntryViewHolderUpdateDisposable;
        private IDisposable lastTimeEntryViewHolderUpdateDisposable;

        public MainRecyclerAdapter MainRecyclerAdapter => (MainRecyclerAdapter)Adapter;

        public IObservable<MainRecyclerViewLogViewHolder> FirstTimeEntryViewHolder { get; }
        public IObservable<MainRecyclerViewLogViewHolder> LastTimeEntryViewHolder { get; }

        public SuggestionsViewModel SuggestionsViewModel
        {
            get => MainRecyclerAdapter.SuggestionsViewModel;
            set => MainRecyclerAdapter.SuggestionsViewModel = value;
        }

        public TimeEntriesLogViewModel TimeEntriesLogViewModel
        {
            get => MainRecyclerAdapter.TimeEntriesLogViewModel;
            set => MainRecyclerAdapter.TimeEntriesLogViewModel = value;
        }

        public bool IsTimeEntryRunning
        {
            get => MainRecyclerAdapter.IsTimeEntryRunning;
            set => MainRecyclerAdapter.IsTimeEntryRunning = value;
        }

        public MainRecyclerView(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        public MainRecyclerView(Context context, IAttributeSet attrs)
            : this(context, attrs, 0)
        {
        }

        public MainRecyclerView(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle, new MainRecyclerAdapter())
        {
            SetItemViewCacheSize(20);
            DrawingCacheEnabled = true;
            DrawingCacheQuality = DrawingCacheQuality.High;

            var callback = new MainRecyclerViewTouchCallback(context, this);
            ItemTouchHelper mItemTouchHelper = new ItemTouchHelper(callback);
            mItemTouchHelper.AttachToRecyclerView(this);

            firstTimeEntryViewHolderUpdateDisposable = Observable
                .FromEventPattern<ScrollChangeEventArgs>(e => ScrollChange += e, e => ScrollChange -= e)
                .Select(_ => Unit.Default)
                .Merge(MainRecyclerAdapter.CollectionChange)
                .VoidSubscribe(onFirstTimeEntryViewHolderUpdate);

            lastTimeEntryViewHolderUpdateDisposable = Observable
                .FromEventPattern<ScrollChangeEventArgs>(e => ScrollChange += e, e => ScrollChange -= e)
                .Select(_ => Unit.Default)
                .Merge(MainRecyclerAdapter.CollectionChange)
                .VoidSubscribe(onLastTimeEntryViewHolderUpdate);

            FirstTimeEntryViewHolder = firstTimeEntryViewHolderSubject
                .AsObservable()
                .DistinctUntilChanged();

            LastTimeEntryViewHolder = lastTimeEntryViewHolderSubject
                .AsObservable()
                .DistinctUntilChanged();
        }

        private void onFirstTimeEntryViewHolderUpdate()
        {
            var viewHolder = findOldestTimeEntryViewHolder();
            firstTimeEntryViewHolderSubject.OnNext(viewHolder);
        }

        private void onLastTimeEntryViewHolderUpdate()
        {
            var viewHolder = findNewestTimeEntryViewHolder();
            lastTimeEntryViewHolderSubject.OnNext(viewHolder);
        }

        private MainRecyclerViewLogViewHolder findOldestTimeEntryViewHolder()
        {
            var oldestPosition = MainRecyclerAdapter.ItemCount - 1;
            if (oldestPosition> 0)
            {
                return findViewHOlderAtPosition(oldestPosition);
            }

            return null;
        }

        private MainRecyclerViewLogViewHolder findNewestTimeEntryViewHolder()
        {
            var newestPosition = (MainRecyclerAdapter.ShouldShowSuggestions ? 1 : 0) + 1;
            if (newestPosition < MainRecyclerAdapter.ItemCount)
            {
                return findViewHOlderAtPosition(newestPosition);
            }

            return null;
        }

        private MainRecyclerViewLogViewHolder findViewHOlderAtPosition(int position)
        {
            var layoutManager = (LinearLayoutManager)GetLayoutManager();

            var viewHolder = FindViewHolderForLayoutPosition(position);

            if (viewHolder == null)
                return null;

            var isVisible =
                layoutManager.IsViewPartiallyVisible(viewHolder.ItemView, true, true)
                || layoutManager.IsViewPartiallyVisible(viewHolder.ItemView, false, true);

            if (viewHolder is MainRecyclerViewLogViewHolder logViewHolder && isVisible)
                return logViewHolder;

            return null;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing)
                return;

            firstTimeEntryViewHolderUpdateDisposable?.Dispose();
        }
    }
}
