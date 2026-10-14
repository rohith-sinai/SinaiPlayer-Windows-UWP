using CommunityToolkit.WinUI.Helpers;
using SinaiPlayer.Lib.Deezer;
using SinaiPlayer.Lib.Helpers;
using SinaiPlayer.Lib.Models;
using SinaiPlayer.Lib.Services;
using SinaiPlayer.Lib.Utils;
using System; 
using System.Net.Http; 
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using Windows.UI;

namespace SinaiPlayer.Lib.Thumbnails
{
    class ArtistThumbnailRequest
    {
        public string Name { get; }
        public string Id { get; }

        public MediaArtwork Artwork { get; private set; }

        public ArtistThumbnailRequest(string id, string name)
        {
            Name = name;
            Id = id;
        }

        public async Task<MediaArtwork> LoadAsync()
        {
            if(PreferenceUtils.GetIsEnabledFetchMetadata())
            {
                var cached = await MediaCacheManager.Instance.LoadArtistArtworkAsync(Id);

                if (cached != null)
                {
                    Artwork = cached;

                    return cached;
                }

                string imageUrl = await DeezerService.Instance.FetchArtistImageAsync(Name);

                if (string.IsNullOrWhiteSpace(imageUrl))
                {
                    return null;
                }

                HttpClient client = new HttpClient();

                byte[] bytes = await client.GetByteArrayAsync(imageUrl);

                InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream();

                IBuffer buffer = CryptographicBuffer.CreateFromByteArray(bytes);

                await stream.WriteAsync(buffer);

                stream.Seek(0);

                var bitmap = await MediaCacheManager.Instance.BitmapFromStreamAsync(stream);

                Color palette = await ColorUtils.GeneratePaletteAsync(bytes);

                Artwork = new MediaArtwork { Image = bitmap, Palette = palette };

                await MediaCacheManager.Instance.SaveArtistArtworkAsync(Id,Artwork);

                return Artwork;
            }
            else
            {
                return null;
            }
            
        }
    }
}
