using System;
using Foundation;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Commands;
using MvvmCross.Platforms.Ios.Binding;
using MvvmCross.Platforms.Ios.Binding.Views;
using MvvmCross.Plugin.Color.Platforms.Ios;
using MvvmCross.Plugin.Visibility;
using Toggl.Daneel.Combiners;
using Toggl.Foundation.MvvmCross.Converters;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels.ReportsCalendar;
using UIKit;

namespace Toggl.Daneel.Views
{
    public sealed partial class ReportsCalendarViewCell : MvxCollectionViewCell
    {
        private const int cornerRadius = 16;

        public static readonly NSString Key = new NSString(nameof(ReportsCalendarViewCell));
        public static readonly UINib Nib;

        static ReportsCalendarViewCell()
        {
            Nib = UINib.FromName(nameof(ReportsCalendarViewCell), NSBundle.MainBundle);
        }

        public IMvxCommand<ReportsCalendarDayViewModel> CellTappedCommand { get; set; }

        public ReportsCalendarViewCell(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            prepareViews();

            this.DelayBind(() =>
            {
                var backgroundColorConverter
                    = new BoolToConstantValueConverter<UIColor>(
                        Color.ReportsCalendar.SelectedDayBackgoundColor.ToNativeColor(),
                        Color.Common.Transparent.ToNativeColor());

                var todayVisibilityConverter = new MvxVisibilityValueConverter();

                var bindingSet = this.CreateBindingSet<ReportsCalendarViewCell, ReportsCalendarDayViewModel>();

                //Text
                bindingSet.Bind(Text).To(vm => vm.Day);

                //Color
                bindingSet.Bind(Text)
                          .For(v => v.TextColor)
                          .ByCombining(new ReportsCalendarCellTextColorValueCombiner(),
                                       v => v.IsInCurrentMonth,
                                       v => v.Selected);

                bindingSet.Bind(BackgroundView)
                          .For(v => v.BackgroundColor)
                          .To(vm => vm.Selected)
                          .WithConversion(backgroundColorConverter);

                //Rounding
                bindingSet.Bind(BackgroundView)
                          .For(v => v.RoundLeft)
                          .To(vm => vm.IsStartOfSelectedPeriod);

                bindingSet.Bind(BackgroundView)
                          .For(v => v.RoundRight)
                          .To(vm => vm.IsEndOfSelectedPeriod);

                //Today
                bindingSet.Bind(TodayBackgroundView)
                    .For(v => v.BindVisibility())
                    .To(vm => vm.IsToday)
                    .WithConversion(todayVisibilityConverter);

                bindingSet.Apply();

            });

            AddGestureRecognizer(new UITapGestureRecognizer(
                () => CellTappedCommand?.Execute((ReportsCalendarDayViewModel)DataContext)));
        }

        private void prepareViews()
        {
            //Background view
            BackgroundView.CornerRadius = cornerRadius;

            //Today background indicator
            TodayBackgroundView.CornerRadius = cornerRadius;
            TodayBackgroundView.RoundLeft = true;
            TodayBackgroundView.RoundRight = true;
            TodayBackgroundView.BackgroundColor = Color.ReportsCalendar.Today.ToNativeColor();
        }

    }
}
