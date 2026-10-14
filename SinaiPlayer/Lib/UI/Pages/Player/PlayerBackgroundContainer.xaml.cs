using SinaiPlayer.Lib.Helpers;
using SinaiPlayer.Lib.LastFM;
using SinaiPlayer.Lib.Models;
using SinaiPlayer.Lib.Services;
using SinaiPlayer.Lib.UI.ViewModels;
using SinaiPlayer.Lib.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SinaiPlayer.Lib.UI.Pages.Player
{
    public sealed partial class PlayerBackgroundContainer : UserControl
    {
        private static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(
            nameof(IsExpanded), typeof(bool), typeof(PlayerBackgroundContainer),
            new PropertyMetadata(false, OnIsExpandedChanged));

        public bool IsExpanded
        {
            get => (bool)GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        private MediaPlayerService Player => MediaPlayerService.Instance;
        private PlayerViewModel VM => PlayerViewModel.Instance;

        public PlayerBackgroundContainer()
        {
            InitializeComponent();

            Loaded += NowPlayingPage_Loaded;
            Unloaded += NowPlayingPage_UnLoaded;

            RefreshUI();
        }

        //////////////////////////////
        // INITIALIZERS
        /////////////////////////////

        private void NowPlayingPage_Loaded(object sender, RoutedEventArgs e)
        {
            VM.PropertyChanged += VM_PropertyChanged;
        }

        private void NowPlayingPage_UnLoaded(object sender, RoutedEventArgs args)
        {
            VM.PropertyChanged -= VM_PropertyChanged;
        }

        private async void VM_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VM.CurrentItem) ||
                e.PropertyName == nameof(VM.CurrentIndex))
            {
                RefreshUI();
            }
        }


        private async void RefreshUI()
        {
            var Current = VM.CurrentItem;

            if (Current == null)
                return;

            await Current.EnsureArtworkLoadedAsync();

            if (Current.IsVideo)
                LoadVideo();
            else
            {
                LoadSongArtwork();

                if (PreferenceUtils.GetIsEnabledNowPlayingSlideshow())
                {
                    await EnsureArtistArtworksLoadedAsync();
                }
            }
        }

        private void LoadSongArtwork()
        {
            VideoPlayerView.Opacity = 0.0;
            VideoPlayerView.IsHitTestVisible = false;
            VideoPlayerView.MediaPlayer = null;
            Overlay.Opacity = 0.3;

            var Current = VM.CurrentItem;

            if (Current == null)
                return;

            BackgroundArtworkImage.Visibility = Visibility.Visible;

            var brush = new AcrylicBrush();
            brush.TintColor = Current.Palette;
            brush.TintOpacity = 0.3;
            brush.FallbackColor = Colors.Black;

            BackgroundArtworkPalette.Fill = brush;


            BackgroundArtworkImage.Source = Current.Image;
            BackgroundArtworkPalette.Visibility = Visibility.Visible;
        }

        private void LoadVideo()
        {
            var Current = VM.CurrentItem;

            if (Current == null)
                return;

            SlideshowContainer.Visibility = Visibility.Collapsed;
            BackgroundArtworkImage.Visibility = Visibility.Collapsed;
            BackgroundArtworkPalette.Visibility = Visibility.Collapsed;
            VideoPlayerView.Opacity = 1.0;
            VideoPlayerView.IsHitTestVisible = true;
            AttachVideoView();
            Overlay.Opacity = 0;
            VideoPlayerOverlayRectangle.Fill = new SolidColorBrush { Color = Current.Palette };
        }

        private void AttachVideoView()
        {
            if (VideoPlayerView.Opacity <= 0.0)
                return;

            try
            {
                var swapChainOptions = VideoPlayerView.SwapChainOptions;
                var mediaPlayer = Player.AttachVideoSurface(swapChainOptions);
                VideoPlayerView.MediaPlayer = mediaPlayer;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("PlayerBackgroundContainer AttachVideoView failed: " + ex);
            }
        }

        private void VideoPlayerView_Loaded(object sender, RoutedEventArgs e)
        {
            if (VM.CurrentItem?.IsVideo == true)
                AttachVideoView();
        }

        private void VideoPlayerView_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Detach only this XAML surface. Do NOT stop LibVLC playback.
                VideoPlayerView.MediaPlayer = null;
            }
            catch
            {
            }

            // A future NowPlayingPage instance will have a different UWP swap chain,
            // so the service must rebuild LibVLC for that new surface while preserving
            // the current playback position.
            try
            {
                Player.ReleaseVideoSurface();
            }
            catch
            {
            }
        }

        public ObservableCollection<MediaArtwork> ArtistArtworks = new ObservableCollection<MediaArtwork>();
        public async Task EnsureArtistArtworksLoadedAsync()
        {

            SlideshowContainer.Visibility = Visibility.Collapsed;
            ArtistArtworks.Clear();

            var Current = VM.CurrentItem;

            if (Current == null)
                return;

            if (Current.IsVideo)
                return;

            try
            {
                var results = await LastFmService.Instance.FetchArtistMediaArtworksAsync(Current.Item.ArtistId, Current.Item.Artist);
                results.Shuffle();
                ArtistArtworks = new ObservableCollection<MediaArtwork>(results);

                SlideshowContainer.Items = ArtistArtworks;
            }
            finally
            {
                SlideshowContainer.Visibility = ArtistArtworks.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public void TriggerInactivityAnimation()
        {
            FadeOutStoryboard.Begin();
        }

        public void ResetState()
        {
            // Stop wherever the fade-out currently is and ease back in from that value
            // rather than jumping straight to 0.4 opacity.
            FadeOutStoryboard.Stop();
            FadingContainer.Visibility = Visibility.Visible;
            FadeInStoryboard.Begin();
        }

        private void FadeOutStoryboard_Completed(object sender, object e)
        {
            FadingContainer.Visibility = Visibility.Collapsed;
        }

        private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (PlayerBackgroundContainer)d;

            bool isExpanded = (bool)e.NewValue;

            control.SlideshowContainer.IsExpanded = isExpanded;

            control.AnimateExpandedOverlay(isExpanded);
        }

        private void AnimateExpandedOverlay(bool isExpanded)
        {
            var Current = VM.CurrentItem;
            if (Current.IsVideo)
            {
                var anim1 = new DoubleAnimation
                {
                    From = VideoPlayerOverlayRectangle.Opacity,
                    To = isExpanded ? 0.8 : 0.0,
                    Duration = new Duration(TimeSpan.FromSeconds(0.1)),
                    EnableDependentAnimation = true,
                };

                Storyboard.SetTarget(anim1, VideoPlayerOverlayRectangle);
                Storyboard.SetTargetProperty(anim1, nameof(Opacity));

                var anim2 = new DoubleAnimation
                {
                    From = VideoPlayerExpandedAcrylicRectangle.Opacity,
                    To = isExpanded ? 1.0 : 0.0,
                    Duration = new Duration(TimeSpan.FromSeconds(0.1)),
                    EnableDependentAnimation = true,
                };

                Storyboard.SetTarget(anim2, VideoPlayerExpandedAcrylicRectangle);
                Storyboard.SetTargetProperty(anim2, nameof(Opacity));

                var storyboard1 = new Storyboard();
                storyboard1.Children.Add(anim1);
                storyboard1.Children.Add(anim2);

                storyboard1.Begin();
            }

            double from = ExpandedOverlay.Opacity;
            double to = isExpanded ? 0.6 : 0.0;

            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = new Duration(TimeSpan.FromSeconds(0.1)),
                EnableDependentAnimation = true,
            };

            Storyboard.SetTarget(animation, ExpandedOverlay);
            Storyboard.SetTargetProperty(animation, nameof(Opacity));

            var storyboard = new Storyboard();
            storyboard.Children.Add(animation);

            storyboard.Begin();

        }
    }
}
