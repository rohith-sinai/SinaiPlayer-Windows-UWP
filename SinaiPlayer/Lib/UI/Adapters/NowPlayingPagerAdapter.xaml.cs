using SinaiPlayer.Lib.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SinaiPlayer.Lib.UI.Adapters
{
    public sealed partial class NowPlayingPagerAdapter : UserControl
    {
        private static MediaPlayerService Player => MediaPlayerService.Instance;

        // --- Artwork shrink, driven by NowPlayingPage's ExpandedView spring ---------------
        // PlayerPager hosts these inside a FlipView, which can have several instances alive
        // (current + recycled neighbors) at once. Rather than each instance running its own
        // spring, NowPlayingPage's single spring loop broadcasts one shared 0..1 progress
        // value every frame via this static event, so every visible/recycled adapter tracks
        // the exact same animation instead of drifting out of sync with each other.
        private static event Action<double> ProgressChanged;
        // ------------------------------------------------------------------------------------

        // --- Inactivity fade, driven by NowPlayingPage's inactivity timer -------------------
        // Mirrors PlayerBackgroundContainer/PlayerControls' TriggerInactivityAnimation/
        // ResetState, but broadcast the same way ProgressChanged is above, since several
        // adapter instances (current + recycled neighbors) can be alive at once.
        private static event Action FadeOutRequested;
        private static event Action FadeInRequested;
        // ------------------------------------------------------------------------------------

        // --- Expanded view (Queue/Lyrics), driven by PlayerBackgroundContainer.IsExpanded ---
        // Same broadcast reasoning as above: several adapter instances can be alive at once.
        // While expanded, the artwork/text shrink to make room, and the inactivity fade is
        // suppressed entirely (even for video) since the titles shouldn't disappear while the
        // user is actively looking at the queue or lyrics.
        private static event Action<bool> ExpandedChanged;
        private static bool _isExpanded;
        // ------------------------------------------------------------------------------------

        public NowPlayingPagerAdapter()
        {
            InitializeComponent();

            // Snap (no animation) to whatever the current state already is - this instance may
            // be a new/recycled neighbor created while ExpandedView is already open.
            //ApplyExpandedState(_isExpanded, animate: false);
            
            Loaded += NowPlayingPagerAdapter_Loaded;
            Unloaded += NowPlayingPagerAdapter_Unloaded;
        }

        private void NowPlayingPagerAdapter_Loaded(object sender, RoutedEventArgs e)
        {
            ProgressChanged += OnExpansionProgressChanged;
            FadeOutRequested += OnFadeOutRequested;
            FadeInRequested += OnFadeInRequested;
            ExpandedChanged += OnExpandedChanged;
        }

        private void NowPlayingPagerAdapter_Unloaded(object sender, RoutedEventArgs e)
        {
            ProgressChanged -= OnExpansionProgressChanged;
            FadeOutRequested -= OnFadeOutRequested;
            FadeInRequested -= OnFadeInRequested;
            ExpandedChanged -= OnExpandedChanged;
        }

        private void OnExpansionProgressChanged(double progress)
        {
            // progress: 0 = ExpandedView_Container closed (full-size artwork),
            //           1 = fully open (artwork at 50% height/width).
            //double scale = 1.0 - (progress * 0.5);
            //CurrentMediaImageView_Left_Scale.ScaleX = scale;
            //CurrentMediaImageView_Left_Scale.ScaleY = scale;
        }

        private void OnFadeOutRequested()
        {
            FadeOutStoryboard.Begin();
        }

        private void OnFadeInRequested()
        {
            // Stop wherever the fade-out currently is and ease back in from that value,
            // same as PlayerBackgroundContainer/PlayerControls' ResetState.
            FadeOutStoryboard.Stop();
            Titles_View.Visibility = Visibility.Visible;
            FadeInStoryboard.Begin();
        }

        private void FadeOutStoryboard_Completed(object sender, object e)
        {
            Titles_View.Visibility = Visibility.Collapsed;
        }

        private void OnExpandedChanged(bool isExpanded)
        {
            ApplyExpandedState(isExpanded, animate: true);
        }

        private void ApplyExpandedState(bool isExpanded, bool animate)
        {
            if (animate)
            {
                (isExpanded ? ExpandedShrinkStoryboard : ExpandedGrowStoryboard).Begin();
                return;
            }

            if (isExpanded)
            {
                CurrentMediaImageView_Left_Viewbox.Width = 72;
                CurrentMediaImageView_Left_Viewbox.Height = 72;
                CurrentMedia_Title.FontSize = 20;
                CurrentMedia_SubTitle.FontSize = 14;
            }
            else
            {
                CurrentMediaImageView_Left_Viewbox.Width = 120;
                CurrentMediaImageView_Left_Viewbox.Height = 120;
                CurrentMedia_Title.FontSize = 28;
                CurrentMedia_SubTitle.FontSize = 18;
            }
        }

        /// <summary>
        /// Called every animation frame by NowPlayingPage's ExpandedView spring loop with the
        /// current 0..1 open/close progress. Updates every live adapter instance.
        /// </summary>
        public static void SetExpansionProgress(double progress)
        {
            //ProgressChanged?.Invoke(progress);
        }

        /// <summary>
        /// Called by NowPlayingPage alongside PlayerBackground_UserControl.IsExpanded, whenever
        /// Queue/Lyrics opens or closes. Updates every live adapter instance's shrink state and,
        /// when opening, forces the titles fully visible (see TriggerInactivityAnimation).
        /// </summary>
        public static void SetIsExpanded(bool isExpanded)
        {
            _isExpanded = isExpanded;

            ExpandedChanged?.Invoke(isExpanded);

            // The titles should never be dimmed by the inactivity fade while the user is
            // actively looking at the queue or lyrics, even for video - bring them back the
            // moment ExpandedView opens instead of waiting for the next pointer move.
            if (isExpanded)
            {
                FadeInRequested?.Invoke();
            }
        }

        /// <summary>
        /// Called by NowPlayingPage's inactivity timer. The titles only fade out when the
        /// current media is a video AND ExpandedView (Queue/Lyrics) isn't open - that's the
        /// only case where they're overlaying content worth revealing, the same way the
        /// background dim and transport controls fade for video. For audio, or whenever
        /// ExpandedView is open, the titles stay put.
        /// </summary>
        public static void TriggerInactivityAnimation()
        {
            if (_isExpanded)
                return;

            var current = Player.CurrentMedia;

            if (current != null && current.Type == Enums.MediaType.Video)
            {
                FadeOutRequested?.Invoke();
            }
        }

        /// <summary>
        /// Called by NowPlayingPage whenever the user becomes active again. Safe to call
        /// unconditionally (e.g. for audio, where the titles were never faded out) since
        /// resetting an already-visible, already-Opacity-1.0 view is a no-op in practice.
        /// </summary>
        public static void ResetState()
        {
            FadeInRequested?.Invoke();
        }
    }
}