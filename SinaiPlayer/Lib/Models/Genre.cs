using System.Collections.Generic;

namespace SinaiPlayer.Lib.Models
{
    public sealed partial class Genre
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public List<MediaFile> Songs { get; set; }
    }
}
