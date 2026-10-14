using SinaiPlayer.Lib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SinaiPlayer.Lib.Loaders
{
    public sealed partial class AlbumLoader
    {

        public static List<Album> LoadAlbums(List<MediaFile> songs)
        {
            return songs
                .GroupBy(x => x.AlbumId)
                .Select(group =>
                {
                    var first = group.First();

                    return new Album
                    {
                        Id = first.AlbumId,

                        Name = first.Album,

                        ArtistId = first.AlbumArtistId,

                        Artist = first.AlbumArtist,

                        Songs = group.OrderBy(x => x.TrackNumber).ToList(),

                        Year = first.Year,
                    };
                })
                .OrderBy(x => x.Name)
                .ToList();
        }

        public static Album LoadAlbum(string albumId, List<MediaFile> songs)
        {
            return LoadAlbums(songs).FirstOrDefault(x => x.Id == albumId);
        }
    }
}
