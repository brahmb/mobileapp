using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Foundation;
using Toggl.Daneel.Cells;
using UIKit;

namespace Toggl.Daneel.ViewSources
{
    public class SectionedListTableViewSource<TModel, TCell> : UITableViewSource
        where TCell : BaseTableViewCell<TModel>
    {
        private readonly string cellIdentifier;
        protected readonly IReadOnlyList<IReadOnlyList<TModel>> items;

        public Action<TModel> OnItemTapped { get; set; }

        public SectionedListTableViewSource(IReadOnlyList<IReadOnlyList<TModel>> items, string cellIdentifier)
        {
            this.items = items;
            this.cellIdentifier = cellIdentifier;
        }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var cell = tableView.DequeueReusableCell(cellIdentifier, indexPath) as BaseTableViewCell<TModel>;
            cell.Item = items[indexPath.Section][indexPath.Row];
            return cell;
        }

        public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            OnItemTapped?.Invoke(items[indexPath.Section][indexPath.Row]);
        }

        public override nint NumberOfSections(UITableView tableView)
            => items.Count;

        public override nint RowsInSection(UITableView tableview, nint section)
            => items[(int)section].Count;
    }
}
