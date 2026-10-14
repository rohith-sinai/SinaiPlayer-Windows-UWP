using SinaiPlayer.Lib.Helpers;
using SinaiPlayer.Lib.Models;
using SinaiPlayer.Lib.Services;
using SinaiPlayer.Lib.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SinaiPlayer.Lib.UI.Pages.Player
{
    /// <summary>
    /// Which panel (if any) NowPlayingPage's ExpandedView_Container should be
    /// showing. Queue and Lyrics share that one container, so only one can be
    /// open at a time.
    /// </summary>
    public enum ExpandedViewMode
    {
        None,
        Queue,
        Lyrics
    }

    public sealed partial class PlayerControls : UserControl
    {

        private MediaPlayerService Player => MediaPlayerService.Instance;

        private WindowManager _windowManager; 

        private DateTime _lastLiveSeekUtc = DateTime.MinValue;
        private static readonly TimeSpan LiveSeekThrottle = TimeSpan.FromMilliseconds(80);
         
        private DispatcherTimer _longPressDelayTimer;
        private DispatcherTimer _fastSeekRepeatTimer;
        private bool _isLongPressing;
        private bool _isPressActive;
        private int _pressDirection;
        private static readonly TimeSpan LongPressDelay = TimeSpan.FromMilliseconds(450);
        private static readonly TimeSpan FastSeekRepeatInterval = TimeSpan.FromMilliseconds(200);
        private const double FastSeekStepSeconds = 5.0;
        private bool _ignoreSliderValueChange;
        private bool _isDraggingSlider; 
        private ExpandedViewMode _expandedViewMode = ExpandedViewMode.None;
        private bool _isSyncingToggles;

        public event EventHandler<ExpandedViewMode> ExpandedViewModeChanged; 

        public PlayerControls()
        {
            InitializeComponent();
            SaveQueueMenuFlyoutItem.Text = LocalizationHelper.GetString("SaveQueue");
            InfoMenuFlyoutItem.Text = LocalizationHelper.GetString("Info");
            PlaybackSpeedMenuFlyoutItem.Text = LocalizationHelper.GetString("Speed");
            StopMenuFlyoutItem.Text = LocalizationHelper.GetString("Stop");

            _windowManager = new WindowManager();
            _windowManager.FullScreenChanged += OnFullScreenChanged;

            SeekBar.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(SeekBar_PointerPressed), true);

            SeekBar.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(SeekBar_PointerReleased), true);

            SeekBar.AddHandler(UIElement.PointerCaptureLostEvent, new PointerEventHandler(SeekBar_PointerCaptureLost), true);

            Loaded += NowPlayingPage_Loaded;
            Unloaded += NowPlayingPage_UnLoaded;

        }

        private void OnFullScreenChanged(object sender, bool isFullScreen)
        {
            ToggleFullScreenButton_Icon.Glyph = isFullScreen ? "\uE73F" : "\uE740";
        }

        //////////////////////////////
        // INITIALIZERS
        /////////////////////////////

        private void NowPlayingPage_Loaded(object sender, RoutedEventArgs e)
        {
            VolumeSlider.Value = PreferenceUtils.GetVolume();
            Player.PropertyChanged += Player_PropertyChanged;
            RefreshAllUI();
        }

        private void NowPlayingPage_UnLoaded(object sender, RoutedEventArgs args)
        {
            Player.PropertyChanged -= Player_PropertyChanged;

            _longPressDelayTimer?.Stop();
            _fastSeekRepeatTimer?.Stop();
        }

        private void Player_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Player.IsLoading):
                    UpdateLoadingUI();
                    break;
                case nameof(Player.CanGoNext):
                    UpdateNextButton();
                    break;
                case nameof(Player.CanGoPrevious):
                    UpdatePreviousButton();
                    break;
                case nameof(Player.IsPlaying):
                    UpdatePlayPauseButton();
                    break;
                case nameof(Player.CurrentPosition):
                    UpdateElapsedTime();
                    break;
                case nameof(Player.TotalDuration):
                    UpdateTotalTime();
                    break;
            }
        }


        private void RefreshAllUI()
        {
            UpdateLoadingUI();
            UpdatePlayPauseButton();
            UpdateNextButton();
            UpdatePreviousButton(); 
            UpdateTotalTime();
            UpdateElapsedTime();
        }

        //////////////////////////
        // CHANGE EVENT LISTENERS
        //////////////////////////

        private void UpdateLoadingUI()
        {
            LoadingIndicator.Visibility = Player.IsLoading ? Visibility.Visible : Visibility.Collapsed;
            SeekBar.Opacity = Player.IsLoading ? 0 : 1;
        }

        private void UpdatePlayPauseButton()
        {
            PlayPause_Icon.Glyph = Player.IsPlaying ? "\uE769" : "\uE768";
        }

        private void UpdateNextButton()
        {
            Next_Button.IsEnabled = Player.CanGoNext;
        }

        private void UpdatePreviousButton()
        {
            Previous_Button.IsEnabled = Player.CanGoPrevious;
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

            TotalTime.Text =
                FormatHelper.FormatTimeToDisplayMillis(duration);
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

        //////////////////////////////
        // ON CLICK EVENT LISTENERS
        /////////////////////////////

        private void PlayPause_Button_Click(object sender, RoutedEventArgs e)
        {
            Player.TogglePlayPause();
        }

        //////////////////////////////
        // SEEK SLIDER
        //////////////////////////////

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

            // IMPORTANT:
            // Do NOT call Player.Seek() here.
            //
            // The media player is updated once when the user releases
            // the slider. This prevents the playback engine and the
            // slider from fighting each other during dragging.
        }

        //////////////////////////////
        // NEXT / PREVIOUS (tap = skip track, hold = fast-seek)
        /////////////////////////////

        private void Next_Button_PointerPressed(object sender, PointerRoutedEventArgs e)
            => BeginPressAndHold(direction: +1);

        private void Next_Button_PointerReleased(object sender, PointerRoutedEventArgs e)
            => EndPressAndHold(onTap: () => _ = Player.NextAsync());

        private void Next_Button_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
            => EndPressAndHold(onTap: null);

        private void Previous_Button_PointerPressed(object sender, PointerRoutedEventArgs e)
            => BeginPressAndHold(direction: -1);

        private void Previous_Button_PointerReleased(object sender, PointerRoutedEventArgs e)
            => EndPressAndHold(onTap: () => _ = Player.PreviousAsync());

        private void Previous_Button_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
            => EndPressAndHold(onTap: null);

        private void BeginPressAndHold(int direction)
        {
            _isPressActive = true;
            _pressDirection = direction;
            _isLongPressing = false;

            _longPressDelayTimer?.Stop();
            _longPressDelayTimer = new DispatcherTimer { Interval = LongPressDelay };
            _longPressDelayTimer.Tick += LongPressDelayTimer_Tick;
            _longPressDelayTimer.Start();
        }

        private void LongPressDelayTimer_Tick(object sender, object e)
        {
            _longPressDelayTimer.Stop();
            _isLongPressing = true;

            // Seek once immediately so holding feels responsive, then keep repeating
            // until the button is released.
            Player.SeekRelative(_pressDirection * FastSeekStepSeconds);

            _fastSeekRepeatTimer?.Stop();
            _fastSeekRepeatTimer = new DispatcherTimer { Interval = FastSeekRepeatInterval };
            _fastSeekRepeatTimer.Tick += (_, __) => Player.SeekRelative(_pressDirection * FastSeekStepSeconds);
            _fastSeekRepeatTimer.Start();
        }

        private void EndPressAndHold(Action onTap)
        {
            if (!_isPressActive)
                return; // Already handled (PointerReleased + PointerCaptureLost can both fire).

            _isPressActive = false;
            _longPressDelayTimer?.Stop();
            _fastSeekRepeatTimer?.Stop();

            // Only treat this as a tap (skip to next/previous track) if the press never
            // crossed into long-press territory - otherwise releasing after fast-seeking
            // would also jump tracks, which isn't what anyone holding the button wants.
            bool wasTap = !_isLongPressing;
            _isLongPressing = false;

            if (wasTap)
                onTap?.Invoke();
        }

        private void ToggleMute_Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Volume_Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            // Access the new value directly from event arguments
            double newValue = e.NewValue;
            double oldValue = e.OldValue;

            Player.SetVolume(newValue);
        }
         

        private void ToggleFullScreen_Button_Click(object sender, RoutedEventArgs e)
        {
            _windowManager.ToggleFullScreen();
        }

        private void AddToPlaylist_Button_Click(object sender, RoutedEventArgs e)
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

        private void SleepTimer_MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {

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

        //////////////////////////////
        // QUEUE / LYRICS EXPANDED VIEW TOGGLE
        /////////////////////////////

        private void ToggleQueue_Button_Checked(object sender, RoutedEventArgs e)
        {
            if (_isSyncingToggles)
                return;

            // Mutually exclusive with Lyrics - both share ExpandedView_Container.
            _isSyncingToggles = true;
            ToggleLyrics_Button.IsChecked = false;
            _isSyncingToggles = false;

            SetExpandedViewMode(ExpandedViewMode.Queue);
        }

        private void ToggleQueue_Button_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isSyncingToggles)
                return;

            if (_expandedViewMode == ExpandedViewMode.Queue)
                SetExpandedViewMode(ExpandedViewMode.None);
        }

        private void ToggleLyrics_Button_Checked(object sender, RoutedEventArgs e)
        {
            if (_isSyncingToggles)
                return;

            _isSyncingToggles = true;
            ToggleQueue_Button.IsChecked = false;
            _isSyncingToggles = false;

            SetExpandedViewMode(ExpandedViewMode.Lyrics);
        }

        private void ToggleLyrics_Button_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isSyncingToggles)
                return;

            if (_expandedViewMode == ExpandedViewMode.Lyrics)
                SetExpandedViewMode(ExpandedViewMode.None);
        }

        private void SetExpandedViewMode(ExpandedViewMode mode)
        {
            if (_expandedViewMode == mode)
                return;

            _expandedViewMode = mode;
            ExpandedViewModeChanged?.Invoke(this, mode);
        }

        public void TriggerInactivityAnimation()
        {
            SlideOutStoryboard.Begin();
        }

        public void ResetState()
        {
            // Stop the slide-out wherever it currently is (TranslateY/Opacity hold their
            // last animated value), then animate smoothly back in from there instead of
            // snapping instantly — this is what makes the reveal feel as smooth as the exit.
            SlideOutStoryboard.Stop();
            SlidingContainer.Visibility = Visibility.Visible;
            SlideInStoryboard.Begin();
        }

        private void SlideOutStoryboard_Completed(object sender, object e)
        {
            SlidingContainer.Visibility = Visibility.Collapsed;
        }

        private async void Previous_Button_Click(object sender, RoutedEventArgs e)
        {
            await Player.PreviousAsync();
        }

        private async void Next_Button_Click(object sender, RoutedEventArgs e)
        {
            await Player.NextAsync();
        }
    }
}