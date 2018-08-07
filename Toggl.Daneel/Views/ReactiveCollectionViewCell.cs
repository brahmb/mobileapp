using System;
using System.Reactive.Disposables;
using CoreGraphics;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;

namespace Toggl.Daneel.Views
{
    public abstract class ReactiveCollectionViewCell<TViewModel> : UICollectionViewCell, IReactiveBindingHolder
    {
        public CompositeDisposable DisposeBag { get; private set; } = new CompositeDisposable();

        public TViewModel Item { get; set; }

        protected internal ReactiveCollectionViewCell(IntPtr handle) : base(handle)
        {
        }

        public ReactiveCollectionViewCell(CGRect frame) : base(frame)
        {
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;
            DisposeBag?.Dispose();
        }
    }
}
