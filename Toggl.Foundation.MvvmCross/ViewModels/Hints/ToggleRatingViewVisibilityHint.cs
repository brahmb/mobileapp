using MvvmCross.ViewModels;

namespace Toggl.Foundation.MvvmCross.ViewModels.Hints
{
    public sealed class ToggleRatingViewVisibilityHint : MvxPresentationHint
    {
        public bool ShouldHide { get; }

        public ToggleRatingViewVisibilityHint(bool shouldHide) : base()
        {
            ShouldHide = shouldHide;
        }
    }
}
