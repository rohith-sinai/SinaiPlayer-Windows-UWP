using Windows.UI.Xaml.Media.Imaging;
using SinaiPlayer.Lib.Helpers;
using SinaiPlayer.Lib.Models;
using SinaiPlayer.Lib.Thumbnails;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace SinaiPlayer.Lib.UI.ViewModels
{
    public sealed partial class PlaylistViewModel : BaseViewModel
    {
        public Playlist Item { get; }

        private string _name;

        public PlaylistViewModel(Playlist playlist)
        {
            Item = playlist;
            _name = playlist.Name;

            Items = new ObservableCollection<PlaylistItemViewModel>(
                playlist.Items.Select(i => new PlaylistItemViewModel(i)));

            Items.CollectionChanged += (s, e) => NotifyCountChanged();
        }


        public ObservableCollection<PlaylistItemViewModel> Items { get; }

        private int SongsCount => Items.Where((i) => !i.IsVideo).ToList().Count;
        private int VideosCount => Items.Where((i) => i.IsVideo).ToList().Count;

        private string SongsCountLabel => SongsCount == 1 ? "1 " + LocalizationHelper.GetString("Song").ToLower() : $"{SongsCount} " + LocalizationHelper.GetString("Songs").ToLower();

        private string VideosCountLabel => VideosCount == 1 ? "1 " + LocalizationHelper.GetString("Video").ToLower() : $"{VideosCount} " + LocalizationHelper.GetString("Videos").ToLower();

        public string Id => Item.Id;

        public string Name => _name;

        private bool isArtworkLoaded;

        private bool isHovered;

        private MediaArtwork artwork;

        private BitmapImage image;

        private Color paletteColor = Colors.Transparent;

        public MediaArtwork Artwork
        {
            get => artwork;

            private set
            {
                artwork = value;
                Notify();
            }
        }

        public BitmapImage Image
        {
            get => image;

            private set
            {
                image = value;
                Notify();
            }
        }

        public Color PaletteColor
        {
            get => paletteColor;

            private set
            {
                paletteColor = value;
                Notify();
            }
        }

        public async Task EnsureArtworkLoadedAsync()
        {
            if (isArtworkLoaded)
                return;

            isArtworkLoaded = true;

            var firstSong = Items.ToList().FirstOrDefault();

            if (firstSong is null)
                return;

            var artwork = await LoadMediaArtwork(firstSong);

            if (artwork is null)
                return;

            Artwork = artwork;

            Image = artwork.Image;

            if (artwork.Image != null)
                PaletteColor = artwork.Palette;
        }

        private async Task<MediaArtwork> LoadMediaArtwork(PlaylistItemViewModel item)
        {
            if(item.FileType is Enums.MediaType.Video && item.FilePath != "unknown")
            {
                var request = new VideoThumbnailRequest(item.FileId, item.FilePath);

                var mediaArtwork = await request.LoadAsync();

                return mediaArtwork;
            }
            else if(item.FileType == Enums.MediaType.Audio && item.FilePath != "unknown")
            {
                var request = new SongThumbnailRequest(item.AlbumId, item.FilePath);

                var mediaArtwork = await request.LoadAsync();

                return mediaArtwork;
            }
            else
            {
                return null;
            }
        }


        public string Title => _name;

        private string GenerateSubTitle()
        {
            if(SongsCount == 0 && VideosCount == 0)
            {
                return LocalizationHelper.GetString("EmptyPlaylistSubTitle");
            }
            else if(SongsCount == 0 && VideosCount != 0)
            {
                return VideosCountLabel;
            }
            else if(SongsCount != 0 && VideosCount == 0)
            {
                return SongsCountLabel;
            }
            else
            {
                return $"{SongsCountLabel}, {VideosCountLabel}";
            }
        }

        public string SubTitle => GenerateSubTitle();


        public string DurationLabel
        {
            get
            {
                var total = TimeSpan.Zero;

                foreach (var i in Items)
                    total += i.File.Duration;

                return FormatHelper.FormatTimeToDisplayMillis(total);
            }
        }

        
        // Called by PlaylistRepository after a rename persists to the DB - kept
        // as a method rather than a settable property so renames always go
        // through the repository and stay in sync with storage.
        public void Rename(string newName)
        {
            _name = newName;
            Notify(nameof(Name));
        }

        public void NotifyCountChanged()
        { 
            Notify(nameof(SubTitle));
            Notify(nameof(DurationLabel));
        }
    }
}
