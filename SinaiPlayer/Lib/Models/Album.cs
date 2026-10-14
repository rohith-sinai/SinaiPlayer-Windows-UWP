using System.Collections.Generic; 

namespace SinaiPlayer.Lib.Models
{
    public sealed partial class Album
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string ArtistId { get; set; }

        public string Artist { get; set; }

        public List<MediaFile> Songs { get; set; }

        public uint Year { get; set; }
    }
}
