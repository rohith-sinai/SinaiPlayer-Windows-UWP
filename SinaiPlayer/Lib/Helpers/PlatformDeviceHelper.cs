using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System.Profile;

namespace SinaiPlayer.Lib.Helpers
{
    public static class PlatformDeviceHelper
    {
        public static bool IsPhone => AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile";

        public static bool IsDesktop => AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop";

        public static bool IsXbox => AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox";

        /// <summary>
        /// Returns the current device family.
        /// Examples:
        /// Windows.Desktop
        /// Windows.Mobile
        /// Windows.Xbox
        /// Windows.Team
        /// Windows.IoT
        /// </summary>
        public static string Family => AnalyticsInfo.VersionInfo.DeviceFamily;
    }
}
