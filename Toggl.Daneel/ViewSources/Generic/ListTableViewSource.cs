using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Foundation;
using Toggl.Daneel.Cells;
using UIKit;

namespace Toggl.Daneel.ViewSources
{
    public class ListTableViewSource<TModel, TCell> : UITableViewSource
        where TCell : BaseTableViewCell<TModel>
    {
        private readonly string cellIdentifier;
        private readonly IImmutableList<TModel> items;

        public Func<TModel, Task> OnItemTapped { get; set; }

        public ListTableViewSource(IImmutableList<TModel> items, string cellIdentifier)
        {
            this.items = items;
            this.cellIdentifier = cellIdentifier;
        }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var cell = tableView.DequeueReusableCell(cellIdentifier, indexPath) as BaseTableViewCell<TModel>;
            cell.Item = items[indexPath.Row];
            return cell;
        }

        public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            OnItemTapped?.Invoke(items[indexPath.Row]);
        }

        public override nint RowsInSection(UITableView tableview, nint section) 
            => items.Count;
    }
}
