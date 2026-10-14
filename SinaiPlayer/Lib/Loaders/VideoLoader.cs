using SinaiPlayer.Lib.Enums;
using SinaiPlayer.Lib.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace SinaiPlayer.Lib.Loaders
{
    public sealed partial class VideoLoader
    {

        public static async Task<List<MediaFile>> LoadVideosAsync(IEnumerable<MediaFile> mediaFiles)
        {
            var videos = new List<MediaFile>();

            foreach (var media in mediaFiles)
            {
                if (media.Type != MediaType.Video)
                    continue;

                try
                {
                    StorageFile file = await StorageFile.GetFileFromPathAsync(media.Path);

                    VideoProperties props = await file.Properties.GetVideoPropertiesAsync();

                    string title =
                        string.IsNullOrWhiteSpace(props.Title)
                            ? file.DisplayName
                            : props.Title;

                    string artist =
                        string.IsNullOrWhiteSpace(props.Directors.FirstOrDefault())
                            ? ""
                            : props.Directors.FirstOrDefault();

                    string artistId = Sanitize(artist.ToLowerInvariant());

                    string albumArtist =
                       string.IsNullOrWhiteSpace(props.Directors.FirstOrDefault())
                           ? artist
                           : props.Directors.FirstOrDefault();


                    string albumArtistId = Sanitize(albumArtist.ToLowerInvariant());

                    string album =
                        string.IsNullOrWhiteSpace(props.Subtitle)
                            ? ""
                            : props.Subtitle;

                    string albumId = Sanitize($"{album}_{albumArtist}".ToLowerInvariant());
                     

                    videos.Add(
                        new MediaFile
                        {
                            Id = media.Id,
                            Path = media.Path,
                            FileName = media.FileName,
                            TrackNumber = 0,
                            Title = title,
                            ArtistId = artistId,
                            Artist = artist,
                            AlbumArtistId = albumArtistId,
                            AlbumArtist = albumArtist,
                            AlbumId = albumId,
                            Album = album,
                            Year = props.Year,
                            Composers = new List<string>(),
                            Genres = props.Keywords,
                            Duration = props.Duration,
                            Comments = "",
                            Lyrics = "",
                            DateModified = media.DateModified,
                            Extension = media.Extension,
                            Size = media.Size,
                            Type = media.Type,
                            Bitrate = props.Bitrate
                        });
                }
                catch
                {
                }
            }

            return videos.OrderBy(x => x.Title).ToList();
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "_unknown";
            }

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(c, '_');
            }

            return value.Trim();
        }
    }
}
