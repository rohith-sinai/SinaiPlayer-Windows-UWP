using System;
using System.Collections.Generic; 

namespace SinaiPlayer.Lib.Models
{
    public sealed partial class Playlist
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public DateTimeOffset DateCreated { get; set; }

        public DateTimeOffset DateModified { get; set; }
         
        public int SortOrder { get; set; }

        public List<PlaylistItem> Items { get; set; } = new List<PlaylistItem>();

        public int Count => Items.Count;
         
    }
}
