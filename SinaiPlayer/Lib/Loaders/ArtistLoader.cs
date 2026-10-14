using SinaiPlayer.Lib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SinaiPlayer.Lib.Loaders
{
    public sealed partial class ArtistLoader
    {

        public static List<Artist> LoadAlbumArtists(List<MediaFile> songs)
        {
            return songs
                .GroupBy(x => x.AlbumArtistId)
                .Select(group =>
                {
                    var first = group.First();

                    var albums = AlbumLoader.LoadAlbums(group.ToList());

                    return new Artist
                    {
                        Id = first.AlbumArtistId,

                        Name = first.AlbumArtist,

                        Albums = albums,

                        Songs = group.ToList(),

                        Genres = first.Genres,
                    };
                })
                .OrderBy(x => x.Name)
                .ToList();
        }


        public static List<Artist> LoadArtists(List<MediaFile> songs)
        {
            return songs
                .GroupBy(x => x.ArtistId)
                .Select(group =>
                {
                    var first = group.First();

                    var albums = AlbumLoader.LoadAlbums(group.ToList());

                    return new Artist
                    {
                        Id = first.ArtistId,

                        Name = first.Artist,

                        Albums = albums,

                        Songs = group.ToList(),

                        Genres = first.Genres,
                    };
                })
                .OrderBy(x => x.Name)
                .ToList();
        }
    }
}
