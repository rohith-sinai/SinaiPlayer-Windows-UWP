using Windows.UI.Xaml.Media.Imaging;
using SinaiPlayer.Lib.Thumbnails;
using SinaiPlayer.Lib.Models; 
using System.Linq; 
using System.Threading.Tasks;
using Windows.UI;
using SinaiPlayer.Lib.Helpers;

namespace SinaiPlayer.Lib.UI.ViewModels
{
    public sealed partial class AlbumViewModel : BaseViewModel
    {
        public Album Item { get; }

        public AlbumViewModel(Album Album)
        {
            Item = Album;
        }

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

            MediaFile firstSong = Item.Songs.FirstOrDefault();

            if (firstSong == null)
                return;

            var request = new SongThumbnailRequest(firstSong.AlbumId, firstSong.Path);

            var mediaArtwork = await request.LoadAsync();

            if (mediaArtwork == null)
                return;

            //Artwork = mediaArtwork;

            Image = mediaArtwork.Image;

            if (mediaArtwork.Image != null)
                PaletteColor = mediaArtwork.Palette;
        }

        private bool isArtworkLoaded;

        private bool isHovered;

        public string Id => Item.Id;

        public string Title => string.IsNullOrEmpty(Item.Name) ? LocalizationHelper.GetString("UnknownAlbum") : Item.Name;

        public string Artist => string.IsNullOrEmpty(Item.Artist) ? LocalizationHelper.GetString("UnknownArtist") : Item.Artist;

        public string Year => Item.Year.ToString() == "0" ? "1990" : Item.Year.ToString();

        public string ArtistId => Item.ArtistId;

        public string SubTitle => $"{Artist} • {Year}";

        public double OverlayOpacity => isHovered ? 0.5 : 0;

        public bool IsHovered
        {
            get => isHovered;

            set
            {
                isHovered = value;

                Notify();

                Notify(nameof(OverlayOpacity));
            }
        }
    }
}
