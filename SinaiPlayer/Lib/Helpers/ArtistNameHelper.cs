using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SinaiPlayer.Lib.Helpers
{
    public static class ArtistNameHelper
    {
        /// <summary>
        /// Cleans artist names before building LastFM URLs.
        /// Mirrors the Swift implementation.
        /// </summary>
        /// <summary>
        /// Cleans an artist name before building the LastFM URL.
        /// Mirrors the behaviour of the macOS implementation.
        /// </summary>
        public static string CleanArtistName(string artist)
        {
            if (string.IsNullOrWhiteSpace(artist))
                return string.Empty;

            string value = artist.Trim();

            //--------------------------------------------------
            // Remove featured artists
            //--------------------------------------------------

            string[] separators =
            {
                " feat. ",
                " feat ",
                " featuring ",
                " ft. ",
                " ft ",
                " with ",
                " presents ",
                " pres. ",
                " pres ",
                " x ",
                " X ",
                " & ",
                ";"
            };

            foreach (string separator in separators)
            {
                int index = value.IndexOf(
                    separator,
                    StringComparison.OrdinalIgnoreCase);

                if (index > 0)
                {
                    value = value.Substring(0, index);
                }
            }

            //--------------------------------------------------
            // Remove bracketed text
            //--------------------------------------------------

            value = Regex.Replace(value, @"\([^)]*\)", "");

            value = Regex.Replace(value, @"\[[^\]]*\]", "");

            value = Regex.Replace(value, @"\{[^}]*\}", "");

            //--------------------------------------------------
            // Replace invalid URL characters
            //--------------------------------------------------

            value = value.Replace("/", " ");
            value = value.Replace("\\", " ");
            value = value.Replace(":", " ");
            value = value.Replace("|", " ");
            value = value.Replace("\"", "");
            value = value.Replace("?", "");
            value = value.Replace("*", "");
            value = value.Replace("<", "");
            value = value.Replace(">", "");

            //--------------------------------------------------
            // Replace punctuation
            //--------------------------------------------------

            value = value.Replace("_", " ");
            value = value.Replace("-", " ");
            value = value.Replace("•", " ");
            value = value.Replace("·", " ");

            //--------------------------------------------------
            // Remove duplicate spaces
            //--------------------------------------------------

            value = Regex.Replace(value, @"\s+", " ");

            value = value.Trim();

            return value;
        }

        private static readonly HashSet<string> InvalidArtistNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {"", "<unknown>", "unknown", "unknown artist", "various", "various artists", "artist", "track", "audio" };

        public static bool IsValidArtistName(string artist)
        {
            if (string.IsNullOrWhiteSpace(artist))
                return false;

            return !InvalidArtistNames.Contains(artist.Trim());
        }

        public static string NormalizeImageUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

            if (url.StartsWith("//"))
            {
                url = "https:" + url;
            }

            if (url.StartsWith("http://"))
            {
                url = "https://" + url.Substring(7);
            }

            url = url.Replace("avatar170s", "700x700");
            url = url.Replace("300x300", "700x700");
            url = url.Replace("174s", "700x700");

            return url;
        }
    }
}
