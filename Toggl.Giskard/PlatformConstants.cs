using System;
using Android.App;
using Android.OS;
using Toggl.Foundation;

namespace Toggl.Giskard
{
    public sealed class PlatformConstants : IPlatformConstants
    {
        public string HelpUrl { get; } = "https://support.toggl.com/toggl-timer-for-android/";

        public string PhoneModel { get; } = $"{Build.Manufacturer} {Build.Model}";

        public string OperatingSystem { get; } = getOperatingSystem();

        public string BuildNumber { get; } = Application.Context
            .ApplicationContext
            .PackageManager
            .GetPackageInfo(Application.Context.ApplicationContext.PackageName, 0)
            .VersionCode
            .ToString();

        private static string getOperatingSystem()
        {
            var releaseVersion = Build.VERSION.Release;
            var platformRelease = new Build.VERSION_CODES().Class.GetFields()[(int)Build.VERSION.SdkInt].Name;
            return $"{releaseVersion} {platformRelease}";
        }
    }
}
