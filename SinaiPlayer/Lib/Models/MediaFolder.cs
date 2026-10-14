using System; 

namespace SinaiPlayer.Lib.Models
{
    public class MediaFolder
    {
        public string Id { get; set; }

        public string Token { get; set; }

        public string Title { get; set; }

        public string Path { get; set; }

        public DateTimeOffset DateAdded { get; set; }
    }
}
