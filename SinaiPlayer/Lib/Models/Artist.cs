using System.Collections.Generic; 

namespace SinaiPlayer.Lib.Models
{
    public sealed partial class Artist
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public List<Album> Albums { get; set; }

        public List<MediaFile> Songs { get; set; }

        public IList<string> Genres { get; set; }
    }
}
