using SinaiPlayer.Lib.Models;
using System; 

namespace SinaiPlayer.Lib.Utils
{
    public static class PlaylistMapper
    {
        public static Playlist ToPlaylist(PlaylistRecord r)
        {
            return new Playlist
            {
                Id = r.Id,
                Name = r.Name,
                DateCreated = new DateTimeOffset(r.DateCreatedTicks, TimeSpan.Zero),
                DateModified = new DateTimeOffset(r.DateModifiedTicks, TimeSpan.Zero),
                SortOrder = r.SortOrder,
            };
        }

        // Note: Song/Video are deliberately left unset here - resolving a Path to
        // an actual Song or Video is PlaylistLoader's job, since it needs the
        // in-memory library index to do that lookup.
        public static PlaylistItem ToPlaylistItem(PlaylistItemRecord r)
        {
            return new PlaylistItem
            {
                RowId = r.RowId,
                PlaylistId = r.PlaylistId,
                Path = r.Path,
                Position = r.Position,
            };
        }
    }
}
