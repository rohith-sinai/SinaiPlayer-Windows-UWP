using SQLite;
using System;

namespace SinaiPlayer.Lib.Models
{
    [Table("MediaFiles")]
    public class MediaRecord
    {
        [PrimaryKey]
        public string Path { get; set; }

        public string Id { get; set; }

        public string FileName { get; set; }

        public string Extension { get; set; }

        // MediaType enum stored as int (0 = Unknown, 1 = Audio, 2 = Video - match your enum order)
        public int Type { get; set; }

        // File system last-modified, in UTC ticks. This is what we diff against
        // on every sync - NOT the same as DateCreated, which never changes on edit.
        public long FileDateModifiedTicks { get; set; }

        public long FileSize { get; set; }

        [Indexed]
        public string FolderId { get; set; }

        // --- Song-only fields (unused/default for videos) ---
        public uint TrackNumber { get; set; }

        public string Title { get; set; }

        [Indexed] public string ArtistId { get; set; }

        public string Artist { get; set; }

        [Indexed] public string AlbumArtistId { get; set; }

        public string AlbumArtist { get; set; }

        [Indexed] public string AlbumId { get; set; }

        public string Album { get; set; }

        public uint Year { get; set; }

        public string Genres { get; set; }

        public string Composers { get; set; }

        public long DurationTicks { get; set; }

        public string Comments { get; set; }

        public string Lyrics { get; set; }

        public uint Bitrate { get; set; }
    }
}
