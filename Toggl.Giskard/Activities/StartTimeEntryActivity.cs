using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using Android.App;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Support.V4.Content;
using Android.Views;
using Android.Widget;
using MvvmCross.Droid.Support.V7.AppCompat;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using Toggl.Foundation.Autocomplete;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.Onboarding.StartTimeEntryView;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.Helper;
using Toggl.Giskard.Views;
using Toggl.Multivac.Extensions;
using static Toggl.Foundation.MvvmCross.Parameters.SelectTimeParameters.Origin;

namespace Toggl.Giskard.Activities
{
    [MvxActivityPresentation]
    [Activity(Theme = "@style/AppTheme",
              ScreenOrientation = ScreenOrientation.Portrait,
              WindowSoftInputMode = SoftInput.StateVisible,
              ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public sealed partial class StartTimeEntryActivity : MvxAppCompatActivity<StartTimeEntryViewModel>, IReactiveBindingHolder
    {
        private static readonly TimeSpan typingThrottleDuration = TimeSpan.FromMilliseconds(300);

        private PopupWindow onboardingPopupWindow;
        private IDisposable onboardingDisposable;

        public CompositeDisposable DisposeBag { get; } = new CompositeDisposable();

        protected override void OnCreate(Bundle bundle)
        {
            this.ChangeStatusBarColor(new Color(ContextCompat.GetColor(this, Resource.Color.blueStatusBarBackground)));

            base.OnCreate(bundle);
            SetContentView(Resource.Layout.StartTimeEntryActivity);
            OverridePendingTransition(Resource.Animation.abc_slide_in_bottom, Resource.Animation.abc_fade_out);

            initializeViews();

            this.Bind(ViewModel.TextFieldInfoObservable, onTextFieldInfo);
            this.Bind(durationLabel.Tapped(), _ => ViewModel.SelectTimeCommand.Execute(Duration));

            editText.TextObservable
                .SubscribeOn(ThreadPoolScheduler.Instance)
                .Throttle(typingThrottleDuration)
                .Select(text => text.AsImmutableSpans(editText.SelectionStart))
                .Subscribe(async spans => await ViewModel.OnTextFieldInfoFromView(spans))
                .DisposedBy(DisposeBag);
        }

        protected override void OnResume()
        {
            base.OnResume();
            editText.RequestFocus();
            selectProjectToolbarButton.LayoutChange += onSelectProjectToolbarButtonLayoutChanged;
        }

        private void onSelectProjectToolbarButtonLayoutChanged(object sender, View.LayoutChangeEventArgs changeEventArgs)
        {
            selectProjectToolbarButton.Post(setupStartTimeEntryOnboardingStep);
        }

        protected override void OnStop()
        {
            base.OnStop();
            selectProjectToolbarButton.LayoutChange -= onSelectProjectToolbarButtonLayoutChanged;
            onboardingPopupWindow?.Dismiss();
        }

        public override void Finish()
        {
            base.Finish();
            OverridePendingTransition(Resource.Animation.abc_fade_in, Resource.Animation.abc_slide_out_bottom);
        }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            if (keyCode == Keycode.Back)
            {
                ViewModel.BackCommand.ExecuteAsync();
                return true;
            }

            return base.OnKeyDown(keyCode, e);
        }

        private void setupStartTimeEntryOnboardingStep()
        {
            clearPreviousOnboardingSetup();

            onboardingPopupWindow = PopupWindowFactory.PopupWindowWithText(
                this,
                Resource.Layout.TooltipWithCenteredBottomArrow,
                Resource.Id.TooltipText,
                Resource.String.OnboardingAddProjectOrTag);

            var storage = ViewModel.OnboardingStorage;

            onboardingDisposable = new AddProjectOrTagOnboardingStep(storage, ViewModel.DataSource)
                .ManageDismissableTooltip(
                    onboardingPopupWindow,
                    selectProjectToolbarButton,
                    (popup, anchor) => popup.TopHorizontallyCenteredOffsetsTo(anchor, 8),
                    storage);
        }

        private void clearPreviousOnboardingSetup()
        {
            onboardingDisposable?.Dispose();
            onboardingDisposable = null;
            onboardingPopupWindow?.Dismiss();
            onboardingPopupWindow = null;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            DisposeBag?.Dispose();
            onboardingDisposable?.Dispose();
        }

        private void onTextFieldInfo(TextFieldInfo textFieldInfo)
        {
            var (formattedText, cursorPosition) = textFieldInfo.AsSpannableTextAndCursorPosition();

            editText.TextFormatted = formattedText;
            editText.SetSelection(cursorPosition);
        }
    }
}
