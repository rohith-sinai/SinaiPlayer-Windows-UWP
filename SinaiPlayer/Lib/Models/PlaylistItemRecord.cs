using SQLite; 

namespace SinaiPlayer.Lib.Models
{
    [Table("PlaylistItems")]
    public class PlaylistItemRecord
    {
        [PrimaryKey, AutoIncrement]
        public int RowId { get; set; }

        [Indexed]
        public string PlaylistId { get; set; }

        // References MediaFiles.Path. Not a DB-enforced foreign key (sqlite-net
        // doesn't support those), just a convention matched at query time -
        // same approach FolderId uses in MediaRecord.
        [Indexed]
        public string Path { get; set; }

        // Position within THIS playlist. Duplicates of the same Path are allowed
        // (a song can legitimately appear twice in a playlist), which is why
        // RowId - not (PlaylistId, Path) - is the identity used for removal/reorder.
        public int Position { get; set; }
    }
}
