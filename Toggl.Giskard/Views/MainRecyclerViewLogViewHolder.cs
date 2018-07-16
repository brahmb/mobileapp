using System;
using System.Linq;
using Android.Animation;
using Android.App;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MvvmCross.Commands;
using MvvmCross.Droid.Support.V7.RecyclerView;
using MvvmCross.Platforms.Android.Binding.BindingContext;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.Extensions;

namespace Toggl.Giskard.Views
{
    public enum AnimationSide
    {
        Left,
        Right
    }

    public sealed class MainRecyclerViewLogViewHolder : MvxRecyclerViewHolder
    {
        private static readonly int animationDuration = 1000;

        private Button continueButton;
        private bool continueClickOverloaded;
        private ObjectAnimator animator;

        public bool CanSync { get; set; }

        public bool IsAnimating => animator?.IsRunning ?? false;

        public View ContinueBackground { get; private set; }
        public View DeleteBackground { get; private set; }
        public View ContentView { get; private set; }

        private IMvxAsyncCommand<TimeEntryViewModel> continueCommand;
        public IMvxAsyncCommand<TimeEntryViewModel> ContinueCommand
        {
            get => continueCommand;
            set
            {
                if (continueCommand == value) return;

                continueCommand = value;
                if (continueCommand == null) return;

                ensureContinueClickOverloaded();
            }
        }

        public MainRecyclerViewLogViewHolder(View itemView, IMvxAndroidBindingContext context)
            : base(itemView, context)
        {
            ContinueBackground = itemView.FindViewById<View>(Resource.Id.MainLogBackgroundContinue);
            DeleteBackground = itemView.FindViewById<View>(Resource.Id.MainLogBackgroundDelete);
            ContentView = itemView.FindViewById<View>(Resource.Id.MainLogContentView);
        }

        public MainRecyclerViewLogViewHolder(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        public void StartAnimating(AnimationSide side)
        {
            if (animator != null && animator.IsRunning)
                return;

            ContinueBackground.Visibility = side == AnimationSide.Right ? ViewStates.Visible : ViewStates.Invisible;
            DeleteBackground.Visibility = side == AnimationSide.Left ? ViewStates.Visible : ViewStates.Invisible;

            var offsetsInDp = getAnimationOffsetsForSide(side);
            var offsetsInPx = offsetsInDp.Select(offset => (float)offset.DpToPixels(Application.Context)).ToArray();

            animator = ObjectAnimator.OfFloat(ContentView, "translationX", offsetsInPx);
            animator.SetDuration(animationDuration);
            animator.RepeatMode = ValueAnimatorRepeatMode.Reverse;
            animator.RepeatCount = ValueAnimator.Infinite;
            animator.Start();
        }

        public void StopAnimating()
        {
            if (animator != null)
            {
                animator.Cancel();
                animator = null;
            }

            ContentView.TranslationX = 0;
            ContinueBackground.Visibility = ViewStates.Invisible;
            DeleteBackground.Visibility = ViewStates.Invisible;
        }

        private float[] getAnimationOffsetsForSide(AnimationSide side)
        {
            switch (side)
            {
                case AnimationSide.Right:
                    return new float[] { 50, 0, 3.5f, 0 };
                case AnimationSide.Left:
                    return new float[] { -50, 0, -3.5f, 0 };
                default:
                    throw new ArgumentException("Unexpected side");
            }
        }

        private void ensureContinueClickOverloaded()
        {
            if (continueClickOverloaded) return;
            continueButton = ItemView.FindViewById<Button>(Resource.Id.TimeEntriesLogCellContinueButton);
            continueButton.Click += onContinueButtonClick;
            continueClickOverloaded = true;
        }

        private void onContinueButtonClick(object sender, EventArgs e)
        {
            ExecuteCommandOnItem(ContinueCommand);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            if (continueButton == null) return;
            continueButton.Click -= onContinueButtonClick;
        }
    }
}
