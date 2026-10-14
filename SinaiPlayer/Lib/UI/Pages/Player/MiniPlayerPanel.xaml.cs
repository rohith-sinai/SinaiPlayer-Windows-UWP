using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media; 
using Windows.UI.Xaml.Navigation;
using SinaiPlayer.Lib.Helpers;
using SinaiPlayer.Lib.Loaders;
using SinaiPlayer.Lib.Models;
using SinaiPlayer.Lib.Repos;
using SinaiPlayer.Lib.Services;
using SinaiPlayer.Lib.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SinaiPlayer.Lib.UI.Pages.Player
{
    public sealed partial class MiniPlayerPanel : UserControl
    {
        private WindowManager _windowManager;

        private MediaPlayerService Player => MediaPlayerService.Instance;

        private bool _ignoreSliderValueChange;

        private bool _isDraggingSlider;

        public MiniPlayerPanel()
        {
            InitializeComponent();
            _windowManager = new WindowManager();

            AddToPlaylistMenuFlyoutItem.Text = LocalizationHelper.GetString("AddToPlaylist");
            SaveQueueMenuFlyoutItem.Text = LocalizationHelper.GetString("SaveQueue");
            InfoMenuFlyoutItem.Text = LocalizationHelper.GetString("Info");
            PlaybackSpeedMenuFlyoutItem.Text = LocalizationHelper.GetString("Speed");
            StopMenuFlyoutItem.Text = LocalizationHelper.GetString("Stop");

            SeekBar.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(SeekBar_PointerPressed), true);

            SeekBar.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(SeekBar_PointerReleased), true);

            SeekBar.AddHandler(UIElement.PointerCaptureLostEvent, new PointerEventHandler(SeekBar_PointerCaptureLost), true);

            Loaded += MiniPlayerPanel_Loaded;
            Unloaded += MiniPlayerPanel_Unloaded;
        }

        private void MiniPlayerPanel_Loaded(object sender, RoutedEventArgs e)
        {
            Player.PropertyChanged += PlayerPropertyChanged;
            VolumeSlider.Value = PreferenceUtils.GetVolume();

            RefreshContentUI();
        }

        private void MiniPlayerPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            Player.PropertyChanged -= PlayerPropertyChanged;
        }


        private void PlayerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Player.CurrentMedia):
                    RefreshContentUI();
                    break;
                case nameof(Player.QueueChanged):
                    RefreshContentUI();
                    break;
                case nameof(Player.CurrentPosition):
                    UpdateElapsedTime();
                    break;
                case nameof(Player.TotalDuration):
                    UpdateTotalTime();
                    break;
            }
        }

        private void RefreshContentUI()
        {
            if (Player.CurrentMedia != null)
            {
                if (Player.CurrentMedia.Type == Enums.MediaType.Video)
                {
                    //VideoPlayerView.Opacity = 1.0;
                    //VideoPlayerView.IsHitTestVisible = true;
                    AttachVideoView();
                }
                else
                {
                    //VideoPlayerView.Opacity = 0.0;
                    //VideoPlayerView.IsHitTestVisible = false;
                    //VideoPlayerView.MediaPlayer = null;
                }
            }
        }

        private void AttachVideoView()
        {
            //if (VideoPlayerView.Opacity <= 0.0)
                //return;

            //try
            //{
                //var swapChainOptions = VideoPlayerView.SwapChainOptions;
                //var mediaPlayer = Player.AttachVideoSurface(swapChainOptions);
                //VideoPlayerView.MediaPlayer = mediaPlayer;
            //}
            //catch (Exception ex)
            //{
                //System.Diagnostics.Debug.WriteLine("MiniPlayerPanel AttachVideoView failed: " + ex);
            //}
        }

        private void VideoPlayerView_Loaded(object sender, RoutedEventArgs e)
        {
            //if (Player.CurrentMedia?.Type == Enums.MediaType.Video)
                //AttachVideoView();
        }

        private void VideoPlayerView_Unloaded(object sender, RoutedEventArgs e)
        {
            //try
            //{
                //VideoPlayerView.MediaPlayer = null;
            //}
            //catch
            //{
            //}
        }

        private void ContentButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationUtils.Navigate_NowPlayingPage();
        }

        private void Grid_DragOver(object sender, DragEventArgs e)
        {
            // Inform the system we accept File/Storage Items
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "Drop here to play";
            e.DragUIOverride.IsCaptionVisible = true;
        }

        private async void Grid_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0)
                {
                    var queue = new List<MediaFile>();

                    foreach (var item in items)
                    {
                        if (item is StorageFile file)
                        {
                            var media = await MediaLoader.GenerateMediaFileFromStorageFile(file);

                            if (media is null)
                                continue;

                            queue.Add(media);
                        }
                    }

                    await Player.SetQueueAsync(queue);
                }
            }
        }

        private void PlayPause_Button_Click(object sender, RoutedEventArgs e)
        {
            Player.TogglePlayPause();
        }

        private async void Next_Button_Click(object sender, RoutedEventArgs e)
        {
            await Player.NextAsync();
        }

        private async void Previous_Button_Click(object sender, RoutedEventArgs e)
        {
            await Player.PreviousAsync();
        }

        private void Shuffle_Button_Click(object sender, RoutedEventArgs e)
        {
            Player.ToggleShuffle();
        }

        private void Repeat_Button_Click(object sender, RoutedEventArgs e)
        {
            Player.CycleRepeatMode();
        }

        private void ToggleMute_Button_Click(object sender, RoutedEventArgs e)
        {
            Player.ToggleMute();
        }

        private void ToggleFullScreen_Button_Click(object sender, RoutedEventArgs e)
        {
            NavigationUtils.Navigate_NowPlayingPage();
            _windowManager.EnterFullScreen();
        }

        private void AddToPlaylist_MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            var item = Player.CurrentMedia;

            if (item != null)
            {
                DialogUtils.Show_AddMediaFilesToPlaylistDialog(new List<MediaFile> { item });
            }
        }

        private void SaveQueue_MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            var queue = Player.Queue;

            if (queue.Count != 0)
            {
                DialogUtils.Show_NewPlaylistDialog(queue.ToList());
            }
        }

        private void Info_MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            var media = Player.CurrentMedia;

            if (media != null)
            {
                if (media.Type is Enums.MediaType.Video)
                {
                    DialogUtils.Show_VideoInfoDialog(media);
                }
                else if (media.Type is Enums.MediaType.Audio)
                {
                    DialogUtils.Show_SongInfoDialog(media);
                }
                else
                {
                    return;
                }
            }
        }

        private void Stop_MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            Player.ClearQueue();
        }

        // Playback Speeds
        private void PlaybackSpeed0Point25xFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            Player.SetPlaybackRate(0.25);
        }

        private void PlaybackSpeed0Point5xFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            Player.SetPlaybackRate(0.5);
        }

        private void PlaybackSpeed1PointOxFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            Player.SetPlaybackRate(1.0);
        }

        private void PlaybackSpeed1Point25xFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            Player.SetPlaybackRate(1.25);
        }

        private void PlaybackSpeed1Point5xFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            Player.SetPlaybackRate(1.5);
        }

        private void PlaybackSpeed2PointOxFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            Player.SetPlaybackRate(2.0);
        }

        private void ArtworkContainerButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationUtils.Navigate_NowPlayingPage();
        }

        private void Volume_Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            // Access the new value directly from event arguments
            double newValue = e.NewValue;
            double oldValue = e.OldValue;

            Player.SetVolume(newValue);
        }


        private void UpdateTotalTime()
        {
            var duration = Player.TotalDuration;

            double max = Math.Max(0, duration.TotalSeconds);

            _ignoreSliderValueChange = true;

            SeekBar.Minimum = 0;
            SeekBar.Maximum = max;

            // Don't allow the current value to exceed the new duration.
            if (SeekBar.Value > max)
                SeekBar.Value = max;

            _ignoreSliderValueChange = false;

            TotalTime.Text = FormatHelper.FormatTimeToDisplayMillis(duration);
        }

        private void UpdateElapsedTime()
        {
            // NEVER overwrite the slider while the user is dragging.
            if (_isDraggingSlider)
                return;

            var position = Player.CurrentPosition;

            double seconds = Math.Max(0, position.TotalSeconds);

            if (seconds > SeekBar.Maximum)
                seconds = SeekBar.Maximum;

            _ignoreSliderValueChange = true;
            SeekBar.Value = seconds;
            _ignoreSliderValueChange = false;

            ElapsedTime.Text =
                FormatHelper.FormatTimeToDisplayMillis(position);
        }

        private void SeekBar_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _isDraggingSlider = true;

            // Prevent the playback-position updater from overwriting
            // the value while the user is dragging.
            _ignoreSliderValueChange = false;
        }

        private void SeekBar_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            CommitSeek();
        }

        private void SeekBar_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            if (_isDraggingSlider)
                CommitSeek();
        }

        private void CommitSeek()
        {
            if (!_isDraggingSlider)
                return;

            _isDraggingSlider = false;

            double value = SeekBar.Value;

            // Make absolutely sure the value is valid.
            value = Math.Clamp(
                value,
                SeekBar.Minimum,
                SeekBar.Maximum);

            // Update the slider one final time with the clamped value.
            _ignoreSliderValueChange = true;
            SeekBar.Value = value;
            _ignoreSliderValueChange = false;

            // Perform ONE final seek.
            Player.Seek(TimeSpan.FromSeconds(value));

            // Immediately update the displayed elapsed time.
            ElapsedTime.Text =
                FormatHelper.FormatTimeToDisplayMillis(
                    TimeSpan.FromSeconds(value));
        }

        private void SeekBar_ValueChanged(
            object sender,
            RangeBaseValueChangedEventArgs e)
        {
            if (_ignoreSliderValueChange)
                return;

            // Always update the displayed time when the slider moves.
            // This makes the UI respond immediately to dragging.
            var position = TimeSpan.FromSeconds(
                Math.Max(0, e.NewValue));

            ElapsedTime.Text =
                FormatHelper.FormatTimeToDisplayMillis(position);
        }
    }
}
