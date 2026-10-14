using SinaiPlayer.Lib.Data;
using SinaiPlayer.Lib.Loaders;
using SinaiPlayer.Lib.Models;
using SinaiPlayer.Lib.Services;
using SinaiPlayer.Lib.UI.ViewModels;
using SinaiPlayer.Lib.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.AccessCache;

namespace SinaiPlayer.Lib.Repos
{
    public static class LibraryRepository
    {
        public static ObservableCollection<MediaFile> MediaFiles { get; } = new ObservableCollection<MediaFile>();
        public static ObservableCollection<MediaFile> Songs { get; } = new ObservableCollection<MediaFile>();
        public static ObservableCollection<SongViewModel> SongItems { get; } = new ObservableCollection<SongViewModel>();
        public static ObservableCollection<ArtistViewModel> ArtistItems { get; } = new ObservableCollection<ArtistViewModel>();
        public static ObservableCollection<AlbumViewModel> AlbumItems { get; } = new ObservableCollection<AlbumViewModel>();
        public static ObservableCollection<VideoViewModel> VideoItems { get; } = new ObservableCollection<VideoViewModel>();
        public static ObservableCollection<MediaFile> Videos { get; } = new ObservableCollection<MediaFile>();

        private static readonly MediaDatabase _db = new MediaDatabase();
        private static bool _initialized;

        // Call once at app startup, before LoadFromCacheAsync.
        public static async Task InitializeAsync()
        {
            if (_initialized) return;
            await _db.InitializeAsync();
            _initialized = true;
        }

        // STEP 1 - instant: populate the UI straight from the local cache.
        public static async Task LoadFromCacheAsync()
        {
            var records = await _db.GetAllAsync();

            var files = records.ToList();

            var songs = records.Where(r => r.Type == (int)Enums.MediaType.Audio)
                                .Select(MediaRecordMapper.ToSong)
                                .ToList();

            var videos = records.Where(r => r.Type == (int)Enums.MediaType.Video)
                                 .Select(MediaRecordMapper.ToVideo)
                                 .ToList();

            MediaFiles.Clear();
            ReplaceSongs(songs);
            ReplaceVideos(videos);
            RebuildArtistsAndAlbums();
        }

        // STEP 2 - background: diffs every known folder against disk.
        // Call this after LoadFromCacheAsync on every app startup.
        public static async Task SyncAsync(IProgress<string> progress = null)
        {
            var folders = FolderManager.GetFolders();
            if (folders.Count == 0) return;

            bool anyChanges = false;

            foreach (var folderInfo in folders)
            {
                if (folderInfo == null || string.IsNullOrWhiteSpace(folderInfo.Token))
                    continue;

                progress?.Report($"Checking {folderInfo.Title}...");

                bool changed = await SyncOneFolderAsync(folderInfo);
                anyChanges |= changed;
            }

            if (anyChanges)
            {
                RebuildArtistsAndAlbums();
            }
        }

        // Sync a SINGLE folder - used right after a folder is added, so we don't
        // have to re-diff every other already-cached folder just to pick up one new one.
        public static async Task SyncFolderAsync(MediaFolder folderInfo)
        {
            bool changed = await SyncOneFolderAsync(folderInfo);

            if (changed)
            {
                RebuildArtistsAndAlbums();
            }
        }

        // Drop a folder's cached rows and pull its songs/videos out of the UI -
        // no disk access needed at all, since we're removing, not scanning.
        public static async Task RemoveFolderAsync(string folderId)
        {
            var snapshot = await _db.GetSnapshotForFolderAsync(folderId);
            var paths = snapshot.Keys.ToList();

            if (paths.Count == 0)
                return;

            RemoveByPaths(paths);

            await _db.DeleteByFolderIdAsync(folderId);

            RebuildArtistsAndAlbums();
        }

        private static async Task<bool> SyncOneFolderAsync(MediaFolder folderInfo)
        {
            if (folderInfo == null || string.IsNullOrWhiteSpace(folderInfo.Token))
                return false;

            Windows.Storage.StorageFolder folder;
            try
            {
                folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(folderInfo.Token);
            }
            catch
            {
                // Token is stale (folder moved/deleted/permission revoked).
                // Surface this to the user rather than silently skipping, in a real UI.
                return false;
            }

            var result = await LibrarySyncService.SyncFolderAsync(folderInfo, folder, _db);

            if (result.Changed)
            {
                ApplyIncrementalChanges(result);
            }

            return result.Changed;
        }

        // Surgical updates instead of Clear()+ReAdd - keeps this cheap even when the
        // library is huge and only a handful of files actually changed.
        private static void ApplyIncrementalChanges(FolderSyncResult result)
        {
            RemoveByPaths(result.RemovedPaths);

            foreach (var song in result.UpsertedSongs)
            {
                var existing = Songs.FirstOrDefault(s => s.Path == song.Path);
                if (existing != null) Songs.Remove(existing);
                Songs.Add(song);

                var existingMedia = MediaFiles.FirstOrDefault(f => f.Path == song.Path);
                if (existingMedia != null) MediaFiles.Remove(existingMedia);
                MediaFiles.Add(song);

                var existingVm = SongItems.FirstOrDefault(v => v.Path == song.Path);
                if (existingVm != null) SongItems.Remove(existingVm);
                SongItems.Add(new SongViewModel(song));
            }

            foreach (var video in result.UpsertedVideos)
            {
                var existing = Videos.FirstOrDefault(v => v.Path == video.Path);
                if (existing != null) Videos.Remove(existing);
                Videos.Add(video);

                var existingMedia = MediaFiles.FirstOrDefault(f => f.Path == video.Path);
                if (existingMedia != null) MediaFiles.Remove(existingMedia);
                MediaFiles.Add(video);

                var existingVm = VideoItems.FirstOrDefault(v => v.Path == video.Path);
                if (existingVm != null) VideoItems.Remove(existingVm);
                VideoItems.Add(new VideoViewModel(video));
            }
        }

        private static void RemoveByPaths(IEnumerable<string> paths)
        {
            var pathSet = new HashSet<string>(
                paths ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            if (pathSet.Count == 0)
                return;

            foreach (var file in MediaFiles
                .Where(f => f != null && pathSet.Contains(f.Path))
                .ToList())
            {
                MediaFiles.Remove(file);
            }

            foreach (var song in Songs
                .Where(s => s != null && pathSet.Contains(s.Path))
                .ToList())
            {
                Songs.Remove(song);
            }

            foreach (var video in Videos
                .Where(v => v != null && pathSet.Contains(v.Path))
                .ToList())
            {
                Videos.Remove(video);
            }

            foreach (var vm in SongItems
                .Where(v => v != null && pathSet.Contains(v.Path))
                .ToList())
            {
                SongItems.Remove(vm);
            }

            foreach (var vm in VideoItems
                .Where(v => v != null && pathSet.Contains(v.Path))
                .ToList())
            {
                VideoItems.Remove(vm);
            }
        }

        private static void ReplaceSongs(List<MediaFile> songs)
        {
            Songs.Clear();
            SongItems.Clear();
            foreach (var s in songs)
            {
                Songs.Add(s);
                MediaFiles.Add(s);
                SongItems.Add(new SongViewModel(s));
            }
        }

        private static void ReplaceVideos(List<MediaFile> videos)
        {
            Videos.Clear();
            VideoItems.Clear();
            foreach (var v in videos)
            {
                Videos.Add(v);
                MediaFiles.Add(v);
                VideoItems.Add(new VideoViewModel(v));
            }
        }

        private static void RebuildArtistsAndAlbums()
        {
            var songList = Songs.ToList();

            var artists = ArtistLoader.LoadAlbumArtists(songList);
            ArtistItems.Clear();
            foreach (var a in artists)
                ArtistItems.Add(new ArtistViewModel(a));

            var albums = AlbumLoader.LoadAlbums(songList);
            AlbumItems.Clear();
            foreach (var a in albums)
                AlbumItems.Add(new AlbumViewModel(a));
        }

        public static async Task DeleteFilesAsync(IEnumerable<MediaFile> files)
        {
            if (files == null)
                return;

            var fileList = files
                .Where(f => f != null && !string.IsNullOrWhiteSpace(f.Path))
                .GroupBy(f => f.Path, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            if (fileList.Count == 0)
                return;

            var deletedPaths = new List<string>();

            foreach (var file in fileList)
            {
                try
                {
                    var storageFile =
                        await Windows.Storage.StorageFile.GetFileFromPathAsync(file.Path);

                    await storageFile.DeleteAsync(
                        Windows.Storage.StorageDeleteOption.PermanentDelete);

                    deletedPaths.Add(file.Path);
                }
                catch (FileNotFoundException)
                {
                    // Already deleted. Treat as successfully removed.
                    deletedPaths.Add(file.Path);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Delete failed: {file.Path}\n{ex}");
                }
            }

            if (deletedPaths.Count == 0)
                return;

            /*
             * Queue + current playback.
             */
            await MediaPlayerService.Instance
                .RemoveFilesFromQueueAsync(deletedPaths);

            /*
             * Playlists.
             */
            await PlaylistRepository.RemoveAllReferencesToPathsAsync(deletedPaths);

            /*
             * Local media database.
             */
            await _db.DeleteByPathsAsync(deletedPaths);

            /*
             * In-memory library.
             */
            RemoveFromLibraryCollections(deletedPaths);

            /*
             * Artists/albums may have become empty.
             */
            RebuildArtistsAndAlbums();
        }

        private static void RemoveFromLibraryCollections(IEnumerable<string> paths)
        {
            var pathSet = new HashSet<string>(
                paths,
                StringComparer.OrdinalIgnoreCase);

            if (pathSet.Count == 0)
                return;

            foreach (var file in MediaFiles
                .Where(f => f != null && pathSet.Contains(f.Path))
                .ToList())
            {
                MediaFiles.Remove(file);
            }

            foreach (var song in Songs
                .Where(s => s != null && pathSet.Contains(s.Path))
                .ToList())
            {
                Songs.Remove(song);
            }

            foreach (var video in Videos
                .Where(v => v != null && pathSet.Contains(v.Path))
                .ToList())
            {
                Videos.Remove(video);
            }

            foreach (var item in SongItems
                .Where(v => v != null && pathSet.Contains(v.Path))
                .ToList())
            {
                SongItems.Remove(item);
            }

            foreach (var item in VideoItems
                .Where(v => v != null && pathSet.Contains(v.Path))
                .ToList())
            {
                VideoItems.Remove(item);
            }
        }

        public static async Task UpdateSongAfterTagEditAsync(MediaFile oldSong, MediaFile newSong)
        {
            if (oldSong == null || newSong == null)
                return;

            if (string.IsNullOrWhiteSpace(newSong.Path))
                return;

            /*
             * Find the existing database record.
             */
            MediaRecord existingRecord =
                await _db.GetByPathAsync(newSong.Path);

            if (existingRecord == null)
                return;

            /*
             * Preserve database information that isn't represented
             * by the editable dialog.
             */
            MediaRecord updatedRecord =
                MediaRecordMapper.FromSong(
                    newSong,
                    existingRecord.FolderId,
                    newSong.DateModified.UtcTicks,
                    (long)newSong.Size);

            await _db.UpsertAsync(updatedRecord);

            /*
             * Replace the MediaFile in the main collections.
             */
            ReplaceSongInCollections(oldSong.Path, newSong);

            /*
             * Album/artist grouping may have changed because Title,
             * Artist, Album or AlbumArtist changed.
             */
            RebuildArtistsAndAlbums();

            /*
             * Playlists contain the path as their stable identity.
             * Only their in-memory File object needs to be refreshed.
             */
            PlaylistRepository.UpdateSongReference(
                oldSong.Path,
                newSong);
        }

        private static void ReplaceSongInCollections(string path, MediaFile newSong)
        {
            if (string.IsNullOrWhiteSpace(path) || newSong == null)
                return;

            /*
             * Songs
             */
            var oldSong =
                Songs.FirstOrDefault(
                    x => x != null &&
                         string.Equals(
                             x.Path,
                             path,
                             StringComparison.OrdinalIgnoreCase));

            if (oldSong != null)
            {
                int index = Songs.IndexOf(oldSong);

                Songs.RemoveAt(index);

                if (index >= 0 && index <= Songs.Count)
                    Songs.Insert(index, newSong);
                else
                    Songs.Add(newSong);
            }

            /*
             * MediaFiles
             */
            var oldMediaFile =
                MediaFiles.FirstOrDefault(
                    x => x != null &&
                         string.Equals(
                             x.Path,
                             path,
                             StringComparison.OrdinalIgnoreCase));

            if (oldMediaFile != null)
            {
                int index = MediaFiles.IndexOf(oldMediaFile);

                MediaFiles.RemoveAt(index);

                if (index >= 0 && index <= MediaFiles.Count)
                    MediaFiles.Insert(index, newSong);
                else
                    MediaFiles.Add(newSong);
            }

            /*
             * SongItems
             *
             * Your existing incremental synchronization already replaces
             * the SongViewModel instead of mutating the old one, so we follow
             * the same pattern here.
             */
            var oldVm =
                SongItems.FirstOrDefault(
                    x => x != null &&
                         string.Equals(
                             x.Path,
                             path,
                             StringComparison.OrdinalIgnoreCase));

            if (oldVm != null)
            {
                int index = SongItems.IndexOf(oldVm);

                SongItems.RemoveAt(index);

                var newVm = new SongViewModel(newSong);

                if (index >= 0 && index <= SongItems.Count)
                    SongItems.Insert(index, newVm);
                else
                    SongItems.Add(newVm);
            }
        }

        public static async Task ApplyAlbumTagChangesAsync(IEnumerable<MediaFile> oldSongs, IEnumerable<MediaFile> updatedSongs)
        {
            if (updatedSongs == null)
                return;

            var updatedList =
                updatedSongs
                    .Where(x => x != null &&
                                !string.IsNullOrWhiteSpace(x.Path))
                    .ToList();

            if (updatedList.Count == 0)
                return;

            /*
             * Update the database.
             */
            foreach (var updatedSong in updatedList)
            {
                var existing =
                    await _db.GetByPathAsync(updatedSong.Path);

                if (existing == null)
                    continue;

                var record =
                    MediaRecordMapper.FromSong(
                        updatedSong,
                        existing.FolderId,
                        updatedSong.DateModified.UtcTicks,
                        (long)updatedSong.Size);

                await _db.UpsertAsync(record);
            }

            /*
             * Replace songs in memory.
             */
            foreach (var updatedSong in updatedList)
            {
                ReplaceSongInCollections(
                    updatedSong.Path,
                    updatedSong);
            }

            /*
             * Album/artist grouping is derived from Songs,
             * therefore rebuild it once after ALL songs have changed.
             */
            RebuildArtistsAndAlbums();

            /*
             * Refresh every playlist item pointing to any
             * affected file.
             */
            PlaylistRepository.UpdateSongReferences(updatedList);
        }
    }
}