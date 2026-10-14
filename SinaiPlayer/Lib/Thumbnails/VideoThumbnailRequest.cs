
using SinaiPlayer.Lib.Helpers;
using SinaiPlayer.Lib.Models;
using SinaiPlayer.Lib.Utils;
using System; 
using System.Diagnostics; 
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI;

namespace SinaiPlayer.Lib.Thumbnails
{
    class VideoThumbnailRequest
    {
        private readonly string id;
        private readonly string path;

        public VideoThumbnailRequest(string Id, string FilePath)
        {
            this.id = Id;
            this.path = FilePath;
        }

        public async Task<MediaArtwork> LoadAsync()
        {
            if (id == null || path == null)
                return null;

            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(path))
                return null;

            // 1) Try cache first
            MediaArtwork cached = await MediaCacheManager.Instance.LoadVideoArtworkAsync(id);

            if (cached != null)
                return cached;

            try
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(path);

                StorageItemThumbnail thumbnail = await file.GetThumbnailAsync(
                        ThumbnailMode.VideosView, 700);

                if (thumbnail == null)
                    return null;

                // reject default Windows placeholder icon
                if (thumbnail.Type == ThumbnailType.Icon)
                    return null;

                byte[] bytes = await ThumbnailUtils.ReadBytesAsync(thumbnail);

                if (bytes == null || bytes.Length == 0)
                    return null;


                var bitmap = await ThumbnailUtils.BitmapFromBytesAsync(bytes);

                Color palette = await ColorUtils.GeneratePaletteAsync(bytes);

                MediaArtwork artwork =
                    new MediaArtwork
                    {
                        Image = bitmap,
                        ImageBytes = bytes,
                        Palette = palette
                    };

                await MediaCacheManager.Instance
                    .SaveVideoArtworkAsync(id, artwork);

                return artwork;
            }
            catch (Exception e)
            {
                Debug.WriteLine("VideoGlideRequest.LoadAsync failed: " + e);
                return null;
            }
        }
    }
}
