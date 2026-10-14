using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SinaiPlayer.Lib.Models
{
    public class PlaylistItem
    { 
        public int RowId { get; set; }

        public string PlaylistId { get; set; }

        public string Path { get; set; }

        public int Position { get; set; }

        public MediaFile File { get; set; }

        public bool IsVideo => File != null &&  File.Type == Enums.MediaType.Video;

        public bool IsMissing => File == null;

    }
}
