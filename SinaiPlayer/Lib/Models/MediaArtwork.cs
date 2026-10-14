using Windows.UI.Xaml.Media.Imaging;
using Windows.UI; 

namespace SinaiPlayer.Lib.Models
{
    public sealed partial class MediaArtwork
    {
        public BitmapImage Image { get; set; }

        public byte[] ImageBytes { get; set; }

        public Color Palette { get; set; }
    }
}
