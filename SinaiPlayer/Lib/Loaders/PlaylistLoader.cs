using SinaiPlayer.Lib.Data;
using SinaiPlayer.Lib.Models;
using SinaiPlayer.Lib.Utils;
using System;
using System.Collections.Generic;
using System.Linq; 
using System.Threading.Tasks;

namespace SinaiPlayer.Lib.Loaders
{
    public sealed partial class PlaylistLoader
    {
        // Builds fully-resolved Playlist models from DB records, matching each
        // item's Path against dictionaries built from the already-loaded
        // Songs/Videos collections rather than re-hitting the database - this
        // stays dependency-free (no reference to Repos) exactly like SongLoader
        // and VideoLoader, which take their input as plain parameters too.
        public static async Task<List<Playlist>> LoadPlaylistsAsync(
            PlaylistDatabase playlistDb,
            IReadOnlyDictionary<string, MediaFile> filesByPath)
        {
            var playlistRecords = await playlistDb.GetAllPlaylistsAsync();

            var result = new List<Playlist>();

            foreach (var record in playlistRecords)
            {
                var playlist = PlaylistMapper.ToPlaylist(record);

                var itemRecords = await playlistDb.GetItemsAsync(record.Id);

                foreach (var itemRecord in itemRecords)
                {
                    var item = PlaylistMapper.ToPlaylistItem(itemRecord);

                    if (filesByPath.TryGetValue(itemRecord.Path, out var file))
                    {
                        item.File = file;
                    }
                    // else: the underlying file was removed from the library since
                    // it was added to this playlist. The item is kept (rather than
                    // silently dropped) so the UI can show a "file not found" row -
                    // PlaylistItem.IsMissing reflects that state.

                    playlist.Items.Add(item);
                }

                result.Add(playlist);
            }

            return result.OrderBy(p => p.SortOrder).ToList();
        }

        public static Dictionary<string, MediaFile> IndexByPath(IEnumerable<MediaFile> files) =>
            files.ToDictionary(f => f.Path, f => f);

    }
}
