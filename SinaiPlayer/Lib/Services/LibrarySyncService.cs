using SinaiPlayer.Lib.Data;
using SinaiPlayer.Lib.Enums;
using SinaiPlayer.Lib.Loaders;
using SinaiPlayer.Lib.Models;
using SinaiPlayer.Lib.Utils; 
using System.Collections.Generic;
using System.Linq; 
using System.Threading.Tasks;
using Windows.Storage;

namespace SinaiPlayer.Lib.Services
{
    public static class LibrarySyncService
    {
        public static async Task<FolderSyncResult> SyncFolderAsync(
            MediaFolder folderInfo,
            StorageFolder folder,
            MediaDatabase db)
        {
            var result = new FolderSyncResult();

            // 1) Cheap disk walk: path + timestamp + size only, no metadata reads.
            List<MediaFile> currentFiles = await MediaLoader.LoadMediaFilesAsync(folder);
            var currentByPath = currentFiles.ToDictionary(f => f.Path);

            // 2) What we already believe is true, from the last sync.
            Dictionary<string, long> previousSnapshot =
                await db.GetSnapshotForFolderAsync(folderInfo.Id);

            // 3) Diff: only files that are new or whose timestamp moved need a real scan.
            var toRescan = new List<MediaFile>();

            foreach (var file in currentFiles)
            {
                bool isNew = !previousSnapshot.TryGetValue(file.Path, out long prevTicks);
                bool isChanged = !isNew && prevTicks != file.DateModified.UtcTicks;

                if (isNew || isChanged)
                {
                    toRescan.Add(file);
                }
            }

            var removedPaths = previousSnapshot.Keys
                .Except(currentByPath.Keys)
                .ToList();

            if (toRescan.Count == 0 && removedPaths.Count == 0)
            {
                result.Changed = false;
                return result;
            }

            // 4) Only NOW do the expensive per-file metadata reads, and only for the subset
            //    that actually changed - this is the step that made full startup slow.
            var songFiles = toRescan.Where(f => f.Type == MediaType.Audio).ToList();
            var videoFiles = toRescan.Where(f => f.Type == MediaType.Video).ToList();

            List<MediaFile> newFiles = toRescan.ToList();
            List<MediaFile> newSongs = await SongLoader.LoadSongsAsync(songFiles);
            List<MediaFile> newVideos = await VideoLoader.LoadVideosAsync(videoFiles);

            // 5) Persist to the local cache.
            var records = new List<MediaRecord>();

            foreach (var s in newSongs)
            {
                var f = currentByPath[s.Path];
                records.Add(MediaRecordMapper.FromSong(s, folderInfo.Id, f.DateModified.UtcTicks, (long)f.Size));
            }

            foreach (var v in newVideos)
            {
                var f = currentByPath[v.Path];
                records.Add(MediaRecordMapper.FromVideo(v, folderInfo.Id, f.DateModified.UtcTicks, (long)f.Size));
            }

            if (records.Count > 0)
            {
                await db.UpsertBatchAsync(records);
            }

            if (removedPaths.Count > 0)
            {
                await db.DeleteByPathsAsync(removedPaths);
            }

            result.Changed = true;
            result.UpsertedMediaFiles = newFiles;
            result.UpsertedSongs = newSongs;
            result.UpsertedVideos = newVideos;
            result.RemovedPaths = removedPaths;
            return result;
        }
    }

    public sealed class FolderSyncResult
    {
        public bool Changed { get; set; }
        public List<MediaFile> UpsertedMediaFiles { get; set; } = new List<MediaFile>();
        public List<MediaFile> UpsertedSongs { get; set; } = new List<MediaFile>();
        public List<MediaFile> UpsertedVideos { get; set; } = new List<MediaFile>();
        public List<string> RemovedPaths { get; set; } = new List<string>();
    }
}
