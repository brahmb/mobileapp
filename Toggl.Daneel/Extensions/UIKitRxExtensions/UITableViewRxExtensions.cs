using System;
using Toggl.Daneel.Cells;
using Toggl.Daneel.ViewSources;
using UIKit;

namespace Toggl.Daneel.Extensions
{
    public static partial class UIKitRxExtensions
    {
        public static IDisposable Bind<TModel, TCell>(this UITableView tableView, ReactiveSectionedListTableViewSource<TModel, TCell> dataSource)
        where TCell : BaseTableViewCell<TModel>
        {
            return new ReactiveTableViewBinder<TModel, TCell>(tableView, dataSource);
        }
    }
}
