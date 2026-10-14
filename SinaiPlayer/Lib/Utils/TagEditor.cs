using SinaiPlayer.Lib.Helpers;
using SinaiPlayer.Lib.Loaders;
using SinaiPlayer.Lib.Models;
using SinaiPlayer.Lib.Repos;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace SinaiPlayer.Lib.Utils
{
    public static class TagEditor
    {
        public static async Task<AlbumTagEditResult> SaveAlbumAsync(Album album, string albumName, string albumArtist, int year, byte[] artworkBytes)
        {
            if (album == null)
                throw new ArgumentNullException(nameof(album));

            albumName = Normalize(albumName);
            albumArtist = Normalize(albumArtist);
            year = Math.Max(0, year);

            /*
             * Find every song currently belonging to this album.
             */
            var albumSongs =
                LibraryRepository.Songs
                    .Where(song =>
                        song != null &&
                        string.Equals(
                            song.Album,
                            album.Name,
                            StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(
                            song.AlbumArtist ?? song.Artist,
                            album.Artist,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();

            var result = new AlbumTagEditResult
            {
                OldAlbumId = album.Id,
                ArtworkChanged =
                    artworkBytes != null &&
                    artworkBytes.Length > 0
            };

            foreach (var song in albumSongs)
            {
                StorageFile file;

                try
                {
                    file =
                        await StorageFile.GetFileFromPathAsync(
                            song.Path);
                }
                catch
                {
                    continue;
                }

                var properties =
                    await file.Properties.GetMusicPropertiesAsync();

                /*
                 * Album editor changes album-level metadata.
                 *
                 * We intentionally do NOT modify:
                 *   Title
                 *   Track number
                 *   Genre
                 *   Song artist
                 *
                 * Those remain song-specific.
                 */
                properties.Album = albumName;
                properties.AlbumArtist = albumArtist;
                properties.Year = (uint)year;

                await properties.SavePropertiesAsync();

                /*
                 * Reload through the same metadata pipeline used
                 * everywhere else in the application.
                 */
                var refreshed =
                    (await SongLoader.LoadSongsAsync(
                        new List<MediaFile> { song }))
                    .FirstOrDefault();

                if (refreshed == null)
                    continue;

                result.UpdatedFiles.Add(refreshed);
                result.UpdatedSongs++;
            }

            /*
             * Update the library/database in one operation.
             */
            await LibraryRepository
                .ApplyAlbumTagChangesAsync(
                    albumSongs,
                    result.UpdatedFiles);

            /*
             * Album artwork belongs to the album cache, not the
             * individual song cache.
             */
            if (result.ArtworkChanged)
            {
                /*
                 * The new Album ID is calculated after metadata has
                 * been applied.
                 */
                var updatedAlbum =
                    AlbumLoader.LoadAlbums(
                        LibraryRepository.Songs.ToList())
                    .FirstOrDefault(a =>
                        string.Equals(
                            a.Name,
                            albumName,
                            StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(
                            a.Artist,
                            albumArtist,
                            StringComparison.OrdinalIgnoreCase));

                if (updatedAlbum != null)
                {
                    result.NewAlbumId = updatedAlbum.Id;

                    var artwork =
                        await CreateMediaArtworkAsync(
                            artworkBytes);

                    await MediaCacheManager.Instance
                        .SaveAlbumArtworkAsync(
                            updatedAlbum.Id,
                            artwork);
                }
            }

            return result;
        }

        /// <summary>
        /// Saves the editable tags of a single song to the physical file,
        /// reloads the song metadata, updates the database and refreshes
        /// the in-memory library and playlists.
        /// </summary>
        public static async Task<MediaFile> SaveSongAsync(
            MediaFile song,
            int trackNumber,
            string title,
            string artist,
            string album,
            string albumArtist,
            int year,
            string genre)
        {
            if (song == null)
                throw new ArgumentNullException(nameof(song));

            if (string.IsNullOrWhiteSpace(song.Path))
                throw new InvalidOperationException(
                    "The song does not contain a valid file path.");

            StorageFile file;

            try
            {
                file = await StorageFile.GetFileFromPathAsync(song.Path);
            }
            catch (FileNotFoundException)
            {
                throw new FileNotFoundException(
                    "The audio file could not be found.",
                    song.Path);
            }

            MusicProperties properties =
                await file.Properties.GetMusicPropertiesAsync();

            /*
             * Write only the editable properties exposed by the dialog.
             */
            properties.TrackNumber = (uint)Math.Max(0, trackNumber);
            properties.Title = Normalize(title);
            properties.Artist = Normalize(artist);
            properties.Album = Normalize(album);
            properties.AlbumArtist = Normalize(albumArtist);
            properties.Year = (uint)Math.Max(0, year);

            /*
             * UWP MusicProperties exposes Genre as a collection.
             */
            properties.Genre.Clear();

            string normalizedGenre = Normalize(genre);

            if (!string.IsNullOrEmpty(normalizedGenre))
            {
                foreach (string value in SplitGenres(normalizedGenre))
                {
                    properties.Genre.Add(value);
                }
            }

            await properties.SavePropertiesAsync();

            /*
             * The file's modified timestamp/size can have changed after
             * SavePropertiesAsync(), so reload the physical metadata.
             */
            MediaFile refreshedSong =
                (await SongLoader.LoadSongsAsync(
                    new List<MediaFile> { song }))
                .FirstOrDefault();

            if (refreshedSong == null)
                throw new InvalidOperationException(
                    "The song metadata could not be reloaded after saving.");

            /*
             * Update library/database/playlists.
             */
            await LibraryRepository.UpdateSongAfterTagEditAsync(
                song,
                refreshedSong);

            return refreshedSong;
        }

        private static async Task<MediaArtwork> CreateMediaArtworkAsync(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return null;

            var bitmap =
                await ThumbnailUtils.BitmapFromBytesAsync(bytes);

            /*
             * Your existing MediaArtwork also contains Palette.
             *
             * For now we preserve the existing artwork pipeline and
             * create the bitmap from the selected bytes.
             */
            return new MediaArtwork
            {
                Image = bitmap,
                ImageBytes = bytes,
                Palette = await ColorUtils.GeneratePaletteAsync(bytes)
            };
        }

        private static IEnumerable<string> SplitGenres(string value)
        {
            return value
                .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
        }
    }

    public sealed class AlbumTagEditResult
    {
        public int UpdatedSongs { get; set; }

        public List<MediaFile> UpdatedFiles { get; set; }
            = new List<MediaFile>();

        public string OldAlbumId { get; set; }

        public string NewAlbumId { get; set; }

        public bool ArtworkChanged { get; set; }
    }
}
