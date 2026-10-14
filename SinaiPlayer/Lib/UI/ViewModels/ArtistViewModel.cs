using SinaiPlayer.Lib.Helpers;
using SinaiPlayer.Lib.Models; 
using SinaiPlayer.Lib.Thumbnails;
using System.Linq; 
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Media.Imaging;

namespace SinaiPlayer.Lib.UI.ViewModels
{
    public sealed partial class ArtistViewModel : BaseViewModel
    {
        public Artist Item { get; }

        private bool isHovered;

        public ArtistViewModel(Artist Artist)
        {
            Item = Artist;
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

        public Color PaletteColor
        {
            get => paletteColor;

            private set
            {
                paletteColor = value;
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

        public async Task EnsureArtworkLoadedAsync()
        {
            if (isArtworkLoaded)
                return;

            isArtworkLoaded = true;


            if (Title == LocalizationHelper.GetString("UnknownArtist"))
                return;

            var request = new ArtistThumbnailRequest(Item.Id, Item.Name);

            var mediaArtwork = await request.LoadAsync();

            if (mediaArtwork == null)
                return;

            //Artwork = mediaArtwork;

            Image = mediaArtwork.Image;

            if (mediaArtwork.Image != null)
                PaletteColor = mediaArtwork.Palette;
        }

        private bool isArtworkLoaded;

        public string Id => Item.Id;

        public string Title => string.IsNullOrEmpty(Item.Name) ? LocalizationHelper.GetString("UnknownArtist") : Item.Name;

        public string SubTitle => string.IsNullOrWhiteSpace(Item.Songs.FirstOrDefault().Genres.FirstOrDefault()) ? LocalizationHelper.GetString("Unknown") : Item.Songs.FirstOrDefault().Genres.FirstOrDefault();

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
