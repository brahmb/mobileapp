using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using MvvmCross.Droid.Support.V7.RecyclerView;
using MvvmCross.Platforms.Android.Binding.BindingContext;
using MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.TemplateSelectors;
using Toggl.Giskard.Views;

namespace Toggl.Giskard.Adapters
{
    public sealed class MainRecyclerAdapter
        : SegmentedRecyclerAdapter<TimeEntryViewModelCollection, TimeEntryViewModel>
    {
        private ISubject<bool> swipeToContinueWasUsedSubject = new Subject<bool>();
        private ISubject<bool> swipeToDeleteWasUsedSubject = new Subject<bool>();

        private CompositeDisposable disposeBag = new CompositeDisposable();

        public bool ShouldShowSuggestions
            => SuggestionsViewModel?.Suggestions.Any() ?? false;

        public SuggestionsViewModel SuggestionsViewModel { get; set; }

        public TimeEntriesLogViewModel TimeEntriesLogViewModel { get; set; }

        public IObservable<bool> SwipeToContinueWasUsedObservable => swipeToContinueWasUsedSubject.AsObservable();

        public IObservable<bool> SwipeToDeleteWasUsedObservable => swipeToDeleteWasUsedSubject.AsObservable();

        private bool isTimeEntryRunning;
        public bool IsTimeEntryRunning
        {
            get => isTimeEntryRunning;
            set
            {
                if (isTimeEntryRunning == value)
                    return;

                isTimeEntryRunning = value;
            }
        }

        public MainRecyclerAdapter()
        {
        }

        public MainRecyclerAdapter(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        protected override int HeaderOffsetForAnimation => ShouldShowSuggestions ? 1 : 0;

        protected override MvxObservableCollection<TimeEntryViewModelCollection> Collection
            => ItemsSource as MvxObservableCollection<TimeEntryViewModelCollection>;

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var itemBindingContext = new MvxAndroidBindingContext(parent.Context, BindingContext.LayoutInflaterHolder);
            var inflatedView = InflateViewForHolder(parent, viewType, itemBindingContext);

            switch (viewType)
            {
                case MainTemplateSelector.Item:
                    return new MainRecyclerViewLogViewHolder(inflatedView, itemBindingContext)
                    {
                        Click = TimeEntriesLogViewModel.EditCommand,
                        ContinueCommand = TimeEntriesLogViewModel.ContinueTimeEntryCommand
                    };

                case MainTemplateSelector.Suggestions:
                    return new MainRecyclerViewSuggestionsViewHolder(inflatedView, itemBindingContext);

                default:
                    return new MvxRecyclerViewHolder(inflatedView, itemBindingContext);
            }

            throw new ArgumentOutOfRangeException(nameof(viewType), $"Invalid viewType provided to {nameof(MainRecyclerAdapter)}");
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            base.OnBindViewHolder(holder, position);

            if (holder is MainRecyclerViewLogViewHolder timeEntriesLogRecyclerViewHolder
                && GetItem(position) is TimeEntryViewModel timeEntry)
            {
                timeEntriesLogRecyclerViewHolder.CanSync = timeEntry.CanSync;
            }
        }

        public override int ItemCount => base.ItemCount + 1 + (ShouldShowSuggestions ? 1 : 0);

        public override object GetItem(int viewPosition)
        {
            if (viewPosition == 0 && ShouldShowSuggestions)
                return SuggestionsViewModel;

            if (viewPosition == ItemCount - 1)
                return IsTimeEntryRunning;

            return base.GetItem(viewPosition - (ShouldShowSuggestions ? 1 : 0));
        }

        internal void ContinueTimeEntry(int viewPosition)
        {
            NotifyItemChanged(viewPosition);
            var timeEntry = GetItem(viewPosition) as TimeEntryViewModel;
            if (timeEntry == null) return;
            TimeEntriesLogViewModel.ContinueTimeEntryCommand.ExecuteAsync(timeEntry);
            swipeToContinueWasUsedSubject.OnNext(true);
        }

        internal void DeleteTimeEntry(int viewPosition)
        {
            var timeEntry = GetItem(viewPosition) as TimeEntryViewModel;
            if (timeEntry == null) return;
            TimeEntriesLogViewModel.DeleteCommand.ExecuteAsync(timeEntry);
            swipeToDeleteWasUsedSubject.OnNext(true);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            disposeBag.Dispose();
        }
    }
}