using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SinaiPlayer.Lib.Enums;
using SinaiPlayer.Lib.Models;
using SinaiPlayer.Lib.Utils;
using Windows.Networking.Sockets;
using Windows.Storage;

namespace SinaiPlayer.Lib.Loaders
{
    public sealed partial class MediaLoader
    {
         

        // Fast, metadata-free walk: no GetMusicPropertiesAsync/GetVideoPropertiesAsync here.
        // This is what runs on every sync - it just needs to be cheap enough to run often.
        public static async Task<List<MediaFile>> LoadMediaFilesAsync(StorageFolder folder)
        {
            List<MediaFile> result = new List<MediaFile>();

            await ScanFolder(folder, result);

            return result;
        }


        private static async Task ScanFolder(StorageFolder folder, List<MediaFile> result)
        {
            try
            {
                var files = await folder.GetFilesAsync();

                foreach (var file in files)
                {
                    var mf = await GenerateMediaFileFromStorageFile(file);

                    if (mf is null)
                        continue;

                    result.Add(mf);
                }

                var folders = await folder.GetFoldersAsync();

                foreach (var subFolder in folders)
                {
                    await ScanFolder(subFolder, result);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        public static async Task<MediaFile> GenerateMediaFileFromStorageFile(StorageFile file)
        {
            var ext = file.FileType.ToLowerInvariant();

            var type = GetMediaType(ext);

            if (type == MediaType.Unknown)
                return null;

            // BasicProperties.DateModified reflects real file changes (edits,
            // re-tags, replacements). DateCreated does NOT change on edit, so it's
            // unusable as a diff signal - a re-tagged file would never look "changed".
            var basicProps = await file.GetBasicPropertiesAsync();

            return new MediaFile
            {
                Id = Guid.NewGuid().ToString(),
                Path = file.Path,
                FileName = file.Name,
                TrackNumber = 0,
                Title = file.DisplayName,
                ArtistId = "unknown",
                Artist = "",
                AlbumArtistId = "unknown",
                AlbumArtist = "",
                AlbumId = "unknown",
                Album = "",
                Year = 0,
                Composers = new List<string>(),
                Genres = new List<string>(),
                Duration = DateTime.Now.TimeOfDay,
                Comments = "",
                Lyrics = "",
                DateModified = basicProps.DateModified,
                Extension = ext,
                Size = (long)basicProps.Size,
                Type = type,
                Bitrate = 0
            };
        }

        private static MediaType GetMediaType(string extension)
        {
            extension = extension.ToLowerInvariant();

            if (FolderManager.AudioExtensions.Contains(extension))
                return MediaType.Audio;

            if (FolderManager.VideoExtensions.Contains(extension))
                return MediaType.Video;

            return MediaType.Unknown;
        }
    }
}
