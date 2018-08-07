using System;
using Foundation;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Ios.Binding.Views;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;
using MvvmCross.Platforms.Ios.Binding;
using MvvmCross.Plugin.Visibility;

namespace Toggl.Daneel.Views.CountrySelection
{
    public sealed partial class CountryViewCell : MvxTableViewCell
    {
        public static readonly NSString Key = new NSString(nameof(CountryViewCell));
        public static readonly UINib Nib;

        static CountryViewCell()
        {
            Nib = UINib.FromName(nameof(CountryViewCell), NSBundle.MainBundle);
        }

        public CountryViewCell(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            this.DelayBind(() =>
            {
                var visibilityConverter = new MvxVisibilityValueConverter();

                var bindingSet = this.CreateBindingSet<CountryViewCell, SelectableCountryViewModel>();

                bindingSet.Bind(NameLabel).To(vm => vm.Country.Name);

                bindingSet.Bind(CheckBoxImageView)
                          .For(v => v.BindVisibility())
                          .To(vm => vm.Selected)
                          .WithConversion(visibilityConverter);

                bindingSet.Apply();
            });
        }
    }
}
