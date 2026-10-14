using Windows.UI.Xaml.Media.Imaging;
using Newtonsoft.Json;
using SinaiPlayer.Lib.Models;
using System;
using System.Collections.Concurrent; 
using System.Collections.Generic;
using System.Diagnostics; 
using System.Threading.Tasks; 
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI; 

namespace SinaiPlayer.Lib.Utils
{
    class MediaCacheManager
    {
        public static MediaCacheManager Instance { get; } = new MediaCacheManager();

        private MediaCacheManager()
        {
        }

        private readonly ConcurrentDictionary<string, MediaArtwork>
            memoryCache = new ConcurrentDictionary<string, MediaArtwork>();

        // Root Folder
        private StorageFolder RootFolder => ApplicationData.Current.LocalCacheFolder;

        // Media Cache Folder
        private async Task<StorageFolder> GetMediaCacheFolderAsync()
        {
            return await RootFolder
                .CreateFolderAsync("MediaCache",
                CreationCollisionOption.OpenIfExists);
        }

        // Albums Cache Folder
        private async Task<StorageFolder> GetAlbumsFolderAsync()
        {
            return await (await GetMediaCacheFolderAsync())
                .CreateFolderAsync("Albums",
                CreationCollisionOption.OpenIfExists);
        }

        // Artists Cache Folder
        private async Task<StorageFolder> GetArtistsFolderAsync()
        {
            return await (
                await GetMediaCacheFolderAsync())
                .CreateFolderAsync(
                    "Artists",
                    CreationCollisionOption.OpenIfExists);
        }

        // Videos Cache Folder
        private async Task<StorageFolder> GetVideosFolderAsync()
        {
            return await (
                await GetMediaCacheFolderAsync())
                .CreateFolderAsync(
                    "Videos",
                    CreationCollisionOption.OpenIfExists);
        }

        // Album Cache Folder
        private async Task<StorageFolder> GetAlbumFolderAsync(string albumId)
        {
            return await (
                await GetAlbumsFolderAsync())
                .CreateFolderAsync(
                    albumId,
                    CreationCollisionOption.OpenIfExists);
        }



        // Artist Cache Folder
        private async Task<StorageFolder> GetArtistFolderAsync(string artistId)
        {
            return await (
                await GetArtistsFolderAsync())
                .CreateFolderAsync(
                    artistId,
                    CreationCollisionOption.OpenIfExists);
        }


        // Artists Images Cache Folder
        private async Task<StorageFolder> GetArtistImagesFolderAsync(string artistId)
        {
            return await (await GetArtistFolderAsync(artistId))
                .CreateFolderAsync("Images", CreationCollisionOption.OpenIfExists);
        }

        // Video Cache Folder
        private async Task<StorageFolder> GetVideoFolderAsync(string videoId)
        {
            return await (
                await GetVideosFolderAsync())
                .CreateFolderAsync(
                    videoId,
                    CreationCollisionOption.OpenIfExists);
        }

        // Bitmap Loader
        public async Task<BitmapImage> BitmapFromStreamAsync(IRandomAccessStream stream)
        {
            BitmapImage image = new BitmapImage();

            stream.Seek(0);

            await image.SetSourceAsync(stream);

            return image;
        }

        // Save Artwork
        private async Task SaveArtworkAsync(StorageFolder folder, MediaArtwork artwork, string imageName)
        {
            if (folder == null || artwork == null || artwork.ImageBytes == null)
                return;

            StorageFile imageFile = await folder.CreateFileAsync(
                    imageName, CreationCollisionOption.ReplaceExisting);

            await FileIO.WriteBytesAsync(imageFile, artwork.ImageBytes);

            PaletteCacheModel model =
                new PaletteCacheModel
                {
                    A = artwork.Palette.A,
                    R = artwork.Palette.R,
                    G = artwork.Palette.G,
                    B = artwork.Palette.B
                };

            StorageFile paletteFile = await folder.CreateFileAsync(
                    "palette.json", CreationCollisionOption.ReplaceExisting);

            await FileIO.WriteTextAsync(paletteFile, JsonConvert.SerializeObject(model));
        }

        // Save Album Artwork
        public async Task SaveAlbumArtworkAsync(string albumId, MediaArtwork artwork)
        {
            memoryCache["album_" + albumId] = artwork;

            await SaveArtworkAsync(
                await GetAlbumFolderAsync(albumId),
                artwork,
                "cover.jpg");
        }

        // Save Artist Artwork
        public async Task SaveArtistArtworkAsync(string artistId, MediaArtwork artwork)
        {
            memoryCache["artist_" + artistId] = artwork;

            await SaveArtworkAsync(
                await GetArtistFolderAsync(artistId),
                artwork,
                "cover.jpg");
        }

        // Save Artist Slideshow artworks 
        private readonly ConcurrentDictionary<string, List<MediaArtwork>> slideshowMemoryCache
            = new ConcurrentDictionary<string, List<MediaArtwork>>();

        public void CacheArtistSlideshowArtworks(string artistId, List<MediaArtwork> artworks)
        {
            slideshowMemoryCache[artistId] = artworks;
        }

        public async Task SaveArtistSlideshowArtworkAsync(string artistId, int index, MediaArtwork artwork)
        {
            if (artwork == null || artwork.ImageBytes == null)
                return;

            StorageFolder folder = await GetArtistImagesFolderAsync(artistId);

            StorageFile imageFile = await folder.CreateFileAsync(
                $"{index}.jpg", CreationCollisionOption.ReplaceExisting);

            await FileIO.WriteBytesAsync(imageFile, artwork.ImageBytes);

            var model = new PaletteCacheModel
            {
                A = artwork.Palette.A,
                R = artwork.Palette.R,
                G = artwork.Palette.G,
                B = artwork.Palette.B
            };

            StorageFile paletteFile = await folder.CreateFileAsync(
                $"{index}.palette.json", CreationCollisionOption.ReplaceExisting);

            await FileIO.WriteTextAsync(paletteFile, JsonConvert.SerializeObject(model));
        }

        // Save Video Artwork
        public async Task SaveVideoArtworkAsync(string videoId, MediaArtwork artwork)
        {
            memoryCache["video_" + videoId] = artwork;

            await SaveArtworkAsync(
                await GetVideoFolderAsync(videoId),
                artwork,
                "thumbnail.jpg");
        }

        // Load Artwork
        private async Task<MediaArtwork> LoadArtworkAsync(StorageFolder folder, string imageName, string cacheKey)
        {
            if (memoryCache.TryGetValue(cacheKey, out MediaArtwork artwork))
                return artwork;

            try
            {
                StorageFile imageFile = await folder.GetFileAsync(imageName);

                StorageFile paletteFile = await folder.GetFileAsync("palette.json");

                byte[] bytes =
                    await FileIO.ReadBufferAsync(imageFile)
                        .AsTask()
                        .ContinueWith(t =>
                        {
                            var buffer = t.Result;
                            byte[] data = new byte[buffer.Length];
                            DataReader.FromBuffer(buffer).ReadBytes(data);
                            return data;
                        });

                var bitmap = await ThumbnailUtils.BitmapFromBytesAsync(bytes);

                PaletteCacheModel model =
                    JsonConvert.DeserializeObject<PaletteCacheModel>(
                        await FileIO.ReadTextAsync(paletteFile));

                artwork = new MediaArtwork
                {
                    Image = bitmap,
                    ImageBytes = bytes,
                    Palette = Color.FromArgb(
                        model.A,
                        model.R,
                        model.G,
                        model.B)
                };

                memoryCache[cacheKey] = artwork;
                return artwork;
            }
            catch (Exception e)
            {
                Debug.WriteLine("LoadArtworkAsync failed: " + e);
                return null;
            }
        }

        // Load Album Artwork
        public async Task<MediaArtwork> LoadAlbumArtworkAsync(string albumId)
        {
            return await LoadArtworkAsync(
                await GetAlbumFolderAsync(albumId),
                "cover.jpg",
                "album_" + albumId);
        }

        // Load Artist Artwork
        public async Task<MediaArtwork> LoadArtistArtworkAsync(string artistId)
        {
            return await LoadArtworkAsync(
                await GetArtistFolderAsync(artistId),
                "cover.jpg",
                "artist_" + artistId);
        }

        // Load Artist Slideshow artworks 
        public async Task<List<MediaArtwork>> LoadArtistSlideshowArtworksAsync(string artistId)
        {
            if (slideshowMemoryCache.TryGetValue(artistId, out var cached))
                return cached;

            var results = new List<MediaArtwork>();

            try
            {
                StorageFolder folder = await GetArtistImagesFolderAsync(artistId);

                IReadOnlyList<StorageFile> files = await folder.GetFilesAsync();

                var indices = new List<int>();

                foreach (StorageFile file in files)
                {
                    string name = file.Name;

                    if (!name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string stem = name.Substring(0, name.Length - ".jpg".Length);

                    int index;
                    if (int.TryParse(stem, out index))
                    {
                        indices.Add(index);
                    }
                }

                indices.Sort();

                foreach (int index in indices)
                {
                    var artwork = await LoadIndexedArtworkAsync(folder, index);
                    if (artwork != null)
                        results.Add(artwork);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("LoadArtistSlideshowArtworksAsync failed: " + e);
            }

            if (results.Count > 0)
                slideshowMemoryCache[artistId] = results;

            return results;
        }

        private async Task<MediaArtwork> LoadIndexedArtworkAsync(StorageFolder folder, int index)
        {
            try
            {
                StorageFile imageFile = await folder.GetFileAsync($"{index}.jpg");
                StorageFile paletteFile = await folder.GetFileAsync($"{index}.palette.json");

                IBuffer buffer = await FileIO.ReadBufferAsync(imageFile);

                byte[] bytes = new byte[buffer.Length];
                DataReader.FromBuffer(buffer).ReadBytes(bytes);

                var model = JsonConvert.DeserializeObject<PaletteCacheModel>(
                    await FileIO.ReadTextAsync(paletteFile));

                var stream = new InMemoryRandomAccessStream();
                await stream.WriteAsync(buffer);
                stream.Seek(0);

                var bitmap = await BitmapFromStreamAsync(stream);

                return new MediaArtwork
                {
                    Image = bitmap,
                    ImageBytes = bytes,
                    Palette = Color.FromArgb(model.A, model.R, model.G, model.B)
                };
            }
            catch (Exception e)
            {
                Debug.WriteLine($"LoadIndexedArtworkAsync failed for index {index}: " + e);
                return null;
            }
        }

        // Load Video Artwork
        public async Task<MediaArtwork> LoadVideoArtworkAsync(string videoId)
        {
            return await LoadArtworkAsync(
                await GetVideoFolderAsync(videoId),
                "thumbnail.jpg",
                "video_" + videoId);
        }

        // Clear cache
        public async Task ClearAllAsync()
        {
            try
            {
                StorageFolder mediaCache = await RootFolder.CreateFolderAsync(
                        "MediaCache", CreationCollisionOption.OpenIfExists);

                var items = await mediaCache.GetItemsAsync();

                foreach (var item in items)
                {
                    await item.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }

                memoryCache.Clear();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ClearAllAsync failed: " + ex);
            }
        }

        // Clears just this artist's slideshow (e.g. a "refresh images" action) -
        // leaves the single cover.jpg from SaveArtistArtworkAsync untouched.
        public async Task ClearArtistSlideshowAsync(string artistId)
        {
            slideshowMemoryCache.TryRemove(artistId, out _);

            try
            {
                StorageFolder folder = await GetArtistImagesFolderAsync(artistId);

                foreach (var item in await folder.GetItemsAsync())
                {
                    await item.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("ClearArtistSlideshowAsync failed: " + e);
            }
        }
    }
}
