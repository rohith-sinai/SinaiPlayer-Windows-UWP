using SinaiPlayer.Lib.Data;
using SinaiPlayer.Lib.Loaders;
using SinaiPlayer.Lib.Models;
using SinaiPlayer.Lib.UI.ViewModels;
using SinaiPlayer.Lib.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq; 
using System.Threading.Tasks;

namespace SinaiPlayer.Lib.Repos
{
    public static class PlaylistRepository
    {
        public static ObservableCollection<PlaylistViewModel> Playlists { get; } = new ObservableCollection<PlaylistViewModel>();

        private static readonly PlaylistDatabase _db = new PlaylistDatabase();
        private static bool _initialized;

        // Call once at app startup, alongside LibraryRepository.InitializeAsync.
        public static async Task InitializeAsync()
        {
            if (_initialized) return;
            await _db.InitializeAsync();
            _initialized = true;
        }

        // Call AFTER LibraryRepository.LoadFromCacheAsync (and ideally after
        // SyncAsync) - playlist items resolve against whatever Songs/Videos are
        // already sitting in LibraryRepository at the time this runs.
        public static async Task LoadAsync()
        {
            var filesByPath = PlaylistLoader.IndexByPath(LibraryRepository.MediaFiles);

            var playlists = await PlaylistLoader.LoadPlaylistsAsync(_db, filesByPath);

            Playlists.Clear();
            foreach (var p in playlists)
                Playlists.Add(new PlaylistViewModel(p));
        }

        public static async Task<PlaylistViewModel> CreatePlaylistAsync(string name)
        {
            name = string.IsNullOrWhiteSpace(name) ? "Untitled Playlist" : name.Trim();

            var record = await _db.CreatePlaylistAsync(name);

            var vm = new PlaylistViewModel(PlaylistMapper.ToPlaylist(record));
            Playlists.Add(vm);

            return vm;
        }

        public static async Task RenamePlaylistAsync(PlaylistViewModel playlist, string newName)
        {
            if (playlist == null || string.IsNullOrWhiteSpace(newName))
                return;

            newName = newName.Trim();

            await _db.RenamePlaylistAsync(playlist.Id, newName);

            playlist.Rename(newName);
        }

        public static async Task DeletePlaylistAsync(PlaylistViewModel playlist)
        {
            if (playlist == null) return;

            await _db.DeletePlaylistAsync(playlist.Id);

            Playlists.Remove(playlist);
        }

        public static async Task AddFilesAsync(PlaylistViewModel playlist, IEnumerable<MediaFile> files)
        {
            var filesList = files as IList<MediaFile> ?? files.ToList();
            var paths = filesList.Select(s => s.Path).ToList();

            var newRecords = await _db.AddItemsAsync(playlist.Id, paths);

            foreach (var record in newRecords)
            {
                var item = PlaylistMapper.ToPlaylistItem(record);
                item.File = filesList.FirstOrDefault(s => s.Path == record.Path);
                playlist.Items.Add(new PlaylistItemViewModel(item));
            }
        }


        public static async Task RemoveItemAsync(PlaylistViewModel playlist, PlaylistItemViewModel item)
        {
            await _db.RemoveItemAsync(item.RowId);

            playlist.Items.Remove(item);
        }

        // Call after a drag-and-drop reorder has already rearranged
        // playlist.Items client-side (e.g. via ListView reorder) - this just
        // persists whatever order the collection is currently in.
        public static Task ReorderAsync(PlaylistViewModel playlist)
        {
            var rowIds = playlist.Items.Select(i => i.RowId).ToList();

            return _db.ReorderAsync(playlist.Id, rowIds);
        }

        // Call this from LibraryRepository whenever a file is removed from the
        // library (folder removed, file deleted from disk, etc.) so playlists
        // don't keep dangling references to it.
        public static async Task RemoveAllReferencesToPathsAsync(IEnumerable<string> paths)
        {
            if (paths == null)
                return;

            var pathList = paths
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (pathList.Count == 0)
                return;

            foreach (var path in pathList)
            {
                await _db.RemoveAllReferencesToPathAsync(path);
            }

            var pathSet = new HashSet<string>(
                pathList,
                StringComparer.OrdinalIgnoreCase);

            foreach (var playlist in Playlists)
            {
                var staleItems = playlist.Items
                    .Where(i => i != null && pathSet.Contains(i.Path))
                    .ToList();

                foreach (var item in staleItems)
                    playlist.Items.Remove(item);
            }
        }

        /// <summary>
        /// Refreshes the resolved MediaFile for every playlist item
        /// referencing the specified path.
        ///
        /// The playlist database record is NOT changed because the path
        /// remains the same. Only the in-memory PlaylistItemViewModel
        /// is replaced with a refreshed one.
        /// </summary>
        public static void UpdateSongReference(
            string path,
            MediaFile updatedSong)
        {
            if (string.IsNullOrWhiteSpace(path) || updatedSong == null)
                return;

            foreach (var playlist in Playlists)
            {
                if (playlist?.Items == null)
                    continue;

                for (int i = 0; i < playlist.Items.Count; i++)
                {
                    var itemViewModel = playlist.Items[i];

                    if (itemViewModel == null)
                        continue;

                    if (!string.Equals(
                            itemViewModel.Path,
                            path,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    ReplacePlaylistItemViewModel(
                        playlist,
                        i,
                        itemViewModel,
                        updatedSong);
                }
            }
        }


        /// <summary>
        /// Refreshes the resolved MediaFile for all playlist items
        /// belonging to the supplied updated songs.
        ///
        /// Playlist row IDs, positions and paths are preserved.
        /// </summary>
        public static void UpdateSongReferences(
            IEnumerable<MediaFile> updatedSongs)
        {
            if (updatedSongs == null)
                return;

            var songsByPath =
                updatedSongs
                    .Where(song =>
                        song != null &&
                        !string.IsNullOrWhiteSpace(song.Path))
                    .GroupBy(
                        song => song.Path,
                        StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Last(),
                        StringComparer.OrdinalIgnoreCase);

            if (songsByPath.Count == 0)
                return;

            foreach (var playlist in Playlists)
            {
                if (playlist?.Items == null)
                    continue;

                /*
                 * Iterate backwards because we're replacing collection
                 * elements while iterating through the playlist.
                 */
                for (int i = playlist.Items.Count - 1; i >= 0; i--)
                {
                    var itemViewModel = playlist.Items[i];

                    if (itemViewModel == null ||
                        string.IsNullOrWhiteSpace(itemViewModel.Path))
                    {
                        continue;
                    }

                    if (!songsByPath.TryGetValue(
                            itemViewModel.Path,
                            out MediaFile updatedSong))
                    {
                        continue;
                    }

                    ReplacePlaylistItemViewModel(
                        playlist,
                        i,
                        itemViewModel,
                        updatedSong);
                }
            }
        }


        /// <summary>
        /// Creates a new PlaylistItemViewModel containing the same
        /// playlist database identity/order/path but with the updated
        /// MediaFile.
        /// </summary>
        private static void ReplacePlaylistItemViewModel(
            PlaylistViewModel playlist,
            int index,
            PlaylistItemViewModel oldItem,
            MediaFile updatedSong)
        {
            if (playlist == null ||
                oldItem == null ||
                updatedSong == null ||
                index < 0 ||
                index >= playlist.Items.Count)
            {
                return;
            }

            var updatedItem = new PlaylistItem
            {
                RowId = oldItem.RowId,
                PlaylistId = oldItem.PlaylistId,
                Path = oldItem.Path,
                Position = oldItem.Position,
                File = updatedSong
            };

            var updatedViewModel =
                new PlaylistItemViewModel(updatedItem);

            playlist.Items[index] = updatedViewModel;
        }
    }
}
