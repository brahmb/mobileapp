using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MvvmCross.Binding.Droid.BindingContext;
using MvvmCross.Core.ViewModels;
using MvvmCross.Droid.Support.V7.RecyclerView;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac.Extensions;

namespace Toggl.Giskard.Views
{
    public sealed class MainRecyclerViewLogViewHolder : MvxRecyclerViewHolder
    {
        private static readonly TimeSpan editThrottleDuration = TimeSpan.FromMilliseconds(1000);

        private Button continueButton;
        private bool continueClickOverloaded;

        public bool CanSync { get; set; }

        private Subject<Unit> editSubject = new Subject<Unit>();
        public IMvxAsyncCommand<TimeEntryViewModel> EditCommand { get; set; }

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

        private CompositeDisposable disposeBag = new CompositeDisposable();

        public MainRecyclerViewLogViewHolder(View itemView, IMvxAndroidBindingContext context)
            : base(itemView, context)
        {
            editSubject
                .Throttle(editThrottleDuration)
                .VoidSubscribe(() => ExecuteCommandOnItem(EditCommand))
                .DisposedBy(disposeBag);

            Click = new MvxCommand(() => editSubject.OnNext(Unit.Default));
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

            if (continueButton != null)
            {
                continueButton.Click -= onContinueButtonClick;
            }
            disposeBag.Dispose();
        }
    }
}
