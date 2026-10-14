using SinaiPlayer.Lib.Enums;
using SinaiPlayer.Lib.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq; 

namespace SinaiPlayer.Lib.Utils
{
    public static class MediaRecordMapper
    {
        public static MediaRecord FromSong(MediaFile f, string folderId, long fileDateModifiedTicks, long fileSize)
        {
            return new MediaRecord
            {
                Path = f.Path,
                Id = f.Id,
                FileName = f.Title,
                Extension = f.Extension,
                Type = (int)MediaType.Audio,
                FileDateModifiedTicks = fileDateModifiedTicks,
                FileSize = fileSize,
                FolderId = folderId,

                TrackNumber = f.TrackNumber,
                Title = f.Title,
                ArtistId = f.ArtistId,
                Artist = f.Artist,
                AlbumArtistId = f.AlbumArtistId,
                AlbumArtist = f.AlbumArtist,
                AlbumId = f.AlbumId,
                Album = f.Album,
                Year = f.Year,
                Genres = string.Join(", ", f.Genres),
                Composers = string.Join(", ", f.Composers),
                DurationTicks = f.Duration.Ticks,
                Comments = f.Comments,
                Lyrics = f.Lyrics,
                Bitrate = f.Bitrate,
            };
        }

        public static MediaRecord FromVideo(MediaFile f, string folderId, long fileDateModifiedTicks, long fileSize)
        {
            return new MediaRecord
            {
                Path = f.Path,
                Id = f.Id,
                FileName = f.Title,
                Extension = f.Extension,
                Type = (int)MediaType.Video,
                FileDateModifiedTicks = fileDateModifiedTicks,
                FileSize = fileSize,
                FolderId = folderId,

                TrackNumber = f.TrackNumber,
                Title = f.Title,
                ArtistId = f.ArtistId,
                Artist = f.Artist,
                AlbumArtistId = f.AlbumArtistId,
                AlbumArtist = f.AlbumArtist,
                AlbumId = f.AlbumId,
                Album = f.Album,
                Year = f.Year,
                Genres = string.Join(", ", f.Genres),
                Composers = string.Join(", ", f.Composers),
                DurationTicks = f.Duration.Ticks,
                Comments = f.Comments,
                Lyrics = f.Lyrics,
                Bitrate = f.Bitrate,
            };
        }

        public static MediaFile ToSong(MediaRecord r)
        {
            return new MediaFile
            {
                Id = r.Id,
                Path = r.Path,
                FileName = r.FileName,
                TrackNumber = r.TrackNumber,
                Title = r.Title,
                ArtistId = r.ArtistId,
                Artist = r.Artist,
                AlbumArtistId = r.AlbumArtistId,
                AlbumArtist = r.AlbumArtist,
                AlbumId = r.AlbumId,
                Album = r.Album,
                Year = r.Year, 
                Genres = r.Genres.Split(","),
                Composers = r.Composers.Split(","),
                Duration = TimeSpan.FromTicks(r.DurationTicks),
                Comments = r.Comments,
                Lyrics = r.Lyrics,
                DateModified = new DateTimeOffset(r.FileDateModifiedTicks, TimeSpan.Zero),
                Extension = r.Extension,
                Size = r.FileSize,
                Type = GetTypeFromInt(r.Type),
                Bitrate = r.Bitrate,
            };
        }

        public static MediaFile ToVideo(MediaRecord r)
        {
            return new MediaFile
            {
                Id = r.Id,
                Path = r.Path,
                FileName = r.FileName,
                TrackNumber = r.TrackNumber,
                Title = r.Title,
                ArtistId = r.ArtistId,
                Artist = r.Artist,
                AlbumArtistId = r.AlbumArtistId,
                AlbumArtist = r.AlbumArtist,
                AlbumId = r.AlbumId,
                Album = r.Album,
                Year = r.Year,
                Genres = r.Genres.Split(","),
                Composers = r.Composers.Split(","),
                Duration = TimeSpan.FromTicks(r.DurationTicks),
                Comments = r.Comments,
                Lyrics = r.Lyrics,
                DateModified = new DateTimeOffset(r.FileDateModifiedTicks, TimeSpan.Zero),
                Extension = r.Extension,
                Size = r.FileSize,
                Type = GetTypeFromInt(r.Type),
                Bitrate = r.Bitrate,
            };
        }


        private static MediaType GetTypeFromInt(int i)
        {
            MediaType type;
            switch (i)
            {
                case 1:
                    type = MediaType.Audio;
                    break;
                case 2:
                    type = MediaType.Video;
                    break;
                case 0:
                default:
                    type = MediaType.Unknown;
                    break;
            }

            return type;
        }
    }
}
