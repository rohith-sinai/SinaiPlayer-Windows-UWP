using SinaiPlayer.Lib.Enums;
using System;
using System.Collections.Generic;

namespace SinaiPlayer.Lib.Models
{
    public sealed partial class MediaFile
    {
        public string Id { get; set; }

        public string Path { get; set; }

        public string FileName { get; set; }

        public uint TrackNumber { get; set; }

        public string Title { get; set; }

        public string ArtistId { get; set; }

        public string Artist { get; set; }

        public string AlbumArtistId { get; set; }

        public string AlbumArtist { get; set; }

        public string AlbumId { get; set; }

        public string Album { get; set; }

        public uint Year { get; set; }

        public IList<string> Composers { get; set; }

        public IList<string> Genres { get; set; }

        public TimeSpan Duration { get; set; }

        public string Comments { get; set; }

        public string Lyrics { get; set; }

        public DateTimeOffset DateModified { get; set; }

        public string Extension { get; set; }

        public long Size { get; set; }

        public MediaType Type { get; set; }

        public uint Bitrate { get; set; }

    }
}
