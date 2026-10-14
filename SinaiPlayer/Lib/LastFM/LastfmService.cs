using CommunityToolkit.WinUI.Helpers;
using SinaiPlayer.Lib.Helpers;
using SinaiPlayer.Lib.Models;
using SinaiPlayer.Lib.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;

namespace SinaiPlayer.Lib.LastFM
{
    public sealed class LastFmService
    {
        public static LastFmService Instance { get; } = new LastFmService();

        private LastFmService() { }

        private const string OrgUrl = "https://www.last.fm";

        private static readonly HttpClient _httpClient = new HttpClient();

        // Scopes to the <ul class="image-list">...</ul> gallery block - the
        // C# equivalent of SwiftSoup's ".image-list img" selector. Non-greedy
        // + Singleline so "." matches across the many lines of markup inside
        // the list without also swallowing a second, unrelated <ul>.
        private static readonly Regex GalleryListRegex = new Regex(
            "<ul[^>]*\\bclass\\s*=\\s*[\"'][^\"']*\\bimage-list\\b[^\"']*[\"'][^>]*>(.*?)</ul>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // Matches any <img ... src="..."> WITHIN that scoped block. The
        // "/i/u/" check below still applies on top of this, same as the Swift
        // version's selector + filter combination.
        //
        // No RegexOptions.Compiled here on purpose: it needs Reflection.Emit,
        // which .NET Native (how UWP/Windows Phone release builds compile) does
        // not support - it would throw PlatformNotSupportedException at runtime
        // on a phone even though it compiles and runs fine on desktop debug.
        private static readonly Regex ImgSrcRegex = new Regex(
            "<img[^>]+src\\s*=\\s*[\"']([^\"']+)[\"']",
            RegexOptions.IgnoreCase);

        public async Task<List<MediaArtwork>> FetchArtistMediaArtworksAsync(string artistId, string artistName)
        {
            if (!ArtistNameHelper.IsValidArtistName(artistName))
                return new List<MediaArtwork>();

            // MARK: - CACHE
            // Checks memory first, then disk (Artists/{artistId}/Images) - see
            // MediaCacheManager.LoadArtistSlideshowArtworksAsync.
            var cached = await MediaCacheManager.Instance.LoadArtistSlideshowArtworksAsync(artistId);

            if (cached.Count > 0)
                return cached;

            if (!NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
                return new List<MediaArtwork>();

            // MARK: - CLEAN ARTIST NAME
            string encodedArtist = CleanAndEncodeArtistName(artistName);

            if (string.IsNullOrEmpty(encodedArtist))
                return new List<MediaArtwork>();

            string urlString = $"{OrgUrl}/music/{encodedArtist}/+images";

            // MARK: - REQUEST
            string html;
            try
            {
                html = await GetHtmlAsync(urlString, TimeSpan.FromSeconds(15));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("LastFM Error: " + ex);
                return new List<MediaArtwork>();
            }

            if (string.IsNullOrEmpty(html))
                return new List<MediaArtwork>();

            // MARK: - ONLY GALLERY IMAGES
            List<string> orderedUrls = ExtractImageUrls(html);

            Debug.WriteLine("Found images: " + orderedUrls.Count);

            if (orderedUrls.Count == 0)
                return new List<MediaArtwork>();

            // MARK: - DOWNLOAD IN ORDER (in parallel, order preserved by index)
            var downloadTasks = orderedUrls
                .Select((url, index) => DownloadAndCacheArtworkAsync(artistId, index, url))
                .ToArray();

            var downloaded = await Task.WhenAll(downloadTasks);

            var ordered = downloaded
                .Where(kv => kv.Value != null)
                .OrderBy(kv => kv.Key)
                .Select(kv => kv.Value)
                .ToList();

            MediaCacheManager.Instance.CacheArtistSlideshowArtworks(artistId, ordered);

            return ordered;
        }

        // last.fm artist URLs replace spaces with a literal "+" between words
        // (e.g. "The+Beatles"); everything else needs normal percent-encoding.
        // Encoding word-by-word then joining with a literal "+" keeps that "+"
        // from being escaped to "%2B" the way encoding the whole string at once would.
        private static string CleanAndEncodeArtistName(string artistName)
        {
            if (string.IsNullOrWhiteSpace(artistName))
                return string.Empty;

            var name = ArtistNameHelper.CleanArtistName(artistName);

            var tokens = name
                .Trim()
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.EscapeDataString);

            return string.Join("+", tokens);
        }

        private static async Task<string> GetHtmlAsync(string url, TimeSpan timeout)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.UserAgent.ParseAdd("Mozilla/5.0");

                using (var cts = new CancellationTokenSource(timeout))
                using (var response = await _httpClient.SendAsync(request, cts.Token))
                {
                    Debug.WriteLine("Status: " + (int)response.StatusCode);

                    if (!response.IsSuccessStatusCode)
                        return null;

                    return await response.Content.ReadAsStringAsync();
                }
            }
        }

        private static List<string> ExtractImageUrls(string html)
        {
            var orderedUrls = new List<string>();

            // Only look inside the gallery list - a page can have plenty of
            // other <img> tags (avatars, icons, related-artist thumbnails)
            // that happen to also pass the "/i/u/" filter below.
            Match galleryMatch = GalleryListRegex.Match(html);

            if (!galleryMatch.Success)
            {
                Debug.WriteLine("LastFM: no <ul class=\"image-list\"> found on page");
                return orderedUrls;
            }

            string galleryHtml = galleryMatch.Groups[1].Value;

            foreach (Match match in ImgSrcRegex.Matches(galleryHtml))
            {
                string src = match.Groups[1].Value;

                if (string.IsNullOrEmpty(src) || !src.Contains("/i/u/"))
                    continue;

                string highQuality = ArtistNameHelper.NormalizeImageUrl(src);

                // PRESERVE ORDER, AVOID DUPLICATES
                if (!orderedUrls.Contains(highQuality))
                    orderedUrls.Add(highQuality);
            }

            // LIMIT
            return orderedUrls.Take(12).ToList();
        }

        private async Task<KeyValuePair<int, MediaArtwork>> DownloadAndCacheArtworkAsync(
            string artistId, int index, string imageUrl)
        {
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, imageUrl))
                {
                    request.Headers.UserAgent.ParseAdd("Mozilla/5.0");

                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
                    using (var response = await _httpClient.SendAsync(request, cts.Token))
                    {
                        if (!response.IsSuccessStatusCode)
                            return new KeyValuePair<int, MediaArtwork>(index, null);

                        byte[] bytes = await response.Content.ReadAsByteArrayAsync();

                        var stream = new InMemoryRandomAccessStream();
                        IBuffer buffer = CryptographicBuffer.CreateFromByteArray(bytes);
                        await stream.WriteAsync(buffer);
                        stream.Seek(0);

                        var bitmap = await MediaCacheManager.Instance.BitmapFromStreamAsync(stream);

                        var palette = await ColorUtils.GeneratePaletteAsync(bytes);

                        var artwork = new MediaArtwork
                        {
                            Image = bitmap,
                            ImageBytes = bytes,
                            Palette = palette,
                        };

                        // CACHE - written as each image finishes, same as the Swift
                        // version, rather than waiting for the whole batch.
                        await MediaCacheManager.Instance.SaveArtistSlideshowArtworkAsync(artistId, index, artwork);

                        return new KeyValuePair<int, MediaArtwork>(index, artwork);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Image download failed: " + ex);
                return new KeyValuePair<int, MediaArtwork>(index, null);
            }
        }
    }
}
