using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CoreGraphics;
using Foundation;
using Toggl.Daneel.Cells;
using Toggl.Foundation.MvvmCross.Collections;
using UIKit;

namespace Toggl.Daneel.ViewSources
{
    public interface IObservableScroll
    {
        IObservable<CGPoint> ScrollOffset { get; }
        IObservable<bool> IsDragging { get; }
    }

    public class ReactiveSectionedListTableViewSource<TModel, TCell> : SectionedListTableViewSource<TModel, TCell>, IObservableScroll
        where TCell : BaseTableViewCell<TModel>
    {
        protected ObservableGroupedOrderedCollection<TModel> collection;

        public IObservable<IEnumerable<CollectionChange>> CollectionChanges
            => collection.CollectionChanges;

        public IObservable<TModel> ItemSelected
            => itemSelectedSubject.AsObservable();

        public IObservable<CGPoint> ScrollOffset
            => scrolledSubject.AsObservable();

        public IObservable<bool> IsDragging
            => isDraggingSubject.AsObservable();

        private Subject<TModel> itemSelectedSubject = new Subject<TModel>();
        private Subject<CGPoint> scrolledSubject = new Subject<CGPoint>();
        private Subject<bool> isDraggingSubject = new Subject<bool>();

        public ReactiveSectionedListTableViewSource(ObservableGroupedOrderedCollection<TModel> collection, string cellIdentifier) : base (collection, cellIdentifier)
        {
            this.collection = collection;

            OnItemTapped = ItemTapped;
        }

        private void ItemTapped(TModel model)
        {
            itemSelectedSubject.OnNext(model);
        }

        public virtual void RefreshHeader(UITableView tableView, int section)
        {
        }

        public override void Scrolled(UIScrollView scrollView)
        {
            scrolledSubject.OnNext(scrollView.ContentOffset);
        }

        public override void DraggingStarted(UIScrollView scrollView)
        {
            isDraggingSubject.OnNext(true);
        }

        public override void DraggingEnded(UIScrollView scrollView, bool willDecelerate)
        {
            isDraggingSubject.OnNext(false);
        }
    }
}
