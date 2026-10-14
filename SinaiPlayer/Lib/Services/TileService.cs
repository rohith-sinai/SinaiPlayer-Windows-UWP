using Microsoft.Toolkit.Uwp.Notifications;
using SinaiPlayer.Lib.Enums;
using SinaiPlayer.Lib.Models;
using SinaiPlayer.Lib.Thumbnails;
using SinaiPlayer.Lib.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Notifications;

namespace SinaiPlayer.Lib.Services
{
    public static class TileService
    {
        // Used to prevent an older, slower artwork request from overwriting
        // a tile that has already been updated for a newer track.
        private static int _updateVersion;

        public static async Task ShowLiveTile(MediaFile file)
        {
            if (file == null)
                return;

            int updateVersion = Interlocked.Increment(ref _updateVersion);

            try
            {
                // Use the exact same artwork pipeline as MediaPlayerService/SMTC:
                // 1. MediaCacheManager
                // 2. SongThumbnailRequest / VideoThumbnailRequest on cache miss
                //
                // Load once and reuse the resulting local tile URI for all sizes.
                var artworkUri = await PrepareTileArtworkAsync(file);

                // The user may have changed tracks while artwork was loading.
                if (updateVersion != Volatile.Read(ref _updateVersion))
                    return;

                var medium = await LoadAsync(file, artworkUri);
                var wide = await LoadAsync(file, artworkUri);
                var large = await LoadAsync(file, artworkUri);

                if (updateVersion != Volatile.Read(ref _updateVersion))
                    return;

                var content = new TileContent
                {
                    Visual = new TileVisual
                    {
                        TileMedium = new TileBinding
                        {
                            Content = medium
                        },

                        TileWide = new TileBinding
                        {
                            Content = wide
                        },

                        TileLarge = new TileBinding
                        {
                            Content = large
                        }
                    }
                };

                var notification = new TileNotification(content.GetXml());

                TileUpdateManager.CreateTileUpdaterForApplication().Update(notification);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"TileService.ShowLiveTile failed: {ex}");
            }
        }

        public static void StopLiveTile()
        {
            Interlocked.Increment(ref _updateVersion);

            TileUpdateManager
                .CreateTileUpdaterForApplication()
                .Clear();
        }

        private static async Task<TileBindingContentAdaptive> LoadAsync(MediaFile file, Uri artworkUri)
        {
            if (file == null)
                return null;

            var content = new TileBindingContentAdaptive();

            var title = new AdaptiveText
            {
                Text = string.IsNullOrWhiteSpace(file.Title)
                    ? file.FileName
                    : file.Title
            };

            content.Children.Add(title);

            if (file.Type == MediaType.Audio)
            {
                content.Children.Add(new AdaptiveText
                {
                    Text = $"{file.Artist} • {file.Album}"
                });

                content.Children.Add(new AdaptiveText
                {
                    Text = "Song"
                });
            }
            else if (file.Type == MediaType.Video)
            {
                content.Children.Add(new AdaptiveText
                {
                    Text = "Video"
                });
            }
            else
            {
                return content;
            }

            // Tile images must be addressable through a supported tile URI.
            // The MediaCacheManager uses LocalCacheFolder, while ms-appdata:///local/
            // addresses LocalFolder. Therefore we copy the artwork bytes to a small
            // LocalFolder tile-artwork cache after using the same artwork pipeline.
            if (artworkUri != null)
            {
                content.PeekImage =
                    new TilePeekImage
                    {
                        Source = artworkUri.ToString(),
                        AlternateText = file.Title
                    };
            }

            return content;
        }

        /// <summary>
        /// Loads artwork exactly like the SMTC pipeline:
        /// cache first, then the normal thumbnail request on a cache miss.
        ///
        /// The resulting bytes are copied to LocalFolder because UWP tile image
        /// sources support ms-appdata:///local/ but not LocalCacheFolder directly.
        /// </summary>
        private static async Task<Uri> PrepareTileArtworkAsync(MediaFile file)
        {
            if (file == null)
                return null;

            MediaArtwork artwork = null;
            string cacheKey;

            if (file.Type == MediaType.Audio)
            {
                cacheKey = !string.IsNullOrWhiteSpace(file.AlbumId)
                    ? file.AlbumId
                    : file.Id;

                if (!string.IsNullOrWhiteSpace(cacheKey))
                {
                    artwork = await MediaCacheManager.Instance.LoadAlbumArtworkAsync(cacheKey);

                    if (artwork == null)
                    {
                        // Same fallback used by the SMTC implementation.
                        var request = new SongThumbnailRequest(cacheKey, file.Path);

                        artwork = await request.LoadAsync();
                    }
                }
            }
            else if (file.Type == MediaType.Video)
            {
                cacheKey = file.Id;

                if (!string.IsNullOrWhiteSpace(cacheKey))
                {
                    artwork = await MediaCacheManager.Instance.LoadVideoArtworkAsync(cacheKey);

                    if (artwork == null)
                    {
                        // Same fallback used by the SMTC implementation.
                        var request = new VideoThumbnailRequest(cacheKey, file.Path);

                        artwork = await request.LoadAsync();
                    }
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }

            if (artwork?.ImageBytes == null ||
                artwork.ImageBytes.Length == 0)
            {
                return null;
            }

            return await SaveTileArtworkAsync(file, cacheKey, artwork.ImageBytes);
        }

        /// <summary>
        /// Copies the artwork into LocalFolder so Windows tile notifications can
        /// reference it with ms-appdata:///local/.
        /// </summary>
        private static async Task<Uri> SaveTileArtworkAsync(MediaFile file, string cacheKey, byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return null;

            if (string.IsNullOrWhiteSpace(cacheKey))
                cacheKey = file?.Path;

            if (string.IsNullOrWhiteSpace(cacheKey))
                return null;

            var root = ApplicationData.Current.LocalFolder;

            var tileFolder = await root.CreateFolderAsync("TileArtwork", CreationCollisionOption.OpenIfExists);

            // Escape the logical cache key so it becomes a safe URI/file name.
            var safeName = Uri.EscapeDataString(cacheKey);

            // Keep the extension stable because the artwork bytes are JPEGs in
            // MediaCacheManager's cover/thumbnail storage pipeline.
            var fileName = safeName + ".jpg";

            var imageFile = await tileFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

            await FileIO.WriteBytesAsync(imageFile, imageBytes);

            return new Uri("ms-appdata:///local/TileArtwork/" + safeName + ".jpg");
        }
    }
}
