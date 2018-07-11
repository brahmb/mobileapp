using System;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MvvmCross.Commands;
using MvvmCross.Droid.Support.V7.RecyclerView;
using MvvmCross.Platforms.Android.Binding.BindingContext;
using Toggl.Foundation.MvvmCross.ViewModels;

namespace Toggl.Giskard.Views
{
    public sealed class MainRecyclerViewLogViewHolder : MvxRecyclerViewHolder
    {
        private Button continueButton;
        private bool continueClickOverloaded;

        public bool CanSync { get; set; }

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
