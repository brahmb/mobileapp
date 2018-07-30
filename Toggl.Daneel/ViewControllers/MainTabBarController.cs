using System;
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
        public MainTabBarController()
        {
            setupViewControllers();
        }

        private void setupViewControllers()
        {
            var viewControllers = ViewModel.ViewModelTuples.Select(tuple => createTabFor(tuple.Item1, tuple.Item2)).ToArray();
            ViewControllers = viewControllers;
        }

        private UIViewController createTabFor(IMvxViewModel viewModel, String imageName)
        {
            var controller = new UINavigationController();
            var screen = this.CreateViewControllerFor(viewModel) as UIViewController;
            var item = new UITabBarItem();
            item.Title = "";
            item.Image = UIImage.FromBundle(imageName);
            item.ImageInsets = new UIEdgeInsets(6, 0, -6, 0);
            screen.TabBarItem = item;
            controller.PushViewController(screen, true);
            return controller;
        }
    }
}
