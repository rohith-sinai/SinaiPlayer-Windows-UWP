using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SinaiPlayer.Lib.Models
{
    public sealed partial class SearchSuggestionItem
    {
        public string Id { get; set; }

        public string Path { get; set; }

        public string Title { get; set; }

        public string Artist { get; set; }

        public string ArtistId { get; set; }

        public string Album { get; set; }

        public string AlbumId { get; set; }

        public Enums.SearchSuggestionType Type { get; set; }
    }
}
