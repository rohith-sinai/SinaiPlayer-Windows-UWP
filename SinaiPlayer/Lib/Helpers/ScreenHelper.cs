using System;
using Windows.UI.Xaml;

namespace SinaiPlayer.Lib.Helpers
{
    class ScreenHelper
    {

        public static event EventHandler ScreenSizeChanged;

        static ScreenHelper()
        {
            Window.Current.SizeChanged += (s, e) =>
            {
                ScreenSizeChanged?.Invoke(null, EventArgs.Empty);
            };
        }

        private const double MediumSize = 600;
        private const double LargeSize = 1000;

        public static double FullWidth => Window.Current.Bounds.Width;
        public static double FullHeight => Window.Current.Bounds.Height;

        public static bool IsMobile => FullWidth < MediumSize;

        public static bool IsTablet =>
            FullWidth >= MediumSize &&
            FullWidth < LargeSize;

        public static bool IsDesktop => FullWidth >= LargeSize;
    }
}
