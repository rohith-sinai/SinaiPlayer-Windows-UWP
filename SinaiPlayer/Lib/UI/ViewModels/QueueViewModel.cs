using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;
using SinaiPlayer.Lib.Enums;
using SinaiPlayer.Lib.Helpers;
using SinaiPlayer.Lib.Models;
using SinaiPlayer.Lib.Repos;
using SinaiPlayer.Lib.Thumbnails;
using SinaiPlayer.Lib.Services;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;

namespace SinaiPlayer.Lib.UI.ViewModels
{
    public sealed partial class QueueViewModel : BaseViewModel
    {
        public MediaFile Item;

        public QueueViewModel(MediaFile MediaFile)
        {
            Item = MediaFile;
            ScreenHelper.ScreenSizeChanged += ScreenSizeChanged;
        }

        private void ScreenSizeChanged(object sender, EventArgs e)
        {
            Notify(nameof(CombinedTitleVisibility));
            Notify(nameof(TitleVisibility));
            Notify(nameof(ArtistVisibility));
            Notify(nameof(AlbumVisibility));
            Notify(nameof(YearVisibility));
            Notify(nameof(GenreVisibility));
            Notify(nameof(GridColumnDefination2Width));
            Notify(nameof(GridColumnDefination3Width));
            Notify(nameof(GridColumnDefination4Width));
            Notify(nameof(GridColumnDefination5Width));
        }

        private bool isArtworkLoaded;

        public async Task EnsureArtworkLoadedAsync()
        {
            if (isArtworkLoaded)
                return;

            isArtworkLoaded = true;

            if (Item.Type == MediaType.Audio)
            {
                var request = new SongThumbnailRequest(Item.AlbumId, Item.Path);

                var art = await request.LoadAsync();

                if (art == null)
                    return;

                Image = art.Image;
                palette = art.Palette;

                Debug.WriteLine("Palette Color  = " + art.Palette);
            }

            if (Item.Type == MediaType.Video)
            {

                var request = new VideoThumbnailRequest(Item.Id, Item.Path);

                var art = await request.LoadAsync();

                if (art == null)
                    return;

                Image = art.Image;
                Palette = art.Palette;

                if (art.Image != null)
                {
                    PreviousPalette = art.Palette;
                }
            }
        }

        private BitmapImage image;

        private Color palette;

        private Color previousPalette;

        public BitmapImage Image
        {
            get => image;

            private set
            {
                image = value;

                Notify();
            }
        }

        public Color Palette
        {
            get => palette;

            private set
            {
                palette = value;

                Notify();
            }
        }

        public Color PreviousPalette
        {
            get => palette;

            private set
            {
                palette = value;

                Notify();
            }
        }


        public string Id => Item.Id;

        public string Path => Item.Path;

        public string Title => string.IsNullOrEmpty(Item.Title) ? LocalizationHelper.Format("UnTitled", LocalizationHelper.GetString(IsVideo ? "Video" : "Song").ToLower()) : Item.Title;

        public Visibility TitleVisibility =>
            ScreenHelper.FullWidth > 600 ? Visibility.Visible : Visibility.Collapsed;

        public Visibility CombinedTitleVisibility =>
            ScreenHelper.FullWidth < 600 ? Visibility.Visible : Visibility.Collapsed;

        public string SubTitle => $"{Artist} • {Album}";

        public string Artist => string.IsNullOrEmpty(Item.Artist) ? LocalizationHelper.GetString("UnknownArtist") : Item.Artist;

        public string ArtistId => Item.ArtistId;

        public string AlbumArtist => string.IsNullOrEmpty(Item.AlbumArtist) ? LocalizationHelper.GetString("UnknownArtist") : Item.AlbumArtist;

        public string AlbumArtistId => Item.AlbumArtistId;

        public string Album => string.IsNullOrEmpty(Item.Album) ? LocalizationHelper.GetString("UnknownAlbum") : Item.Album;

        public string AlbumId => Item.AlbumId;

        public string Year => Item.Year.ToString() == "0" ? "1990" : Item.Year.ToString();

        public string Genre => string.IsNullOrWhiteSpace(Item.Genres.FirstOrDefault()) ? LocalizationHelper.GetString("Unknown") : Item.Genres.FirstOrDefault();

        public string Duration => FormatHelper.FormatTimeToDisplayMillis(Item.Duration);

        public bool IsVideo => Item != null && Item.Type == MediaType.Video;

        // Visibilities of Grid Columns

        public Visibility ArtistVisibility =>
            ScreenHelper.FullWidth > 400 ? Visibility.Visible : Visibility.Collapsed;

        public Visibility AlbumVisibility =>
            ScreenHelper.FullWidth > 600 ? Visibility.Visible : Visibility.Collapsed;

        public Visibility YearVisibility =>
            ScreenHelper.FullWidth > 1000 ? Visibility.Visible : Visibility.Collapsed;

        public Visibility GenreVisibility =>
            ScreenHelper.FullWidth > 1400 ? Visibility.Visible : Visibility.Collapsed;

        public string GridColumnDefination2Width =>
            ScreenHelper.FullWidth > 400 ? "*" : "Auto";
        public string GridColumnDefination3Width =>
            ScreenHelper.FullWidth > 600 ? "*" : "Auto";
        public string GridColumnDefination4Width =>
            ScreenHelper.FullWidth > 1000 ? "80" : "Auto";
        public string GridColumnDefination5Width =>
            ScreenHelper.FullWidth > 1400 ? "140" : "Auto";


        /// <summary>
        /// Globally reorders this queue item through the authoritative
        /// MediaPlayerService. All consumers of PlayerViewModel.Queue are then
        /// synchronized by the service's QueueChanged event.
        /// </summary>
        public static void ReorderItem(MediaFile item, int newIndex)
        {
            if (item == null)
                return;

            MediaPlayerService.Instance.MoveQueueItem(item, newIndex);
        }

        /// <summary>
        /// Global shortcut for reordering by the item's current index.
        /// </summary>
        public static void ReorderItem(int oldIndex, int newIndex)
        {
            var queue = PlayerViewModel.Instance.Queue;

            if (oldIndex < 0 || oldIndex >= queue.Count)
                return;

            if (newIndex < 0 || newIndex >= queue.Count)
                return;

            ReorderItem(queue[oldIndex].Item, newIndex);
        }

    }
}
