using MvvmCross.ViewModels;

namespace Toggl.Foundation.MvvmCross.ViewModels.Hints
{
    public sealed class ToggleRatingViewVisibilityHint : MvxPresentationHint
    {
        public bool ShouldHide { get; }

        private ToggleRatingViewVisibilityHint(bool shouldHide) : base()
        {
            ShouldHide = shouldHide;
        }

        public static ToggleRatingViewVisibilityHint Show()
        {
            var hint = new ToggleRatingViewVisibilityHint(shouldHide: false);
            return hint;
        }

        public static ToggleRatingViewVisibilityHint Hide()
        {
            var hint = new ToggleRatingViewVisibilityHint(shouldHide: true);
            return hint;
        }
    }
}
