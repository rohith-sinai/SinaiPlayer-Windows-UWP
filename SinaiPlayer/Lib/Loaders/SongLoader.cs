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
    public sealed partial class SongLoader
    {

        public static async Task<List<MediaFile>> LoadSongsAsync(IEnumerable<MediaFile> mediaFiles)
        {
            var songs = new List<MediaFile>();

            foreach (var media in mediaFiles)
            {
                if (media.Type != MediaType.Audio)
                    continue;

                try
                {
                    var file = await StorageFile.GetFileFromPathAsync(media.Path);

                    var props = await file.Properties.GetMusicPropertiesAsync();

                    string title =
                        string.IsNullOrWhiteSpace(props.Title)
                            ? ""
                            : props.Title;

                    string artist =
                        string.IsNullOrWhiteSpace(props.Artist)
                            ? ""
                            : props.Artist;

                    string artistId = Sanitize(artist.ToLowerInvariant());

                    string albumArtist =
                       string.IsNullOrWhiteSpace(props.AlbumArtist)
                           ? artist
                           : props.AlbumArtist;


                    string albumArtistId = Sanitize(albumArtist.ToLowerInvariant());

                    string album =
                        string.IsNullOrWhiteSpace(props.Album)
                            ? ""
                            : props.Album;

                    string albumId = Sanitize($"{album}_{albumArtist}".ToLowerInvariant());

                    songs.Add(
                        new MediaFile
                        {
                            Id = media.Id,
                            Path = media.Path,
                            FileName = media.FileName,
                            TrackNumber = props.TrackNumber,
                            Title = title,
                            ArtistId = artistId,
                            Artist = artist,
                            AlbumArtistId = albumArtistId,
                            AlbumArtist = albumArtist,
                            AlbumId = albumId,
                            Album = album,
                            Year = props.Year,
                            Composers = props.Composers,
                            Genres = props.Genre,
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

            return songs
                .OrderBy(x => x.Title)
                .ToList();
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
