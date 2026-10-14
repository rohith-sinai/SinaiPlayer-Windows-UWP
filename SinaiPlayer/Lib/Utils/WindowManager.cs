using System;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace SinaiPlayer.Lib.Utils
{
    public class WindowManager : IDisposable
    {
        private readonly ApplicationView _currentView;

        // Custom event to notify listeners of full-screen changes
        public event EventHandler<bool> FullScreenChanged;

        public WindowManager()
        {
            _currentView = ApplicationView.GetForCurrentView();

            // Listen to the native window size changes to track full-screen state
            Window.Current.SizeChanged += OnWindowSizeChanged;
        }

        /// <summary>
        /// Checks if the application is currently in full-screen mode.
        /// </summary>
        public bool IsFullScreen => _currentView.IsFullScreenMode;

        /// <summary>
        /// Attempts to enter full-screen mode.
        /// </summary>
        /// <returns>True if the app successfully entered full-screen mode; otherwise, false.</returns>
        public bool EnterFullScreen()
        {
            if (!_currentView.IsFullScreenMode)
            {
                return _currentView.TryEnterFullScreenMode();
            }
            return true;
        }

        /// <summary>
        /// Exits full-screen mode.
        /// </summary>
        public void ExitFullScreen()
        {
            if (_currentView.IsFullScreenMode)
            {
                _currentView.ExitFullScreenMode();
            }
        }

        /// <summary>
        /// Toggles between full-screen and windowed mode.
        /// </summary>
        /// <returns>The new full-screen state (true if full screen, false if windowed).</returns>
        public bool ToggleFullScreen()
        {
            if (_currentView.IsFullScreenMode)
            {
                ExitFullScreen();
                return false;
            }
            else
            {
                return EnterFullScreen();
            }
        }

        private void OnWindowSizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            // Safely invoke the event handler with the updated state
            FullScreenChanged?.Invoke(this, _currentView.IsFullScreenMode);
        }

        public void Dispose()
        {
            // Clean up event subscription to prevent memory leaks
            Window.Current.SizeChanged -= OnWindowSizeChanged;
        }
    }
}
