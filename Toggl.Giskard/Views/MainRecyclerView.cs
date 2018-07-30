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
        private IDisposable firstTimeEntryViewUpdateDisposable;
        private bool isRunningSync;
        private bool canTraverseAdapter = true;

        public MainRecyclerAdapter MainRecyclerAdapter => (MainRecyclerAdapter)Adapter;

        public IObservable<View> FirstTimeEntryView { get; }

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

        public bool IsSyncRunning
        {
            get => isRunningSync;
            set
            {
                isRunningSync = value;
                if (isRunningSync)
                {
                    canTraverseAdapter = false;
                }
            }
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

            FirstTimeEntryView = firstTimeEntryViewSubject
                .AsObservable()
                .DistinctUntilChanged();
        }

        public override void OnScrollStateChanged(int state)
        {
            base.OnScrollStateChanged(state);
            if (!canTraverseAdapter && !isRunningSync)
            {
                canTraverseAdapter = ScrollState == ScrollStateIdle;
            }
        }

        private void onFirstTimeEntryViewUpdate()
        {
            if (!canTraverseAdapter) return;

            var view = findOldestTimeEntryView();
            firstTimeEntryViewSubject.OnNext(view);
        }

        private View findOldestTimeEntryView()
        {
            var layoutManager = (LinearLayoutManager)GetLayoutManager();

            for (var position = MainRecyclerAdapter.ItemCount - 1; position >= 0; position--)
            {
                if (!canTraverseAdapter) return null;

                var item = MainRecyclerAdapter.GetItem(position);
                if (item is TimeEntryViewModel)
                {
                    View view = layoutManager.FindViewByPosition(position);
                    if (view == null || layoutManager.GetItemViewType(view) != MainTemplateSelector.TimeEntry)
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
