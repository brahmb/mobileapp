using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using Foundation;
using Toggl.Daneel.Cells;
using Toggl.Daneel.Views;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Multivac.Extensions;
using UIKit;


namespace Toggl.Daneel.ViewSources
{
    public class ReactiveTableViewBinder<TModel, TCell> : IDisposable
    where TCell : BaseTableViewCell<TModel>
    {
        private readonly object animationLock = new object();
        private CompositeDisposable disposeBag = new CompositeDisposable();

        private ReactiveSectionedListTableViewSource<TModel, TCell> dataSource;
        private UITableView tableView;

        public ReactiveTableViewBinder(UITableView tableView, ReactiveSectionedListTableViewSource<TModel, TCell> dataSource)
        {
            this.tableView = tableView;
            this.dataSource = dataSource;

            tableView.Source = dataSource;

            dataSource.CollectionChanges
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(handleCollectionChanges)
                .DisposedBy(disposeBag);
        }

        public void handleCollectionChanges(IEnumerable<CollectionChange> changes)
        {
            lock (animationLock)
            {
                if (changes.Any(c => c.Type == CollectionChangeType.Reload))
                {
                    tableView.ReloadData();
                    return;
                }

                tableView.BeginUpdates();

                foreach (var change in changes)
                {
                    NSIndexPath indexPath = NSIndexPath.FromRowSection(change.Index.Row, change.Index.Section);
                    NSIndexPath[] indexPaths = {indexPath};
                    NSMutableIndexSet indexSet = (NSMutableIndexSet) NSIndexSet.FromIndex(change.Index.Section).MutableCopy();

                    switch (change.Type)
                    {
                        case CollectionChangeType.AddRow:
                            tableView.InsertRows(indexPaths, UITableViewRowAnimation.Automatic);
                            break;

                        case CollectionChangeType.RemoveRow:
                            tableView.DeleteRows(indexPaths, UITableViewRowAnimation.Automatic);
                            break;

                        case CollectionChangeType.UpdateRow:
                            tableView.ReloadRows(indexPaths, UITableViewRowAnimation.Automatic);
                            break;

                        case CollectionChangeType.MoveRow:
                            if (change.OldIndex.HasValue)
                            {
                                NSIndexPath oldIndexPath = NSIndexPath.FromRowSection(change.OldIndex.Value.Row, change.OldIndex.Value.Section);
                                tableView.MoveRow(oldIndexPath, indexPath);
                                dataSource.RefreshHeader(tableView, change.OldIndex.Value.Section);
                            }
                            break;

                        case CollectionChangeType.AddSection:
                            tableView.InsertSections(indexSet, UITableViewRowAnimation.Automatic);
                            break;

                        case CollectionChangeType.RemoveSection:
                            tableView.DeleteSections(indexSet, UITableViewRowAnimation.Automatic);
                            break;
                    }

                    dataSource.RefreshHeader(tableView, change.Index.Section);
                }

                tableView.EndUpdates();
            }
        }

        public void Dispose()
        {
            disposeBag?.Dispose();
            dataSource?.Dispose();
            tableView?.Dispose();
        }
    }
}
