using System.Reactive.Linq;
using CoreGraphics;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac.Extensions;
using UIKit;

namespace Toggl.Daneel.ViewControllers
{
    [ModalDialogPresentation]
    public sealed partial class NoWorkspaceViewController
        : ReactiveViewController<NoWorkspaceViewModel>
    {
        private const float cardHeight = 368;

        public NoWorkspaceViewController() : base(nameof(NoWorkspaceViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var screenWidth = UIScreen.MainScreen.Bounds.Width;
            PreferredContentSize = new CGSize
            {
                // ScreenWidth - 32 for 16pt margins on both sides
                Width = screenWidth > 320 ? screenWidth - 32 : 312,
                Height = cardHeight
            };

            prepareViews();

            this.Bind(CreateWorkspaceButton.Tapped(), ViewModel.CreateWorkspaceWithDefaultName);
            this.Bind(TryAgainButton.Tapped(), ViewModel.TryAgain);

            this.Bind(ViewModel.IsLoading.Select(CommonFunctions.Invert), CreateWorkspaceButton.BindEnabled());
            this.Bind(ViewModel.IsLoading.Select(CommonFunctions.Invert), TryAgainButton.BindIsVisibleWithFade());
            this.Bind(ViewModel.IsLoading.StartWith(false), ActivityIndicatorView.BindIsVisibleWithFade());
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            ActivityIndicatorView.StartAnimation();
        }

        private void prepareViews()
        {
            ActivityIndicatorView.TintColor = Color.NoWorkspace.ActivityIndicator.ToNativeColor();
            ActivityIndicatorView.Image = ActivityIndicatorView.Image.ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
            CreateWorkspaceButton.SetTitleColor(Color.NoWorkspace.DisabledCreateWorkspaceButton.ToNativeColor(), UIControlState.Disabled);
        }
    }
}
