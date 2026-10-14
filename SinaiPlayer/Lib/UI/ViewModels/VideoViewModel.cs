using Windows.UI.Xaml.Media.Imaging;
using SinaiPlayer.Lib.Helpers;
using SinaiPlayer.Lib.Models; 
using SinaiPlayer.Lib.Thumbnails;
using System.Threading.Tasks;

namespace SinaiPlayer.Lib.UI.ViewModels
{
    public sealed partial class VideoViewModel : BaseViewModel
    {
        public MediaFile Item { get; }


        public VideoViewModel(MediaFile Video)
        {
            Item = Video;
        }

        private MediaArtwork artwork;

        private BitmapImage image;

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

        public async Task EnsureArtworkLoadedAsync()
        {
            if (isArtworkLoaded)
                return;

            isArtworkLoaded = true;

            var request = new VideoThumbnailRequest(Item.Id, Item.Path);

            var mediaArtwork = await request.LoadAsync();

            if (mediaArtwork == null)
                return;

            //Artwork = mediaArtwork;

            Image = mediaArtwork?.Image;
        }

        private bool isHovered;

        private bool isArtworkLoaded;

        public string Id => Item.Id;

        public string Path => Item.Path;

        public string Title => string.IsNullOrEmpty(Item.Title) ? LocalizationHelper.Format("UnTitled", LocalizationHelper.GetString("Video").ToLower()) : Item.Title;

        public string SubTitle => FormatHelper.FormatTimeToDisplayMillis(Item.Duration);

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