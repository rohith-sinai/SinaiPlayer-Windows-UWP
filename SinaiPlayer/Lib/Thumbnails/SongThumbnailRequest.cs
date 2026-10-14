
using SinaiPlayer.Lib.Models;
using SinaiPlayer.Lib.Utils;
using System; 
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI; 
using SinaiPlayer.Lib.Helpers;
using System.Diagnostics;

namespace SinaiPlayer.Lib.Thumbnails
{
    class SongThumbnailRequest
    {
        private readonly string id;
        private readonly string path;

        public SongThumbnailRequest(string AlbumId, string FilePath)
        {
            this.id = AlbumId;
            this.path = FilePath;
        }

        public async Task<MediaArtwork> LoadAsync()
        {
            if (id == null || path == null)
                return null;

            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(path))
                return null;

            // 1) Try disk/memory cache first
            MediaArtwork cached = await MediaCacheManager.Instance.LoadAlbumArtworkAsync(id);

            if (cached != null)
                return cached;

            try
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(path);

                StorageItemThumbnail thumbnail = await file.GetThumbnailAsync(
                        ThumbnailMode.MusicView, 500);

                if (thumbnail == null)
                    return null;

                // Reject placeholder icons from Windows/Groove
                if (thumbnail.Type == ThumbnailType.Icon)
                    return null;

                // Read bytes ONCE
                byte[] bytes = await ThumbnailUtils.ReadBytesAsync(thumbnail);

                if (bytes == null || bytes.Length == 0)
                    return null;

                // Build bitmap from bytes
                var bitmap = await ThumbnailUtils.BitmapFromBytesAsync(bytes);

                // Generate palette from same bytes
                Color palette = await ColorUtils.GeneratePaletteAsync(bytes);

                MediaArtwork artwork =
                    new MediaArtwork
                    {
                        Image = bitmap,
                        ImageBytes = bytes,
                        Palette = palette
                    };

                await MediaCacheManager.Instance.SaveAlbumArtworkAsync(id, artwork);

                return artwork;
            }
            catch (Exception e)
            {
                Debug.WriteLine("SongGlideRequest.LoadAsync failed: " + e);
                return null;
            }
        }
    }
}
