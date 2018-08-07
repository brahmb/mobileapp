using System;
using Foundation;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Ios.Binding.Views;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Foundation.MvvmCross.Converters;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels.Calendar.QuickSelectShortcuts;
using UIKit;

namespace Toggl.Daneel.Views.Reports
{
    public sealed partial class ReportsCalendarQuickSelectViewCell : MvxCollectionViewCell
    {
        public static readonly NSString Key = new NSString(nameof(ReportsCalendarQuickSelectViewCell));
        public static readonly UINib Nib;

        static ReportsCalendarQuickSelectViewCell()
        {
            Nib = UINib.FromName(nameof(ReportsCalendarQuickSelectViewCell), NSBundle.MainBundle);
        }

        public ReportsCalendarQuickSelectViewCell(IntPtr handle)
            : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            TitleLabel.Font = UIFont.SystemFontOfSize(13, UIFontWeight.Medium);

            this.DelayBind(() =>
            {
                var backgroundColorConverter = new BoolToConstantValueConverter<UIColor>(
                    Color.Calendar.QuickSelect.SelectedBackground.ToNativeColor(),
                    Color.Calendar.QuickSelect.UnselectedBackground.ToNativeColor()
                );
                var titleColorConverter = new BoolToConstantValueConverter<UIColor>(
                    Color.Calendar.QuickSelect.SelectedTitle.ToNativeColor(),
                    Color.Calendar.QuickSelect.UnselectedTitle.ToNativeColor()
                );

                var bindingSet = this.CreateBindingSet<ReportsCalendarQuickSelectViewCell, CalendarBaseQuickSelectShortcut>();

                //Text
                bindingSet.Bind(TitleLabel).To(vm => vm.Title);

                //Color
                bindingSet.Bind(ContentView)
                          .For(v => v.BackgroundColor)
                          .To(vm => vm.Selected)
                          .WithConversion(backgroundColorConverter);

                bindingSet.Bind(TitleLabel)
                          .For(v => v.TextColor)
                          .To(vm => vm.Selected)
                          .WithConversion(titleColorConverter);

                bindingSet.Apply();
            });
        }
    }
}
