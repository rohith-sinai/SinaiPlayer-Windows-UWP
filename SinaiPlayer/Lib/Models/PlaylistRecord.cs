using SQLite; 

namespace SinaiPlayer.Lib.Models
{
    [Table("Playlists")]
    public class PlaylistRecord
    {
        [PrimaryKey]
        public string Id { get; set; }

        public string Name { get; set; }

        public long DateCreatedTicks { get; set; }

        public long DateModifiedTicks { get; set; }

        // Manual sort order for the playlists list itself (e.g. drag-to-reorder
        // playlists in a sidebar). Independent of item Position below.
        public int SortOrder { get; set; }
    }
}
