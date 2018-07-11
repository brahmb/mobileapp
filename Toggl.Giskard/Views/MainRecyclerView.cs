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
using Toggl.Giskard.TemplateSelectors;
using System.Reactive;

namespace Toggl.Giskard.Views
{
    [Register("toggl.giskard.views.TimeEntriesLogRecyclerView")]
    public sealed class MainRecyclerView : MvxRecyclerView
    {
        private BehaviorSubject<View> firstTimeEntryViewSubject = new BehaviorSubject<View>(null);
        private BehaviorSubject<View> lastTimeEntryViewSubject = new BehaviorSubject<View>(null);
        private IDisposable firstTimeEntryViewUpdateDisposable;
        private IDisposable lastTimeEntryViewUpdateDisposable;

        public MainRecyclerAdapter MainRecyclerAdapter => (MainRecyclerAdapter)Adapter;

        public IObservable<View> FirstTimeEntryView { get; }
        public IObservable<View> LastTimeEntryView { get; }

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

            firstTimeEntryViewUpdateDisposable = Observable
                .FromEventPattern<ScrollChangeEventArgs>(e => ScrollChange += e, e => ScrollChange -= e)
                .Select(_ => Unit.Default)
                .Merge(MainRecyclerAdapter.CollectionChange)
                .VoidSubscribe(onFirstTimeEntryViewUpdate);

            lastTimeEntryViewUpdateDisposable = Observable
                .FromEventPattern<ScrollChangeEventArgs>(e => ScrollChange += e, e => ScrollChange -= e)
                .Select(_ => Unit.Default)
                .Merge(MainRecyclerAdapter.CollectionChange)
                .VoidSubscribe(onLastTimeEntryViewUpdate);

            FirstTimeEntryView = firstTimeEntryViewSubject
                .AsObservable()
                .DistinctUntilChanged();

            LastTimeEntryView = lastTimeEntryViewSubject
                .AsObservable()
                .DistinctUntilChanged();
        }

        private void onFirstTimeEntryViewUpdate()
        {
            var view = findOldestTimeEntryView();
            firstTimeEntryViewSubject.OnNext(view);
        }

        private void onLastTimeEntryViewUpdate()
        {
            var view = findNewestTimeEntryView();
            lastTimeEntryViewSubject.OnNext(view);
        }

        private View findOldestTimeEntryView()
        {
            var layoutManager = (LinearLayoutManager)GetLayoutManager();

            for (var position = MainRecyclerAdapter.ItemCount - 1; position >= 0; position--)
            {
                var item = MainRecyclerAdapter.GetItem(position);
                if (item is TimeEntryViewModel)
                {
                    View view = layoutManager.FindViewByPosition(position);
                    if (view == null || layoutManager.GetItemViewType(view) != MainTemplateSelector.Item)
                        return null;

                    var isVisible =
                        layoutManager.IsViewPartiallyVisible(view, true, true)
                        || layoutManager.IsViewPartiallyVisible(view, false, true);

                    return isVisible ? view : null;
                }
            }

            return null;
        }

        private View findNewestTimeEntryView()
        {
            var layoutManager = (LinearLayoutManager)GetLayoutManager();

            for (var position = 0; position <= MainRecyclerAdapter.ItemCount - 1; position++)
            {
                var item = MainRecyclerAdapter.GetItem(position);
                if (item is TimeEntryViewModel)
                {
                    View view = layoutManager.FindViewByPosition(position);
                    if (view == null || layoutManager.GetItemViewType(view) != MainTemplateSelector.Item)
                        return null;

                    var isVisible =
                        layoutManager.IsViewPartiallyVisible(view, true, true)
                        || layoutManager.IsViewPartiallyVisible(view, false, true);

                    return isVisible ? view : null;
                }
            }

            return null;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing)
                return;

            firstTimeEntryViewUpdateDisposable?.Dispose();
        }
    }
}
