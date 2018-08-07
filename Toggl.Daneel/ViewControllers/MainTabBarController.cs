using System;
using System.Collections.Generic;
using System.Linq;
using MvvmCross.Platforms.Ios.Presenters.Attributes;
using MvvmCross.Platforms.Ios.Views;
using MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;

namespace Toggl.Daneel.ViewControllers
{
    [MvxRootPresentation(WrapInNavigationController = false)]
    public partial class MainTabBarController : MvxTabBarViewController<MainTabBarViewModel>
    {
        private Dictionary<Type, String> imageNameForType = new Dictionary<Type, String>
        {
            {typeof(MainViewModel), "icTime"},
            {typeof(ReportsViewModel), "icReports"}
        };

        public MainTabBarController()
        {
            setupViewControllers();
        }

        private void setupViewControllers()
        {
            var viewControllers = ViewModel.ViewModels.Select(vm => createTabFor(vm)).ToArray();
            ViewControllers = viewControllers;
        }

        private UIViewController createTabFor(IMvxViewModel viewModel)
        {
            var controller = new UINavigationController();
            var screen = this.CreateViewControllerFor(viewModel) as UIViewController;
            var item = new UITabBarItem();
            item.Title = "";
            item.Image = UIImage.FromBundle(imageNameForType[viewModel.GetType()]);
            item.ImageInsets = new UIEdgeInsets(6, 0, -6, 0);
            screen.TabBarItem = item;
            controller.PushViewController(screen, true);
            return controller;
        }
    }
}
