using System;
using System.Linq;
using MvvmCross.UI;

namespace Toggl.Foundation.MvvmCross.Helper
{
    public static class Color
    {
        private static readonly MvxColor lightishGreen = new MvxColor(76, 217, 100);
        private static readonly MvxColor steel = new MvxColor(142, 142, 147);
        private static readonly MvxColor darkMint = new MvxColor(76, 190, 100);
        private static readonly MvxColor pinkishGrey = new MvxColor(206, 206, 206);
        private static readonly MvxColor black = new MvxColor(46, 46, 46);
        private static readonly MvxColor azure = new MvxColor(6, 170, 245);
        private static readonly MvxColor macaroniAndCheese = new MvxColor(241, 195, 63);
        private static readonly MvxColor easterPurple = new MvxColor(197, 107, 255);
        private static readonly MvxColor nearlyWhite = new MvxColor(250, 251, 252);

        public static class Onboarding
        {
            internal static readonly MvxColor TrackPageBorderColor = new MvxColor(14, 150, 213);
            internal static readonly MvxColor TrackPageBackgroundColor = new MvxColor(6, 170, 245);

            internal static readonly MvxColor LogPageBorderColor = new MvxColor(165, 81, 220);
            internal static readonly MvxColor LogPageBackgroundColor = new MvxColor(187, 103, 241);

            internal static readonly MvxColor ReportsPageBorderColor = new MvxColor(230, 179, 31);
            internal static readonly MvxColor ReportsPageBackgroundColor = macaroniAndCheese;

            public static readonly MvxColor BlueColor = azure;
            public static readonly MvxColor PurpleColor = easterPurple;
            public static readonly MvxColor YellowColor = macaroniAndCheese;
            public static readonly MvxColor GrayColor = pinkishGrey;

            internal static readonly MvxColor LoginPageBackgroundColor = azure;
        }

        public static class Login
        {
            public static readonly MvxColor EnabledButtonColor = new MvxColor(255, 255, 255);
            public static readonly MvxColor DisabledButtonColor = new MvxColor(255, 255, 255, 122);
            public static readonly MvxColor TextViewPlaceholder = pinkishGrey;
            public static readonly MvxColor ForgotPassword = new MvxColor(94, 91, 91);
        }

        public static class Signup
        {
            public static readonly MvxColor HighlightedText = azure;
        }

        public static class NavigationBar
        {
            public static readonly MvxColor BackButton = new MvxColor(94, 91, 91);
            public static readonly MvxColor BackgroundColor = nearlyWhite;
        }

        public static class TabBar
        {
            public static readonly MvxColor SelectedImageTintColor = new MvxColor(6, 170, 245);
        }

        public static class Main
        {
            private static readonly MvxColor lightGrey = new MvxColor(181, 188, 192);

            public static readonly MvxColor BackgroundColor = nearlyWhite;

            public static readonly MvxColor SpiderNetColor = new MvxColor(0, 0, 0);

            public static readonly MvxColor CardBorder = new MvxColor(232, 232, 232);

            public static readonly MvxColor CurrentTimeEntryClientColor = new MvxColor(94, 91, 91);

            public static readonly MvxColor SuggestionsTitle = new MvxColor(94, 91, 91);

            public static readonly MvxColor SpiderHinge = new MvxColor(50, 50, 50);

            public static readonly MvxColor WelcomeBackText = new MvxColor(181, 188, 192);

            public static readonly MvxColor PullToRefresh = lightGrey;
            public static readonly MvxColor Syncing = lightGrey;
            public static readonly MvxColor Offline = lightGrey;
            public static readonly MvxColor SyncFailed = lightGrey;
            public static readonly MvxColor SyncCompleted = lightishGreen;
        }

        public static class TimeEntriesLog
        {
            public static readonly MvxColor ClientColor = new MvxColor(163, 163, 163);

            public static readonly MvxColor SectionFooter = new MvxColor(250, 251, 252);

            public static readonly MvxColor DeleteSwipeActionBackground = new MvxColor(247, 64, 73);

            public static readonly MvxColor ContinueSwipeActionBackground = lightishGreen;
        }

        public static class StartTimeEntry
        {
            public static readonly MvxColor Cursor = lightishGreen;

            public static readonly MvxColor Placeholder = pinkishGrey;

            public static readonly MvxColor ActiveButton = lightishGreen;

            public static readonly MvxColor BoldQuerySuggestionColor = new MvxColor(181, 188, 192);

            public static readonly MvxColor InactiveButton = new MvxColor(181, 188, 192);

            public static readonly MvxColor SeparatorColor = new MvxColor(181, 188, 192);

            public static readonly MvxColor TokenText = new MvxColor(94, 91, 91);

            public static readonly MvxColor TokenBorder = new MvxColor(232, 232, 232);

            public static readonly MvxColor AddIconColor = new MvxColor(75, 200, 0);
        }

        public static class DurationField
        {
            public static readonly MvxColor Cursor = lightishGreen;
        }

        public static class NoWorkspace
        {
            public static readonly MvxColor ActivityIndicator = lightishGreen;
            public static readonly MvxColor DisabledCreateWorkspaceButton = new MvxColor(255, 255, 255, 122);
        }

        internal static MvxColor FromHSV(float hue, float saturation, float value)
        {
            int r = 0, g = 0, b = 0;

            if (saturation == 0)
            {
                r = g = b = (int)(value * 255.0f + 0.5f);
            }
            else
            {
                float h = (hue - (float)Math.Floor(hue)) * 6.0f;
                float f = h - (float)Math.Floor(h);
                float p = value * (1.0f - saturation);
                float q = value * (1.0f - saturation * f);
                float t = value * (1.0f - (saturation * (1.0f - f)));
                switch ((int)h)
                {
                    case 0:
                        r = (int)(value * 255.0f + 0.5f);
                        g = (int)(t * 255.0f + 0.5f);
                        b = (int)(p * 255.0f + 0.5f);
                        break;
                    case 1:
                        r = (int)(q * 255.0f + 0.5f);
                        g = (int)(value * 255.0f + 0.5f);
                        b = (int)(p * 255.0f + 0.5f);
                        break;
                    case 2:
                        r = (int)(p * 255.0f + 0.5f);
                        g = (int)(value * 255.0f + 0.5f);
                        b = (int)(t * 255.0f + 0.5f);
                        break;
                    case 3:
                        r = (int)(p * 255.0f + 0.5f);
                        g = (int)(q * 255.0f + 0.5f);
                        b = (int)(value * 255.0f + 0.5f);
                        break;
                    case 4:
                        r = (int)(t * 255.0f + 0.5f);
                        g = (int)(p * 255.0f + 0.5f);
                        b = (int)(value * 255.0f + 0.5f);
                        break;
                    case 5:
                        r = (int)(value * 255.0f + 0.5f);
                        g = (int)(p * 255.0f + 0.5f);
                        b = (int)(q * 255.0f + 0.5f);
                        break;
                }
            }

            return new MvxColor(r, g, b);
        }

        internal static string ToHexString(this MvxColor color)
            => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        public static class EditTimeEntry
        {
            public static readonly MvxColor ClientText = new MvxColor(94, 91, 91);
            public static readonly MvxColor DescriptionCharacterCounter = new MvxColor(247, 64, 73);
        }

        public static class EditDuration
        {
            public static readonly MvxColor SetButton = lightishGreen;
            public static readonly MvxColor EditedTime = lightishGreen;
            public static readonly MvxColor NotEditedTime = black;

            public static class Wheel
            {
                public static readonly MvxColor Background = new MvxColor(244, 244, 244);
                public static readonly MvxColor Shadow = new MvxColor(94, 91, 91);
                public static readonly MvxColor CapBackground = new MvxColor(181, 188, 192);
                public static readonly MvxColor Cap = new MvxColor(255, 255, 255);
                public static readonly MvxColor ThickMinuteSegment = new MvxColor(188, 196, 199);
                public static readonly MvxColor ThinMinuteSegment = new MvxColor(230, 235, 237);

                public static readonly MvxColor[] Rainbow =
                {
                    MvxColor.ParseHexString("F1F2F3"),
                    MvxColor.ParseHexString("EDE4F0"),
                    MvxColor.ParseHexString("DFC3E6"),
                    MvxColor.ParseHexString("CA99D7"),
                    MvxColor.ParseHexString("B599D7"),
                    MvxColor.ParseHexString("8799E5"),
                    MvxColor.ParseHexString("5168C5"),
                    MvxColor.ParseHexString("55BBDF"),
                    MvxColor.ParseHexString("95D0E5"),
                    MvxColor.ParseHexString("B0DCEC"),
                    MvxColor.ParseHexString("BFDBD7"),
                    MvxColor.ParseHexString("7FC5BC"),
                    MvxColor.ParseHexString("E5F0BA"),
                    MvxColor.ParseHexString("F0E3BA"),
                    MvxColor.ParseHexString("F8DAB8"),
                    MvxColor.ParseHexString("F0D06C"),
                    MvxColor.ParseHexString("EFBA7A"),
                    MvxColor.ParseHexString("F1ACAE")
                };
            }
        }

        public static class ModalDialog
        {
            public static readonly MvxColor BackgroundOverlay = new MvxColor(181, 188, 192);
        }

        public static class Suggestions
        {
            public static readonly MvxColor ClientColor = new MvxColor(94, 91, 91);
            public static readonly MvxColor HeaderText = new MvxColor(94, 91, 91);
        }

        public static class Settings
        {
            public static readonly MvxColor SyncStatusText = new MvxColor(144, 146, 147);
        }

        public static class Feedback
        {
            public static readonly MvxColor Cursor = lightishGreen;
            public static readonly MvxColor ActivityIndicator = lightishGreen;
        }

        public static class Common
        {
            public static readonly MvxColor PlaceholderText = pinkishGrey;
            public static readonly MvxColor Transparent = new MvxColor(rgb: 0, alpha: 0);
            public static readonly MvxColor LightGray = pinkishGrey;
        }

        public sealed class Calendar
        {
            public sealed class QuickSelect
            {
                public static readonly MvxColor UnselectedBackground = new MvxColor(181, 188, 192, 30);
                public static readonly MvxColor SelectedBackground = new MvxColor(181, 188, 192);

                public static readonly MvxColor UnselectedTitle = new MvxColor(181, 188, 192);
                public static readonly MvxColor SelectedTitle = new MvxColor(255, 255, 255);
            }

            public static readonly MvxColor SelectedDayBackgoundColor = new MvxColor(181, 188, 192);
            public static readonly MvxColor Today = new MvxColor(181, 188, 192, 30);
            public static readonly MvxColor CellTextColorInCurrentMonth = new MvxColor(0, 0, 0);
            public static readonly MvxColor CellTextColorOutOfCurrentMonth = new MvxColor(163, 163, 163);
            public static readonly MvxColor CellTextColorSelected = new MvxColor(255, 255, 255);
        }

        public static class Reports
        {
            public static readonly MvxColor Background = new MvxColor(250, 251, 252);

            public static readonly MvxColor PercentageActivated = new MvxColor(0, 171, 252);

            public static readonly MvxColor PercentageActivatedBackground = new MvxColor(6, 170, 245, 61);

            public static readonly MvxColor PercentageDisabled = new MvxColor(181, 188, 192, 30);

            public static readonly MvxColor TotalTimeActivated = new MvxColor(76, 217, 100);

            public static readonly MvxColor DayNotInMonth = new MvxColor(149, 149, 149);

            public static readonly MvxColor Disabled = new MvxColor(181, 188, 192);

            public static readonly MvxColor OtherProjectsSegmentBackground = pinkishGrey;

            public static class Loading
            {
                public static readonly MvxColor LightColor = new MvxColor(250, 251, 252);

                public static readonly MvxColor DarkColor = new MvxColor(236, 240, 242);
            }
        }

        public static class Licenses
        {
            public static readonly MvxColor Border = new MvxColor(232, 232, 232);
        }

        public static readonly MvxColor[] DefaultProjectColors =
            Toggl.Foundation.Helper.Color.DefaultProjectColors.Select(MvxColor.ParseHexString).ToArray();
    }
}
