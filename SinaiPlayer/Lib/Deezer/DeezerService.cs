using CommunityToolkit.WinUI.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SinaiPlayer.Lib.Deezer
{
    public sealed class DeezerService
    {
        public static DeezerService Instance { get; } = new DeezerService();

        private readonly HttpClient client = new HttpClient();

        public async Task<string> FetchArtistImageAsync(string artistName)
        {
            try
            {
                if(NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
                {
                    string url =
                    "https://api.deezer.com/search/artist?q="
                    + Uri.EscapeDataString(artistName);

                    string json = await client.GetStringAsync(url);

                    JObject obj = JObject.Parse(json);

                    JToken artist = obj["data"]?.First;

                    return artist?["picture_xl"]?.ToString();
                }
                else
                {
                    return null;
                }
                
            }
            catch
            {
                return null;
            }
        }
    }
}
