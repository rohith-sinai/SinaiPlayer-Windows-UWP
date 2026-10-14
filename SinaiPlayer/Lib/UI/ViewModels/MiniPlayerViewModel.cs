using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;
using SinaiPlayer.Lib.Enums;
using SinaiPlayer.Lib.Helpers;
using SinaiPlayer.Lib.Models;
using SinaiPlayer.Lib.Services;
using SinaiPlayer.Lib.Thumbnails;
using System; 
using System.ComponentModel; 
using System.Threading.Tasks; 

namespace SinaiPlayer.Lib.UI.ViewModels
{
    public sealed partial class MiniPlayerViewModel : BaseViewModel
    {
        private MediaPlayerService Player => MediaPlayerService.Instance;
        private MediaFile Item => Player.CurrentMedia;

        public MiniPlayerViewModel()
        {
            ScreenHelper.ScreenSizeChanged += ScreenSizeChanged;
            Player.PropertyChanged += PlayerPropertyChanged;

            UpdatePlayPauseIcon();
            UpdatePreviousNextButtonVisibility();
            UpdateRepeatModeIcon();
            UpdateCurrentMedia();
        }

        private void ScreenSizeChanged(object sender, EventArgs e)
        {
            Notify(nameof(MiniPlayerHeight));
            Notify(nameof(GridColumnDefination2Width));
            Notify(nameof(GridColumnDefination3Width));
            Notify(nameof(VolumeButtonVisiblility));
            Notify(nameof(ToggleSmallScreenButtonVisiblility));
            Notify(nameof(ToggleFullScreenButtonVisiblility));
            Notify(nameof(MoreButtonVisiblility));
            Notify(nameof(DesktopControlsVisiblility));
            Notify(nameof(MobileControlsVisiblility));
            Notify(nameof(TitleFontSize));
            Notify(nameof(SubTitleFontSize));
            Notify(nameof(ArtworkPlacholderFontSize));
            Notify(nameof(ArtworkHeight));
            Notify(nameof(ArtworkWidth));
        }

        private void PlayerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Player.IsLoading):
                    UpdateLoading();
                    break;
                case nameof(Player.CurrentMedia):
                    UpdateCurrentMedia();
                    break;
                case nameof(Player.QueueChanged):
                    UpdateMiniPlayerVisibility();
                    break;
                case nameof(Player.IsShuffled):
                    UpdateShuffleIcon();
                    break;
                case nameof(Player.RepeatMode):
                    UpdateRepeatModeIcon();
                    break;
                case nameof(Player.CanGoNext):
                case nameof(Player.CanGoPrevious):
                    UpdatePreviousNextButtonVisibility();
                    break;
                case nameof(Player.IsPlaying):
                    UpdatePlayPauseIcon();
                    break;
                case nameof(Player.Volume):
                    UpdateVolumeIcon();
                    break;

            }

        }

        private bool isVideo => Item != null && Item.Type == Enums.MediaType.Video;
        private bool isAudio => Item != null && Item.Type == Enums.MediaType.Audio;


        private BitmapImage image = null;

        private bool isMiniPlayerVisibile = false;
        public bool IsMiniPlayerVisible
        {
            get => isMiniPlayerVisibile;
            set
            {
                isMiniPlayerVisibile = value;
                Notify();
            }
        }

        public double MiniPlayerHeight => IsMiniPlayerVisible ? 0 : ScreenHelper.FullWidth < 800 ? 64 : 80;
        public double MiniPlayerWidth => ScreenHelper.FullWidth;

        public string GridColumnDefination2Width =>
            ScreenHelper.FullWidth < 800 ? "Auto" : "*";

        public string GridColumnDefination3Width =>
            ScreenHelper.FullWidth < 800 ? "Auto" : "*";

        public string Title { get; private set; } = "";
        public double TitleFontSize => ScreenHelper.FullWidth < 800 ? 16 : 18;

        public string SubTitle { get; private set; } = "";
        public double SubTitleFontSize => ScreenHelper.FullWidth < 800 ? 12 : 13.6;

        public double SeekBarValue { get; private set; } = 0;

        public Visibility SubTitleVisibility { get; private set; } = Visibility.Collapsed;

        private double artWorkHeight =>
            ScreenHelper.FullWidth < 800 ? MiniPlayerHeight - 24 : MiniPlayerHeight - 20;

        public double ArtworkHeight { get; private set; }
        public double ArtworkWidth { get; private set; }

        public double ArtworkPlacholderFontSize =>
            ScreenHelper.FullWidth < 800 ? 16 : 20;

        public BitmapImage Image
        {
            get => image;
            set
            {
                image = value;
                Notify();
            }
        }


        private string playpauseIcon = "\uE768";
        public string PlayPauseIcon
        {
            get => playpauseIcon;
            set
            {
                playpauseIcon = value;
                Notify();
            }
        }

        private bool isNextButtonVisible = false;
        public bool IsNextButtonVisible
        {
            get => isNextButtonVisible;
            set
            {
                isNextButtonVisible = value;
                Notify();
            }
        }

        private bool isPreviousButtonVisible = false;
        public bool IsPreviousButtonVisible
        {
            get => isPreviousButtonVisible;
            set
            {
                isPreviousButtonVisible = value;
                Notify();
            }
        }

        private bool isShuffleEnabled = false;
        public bool IsShuffleEnabled
        {
            get => isShuffleEnabled;
            set
            {
                isShuffleEnabled = value;
                Notify();
            }
        }

        private double shuffleIconOpacity = 0.5;
        public double ShuffleIconOpacity
        {
            get => shuffleIconOpacity;
            set
            {
                shuffleIconOpacity = value;
                Notify();
            }
        }

        private double shuffleDisabledLineOpacity = 0.5;
        public double ShuffleDisabledLineOpacity
        {
            get => shuffleDisabledLineOpacity;
            set
            {
                shuffleDisabledLineOpacity = value;
                Notify();
            }
        }

        private string repeatIcon = "\uE8EE";
        public string RepeatIcon
        {
            get => repeatIcon;
            set
            {
                repeatIcon = value;
                Notify();
            }
        }

        private double repeatIconOpacity = 0.5;

        public double RepeatIconOpacity
        {
            get => repeatIconOpacity;
            set
            {
                repeatIconOpacity = value;
                Notify();
            }
        }

        private double repeatDisabledLineOpacity = 0.5;

        public double RepeatDisabledLineOpacity
        {
            get => repeatDisabledLineOpacity;
            set
            {
                repeatDisabledLineOpacity = value;
                Notify();
            }
        }

        private string volumeIcon = "\uE995";
        public string VolumeIcon
        {
            get => volumeIcon;
            set
            {
                volumeIcon = value;
                Notify();
            }
        }

        public Visibility VolumeHintBarVisibility { get; private set; } = Visibility.Visible;

        public double SeekBarOpacity { get; private set; } = 1;

        public Visibility ProgressBarVisibility { get; private set; } = Visibility.Collapsed;

        public Visibility MobileControlsVisiblility =>
            ScreenHelper.FullWidth < 800 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility DesktopControlsVisiblility =>
            ScreenHelper.FullWidth > 800 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility VolumeButtonVisiblility =>
             ScreenHelper.FullWidth > 800 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ToggleSmallScreenButtonVisiblility =>
             ScreenHelper.FullWidth > 800 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ToggleFullScreenButtonVisiblility =>
             ScreenHelper.FullWidth > 800 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility MoreButtonVisiblility =>
             ScreenHelper.FullWidth > 800 ? Visibility.Visible : Visibility.Collapsed;

        public async Task LoadArtworkAsync()
        {
            if (Item == null) return;


            if (isAudio)
            {

                var request = new SongThumbnailRequest(Item.AlbumId, Item.Path);

                var art = await request.LoadAsync();

                if (art == null) return;

                Image = art.Image;


            }

            if (isVideo)
            {

                var request = new VideoThumbnailRequest(Item.Id, Item.Path);

                var art = await request.LoadAsync();

                if (art == null) return;

                Image = art.Image;


            }
        }


        public void UpdateMiniPlayerVisibility()
        {
            IsMiniPlayerVisible = Player.Queue.Count != 0;
            Notify(nameof(MiniPlayerHeight));
        }


        private void UpdateLoading()
        {
            ProgressBarVisibility = Player.IsLoading ? Visibility.Visible : Visibility.Collapsed;
            SeekBarOpacity = Player.IsLoading ? 0 : 1;
            Notify(nameof(ProgressBarVisibility));
            Notify(nameof(SeekBarOpacity));
        }

        private async void UpdateCurrentMedia()
        {
            Image = null;

            if (Item != null)
            {
                UpdateArtworkSize();

                Title = Item.Title;
                
                var artist = string.IsNullOrEmpty(Item.Artist) ? LocalizationHelper.GetString("UnknownArtist") : Item.Artist;
                var album = string.IsNullOrEmpty(Item.Album) ? LocalizationHelper.GetString("UnknownArtist") : Item.Album;

                SubTitle = artist + " • " + album;
                SubTitleVisibility = Item.Type == MediaType.Audio ? Visibility.Visible : Visibility.Collapsed;

                Notify(nameof(SubTitleVisibility));
                Notify(nameof(Title));
                Notify(nameof(SubTitle));
                Notify(nameof(ArtworkHeight));
                Notify(nameof(ArtworkWidth));

                await LoadArtworkAsync();

            }

        }

        private void UpdateArtworkSize()
        {
            ArtworkHeight = artWorkHeight;
            ArtworkWidth = isVideo ? artWorkHeight * 1.7 : artWorkHeight;
        }

        private void UpdatePlayPauseIcon()
        {
            PlayPauseIcon = Player.IsPlaying ? "\uE769" : "\uE768";
            Notify(PlayPauseIcon);
        }

        private void UpdatePreviousNextButtonVisibility()
        {
            IsNextButtonVisible = Player.CanGoNext;
            IsPreviousButtonVisible = Player.CanGoPrevious;
            Notify(nameof(IsNextButtonVisible));
            Notify(nameof(IsPreviousButtonVisible));
        }

        private void UpdateRepeatModeIcon()
        {
            switch (Player.RepeatMode)
            {
                // repeat none
                case RepeatModeType.None:
                    RepeatIcon = "\uE8EE";
                    RepeatIconOpacity = 0.5;
                    RepeatDisabledLineOpacity = 0.5;
                    break;
                // repeat all
                case RepeatModeType.All:
                    RepeatIcon = "\uE8EE";
                    RepeatIconOpacity = 1;
                    RepeatDisabledLineOpacity = 0;
                    break;
                // repeat one
                case RepeatModeType.One:
                    RepeatIcon = "\uE8ED";
                    RepeatIconOpacity = 1;
                    RepeatDisabledLineOpacity = 0;
                    break;
            }
        }

        private void UpdateShuffleIcon()
        {
            if (Player.IsShuffled)
            {
                ShuffleIconOpacity = 1;
                ShuffleDisabledLineOpacity = 0;
            }
            else
            {
                ShuffleIconOpacity = 0.5;
                ShuffleDisabledLineOpacity = 0.5;
            }
        }

        private void UpdateVolumeIcon()
        {
            if (Player.Volume == 0)
                VolumeIcon = "\uE74F";
            else if (Player.Volume > 0 && Player.Volume < 30)
                VolumeIcon = "\uE993";
            else if (Player.Volume > 30 && Player.Volume < 60)
                VolumeIcon = "\uE994";
            else if (Player.Volume > 60)
                VolumeIcon = "\uE995";
        }
    }
}
