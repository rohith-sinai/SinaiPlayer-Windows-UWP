using SinaiPlayer.Lib.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; 
using System.Threading.Tasks;

namespace SinaiPlayer.Lib.Data
{
    public sealed class PlaylistDatabase
    {
        private SQLiteAsyncConnection _connection;

        public async Task InitializeAsync()
        {
            string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "library.db");

            _connection = new SQLiteAsyncConnection(dbPath);

            await _connection.ExecuteScalarAsync<string>("PRAGMA journal_mode=WAL;");

            await _connection.CreateTableAsync<PlaylistRecord>();
            await _connection.CreateTableAsync<PlaylistItemRecord>();
        }

        public Task<List<PlaylistRecord>> GetAllPlaylistsAsync()
        {
            return _connection.Table<PlaylistRecord>()
                               .OrderBy(p => p.SortOrder)
                               .ToListAsync();
        }

        public Task<List<PlaylistItemRecord>> GetItemsAsync(string playlistId)
        {
            return _connection.Table<PlaylistItemRecord>()
                               .Where(i => i.PlaylistId == playlistId)
                               .OrderBy(i => i.Position)
                               .ToListAsync();
        }

        public async Task<PlaylistRecord> CreatePlaylistAsync(string name)
        {
            int count = await _connection.Table<PlaylistRecord>().CountAsync();

            var record = new PlaylistRecord
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                DateCreatedTicks = DateTimeOffset.UtcNow.Ticks,
                DateModifiedTicks = DateTimeOffset.UtcNow.Ticks,
                SortOrder = count,
            };

            await _connection.InsertAsync(record);

            return record;
        }

        public Task RenamePlaylistAsync(string playlistId, string newName)
        {
            return _connection.ExecuteAsync(
                "UPDATE Playlists SET Name = ?, DateModifiedTicks = ? WHERE Id = ?",
                newName, DateTimeOffset.UtcNow.Ticks, playlistId);
        }

        public Task DeletePlaylistAsync(string playlistId)
        {
            return _connection.RunInTransactionAsync(conn =>
            {
                conn.Execute("DELETE FROM PlaylistItems WHERE PlaylistId = ?", playlistId);
                conn.Execute("DELETE FROM Playlists WHERE Id = ?", playlistId);
            });
        }

        // Appends paths to the end of the playlist, preserving the order the
        // caller passed them in. Returns the inserted rows (with RowId populated)
        // so the caller can build matching view-models without a re-query.
        public async Task<List<PlaylistItemRecord>> AddItemsAsync(string playlistId, IEnumerable<string> paths)
        {
            var pathList = paths as IList<string> ?? paths.ToList();
            if (pathList.Count == 0)
                return new List<PlaylistItemRecord>();

            var existing = await _connection.Table<PlaylistItemRecord>()
                                             .Where(i => i.PlaylistId == playlistId)
                                             .ToListAsync();

            int nextPosition = existing.Count == 0 ? 0 : existing.Max(i => i.Position) + 1;

            var newItems = pathList
                .Select((path, offset) => new PlaylistItemRecord
                {
                    PlaylistId = playlistId,
                    Path = path,
                    Position = nextPosition + offset,
                })
                .ToList();

            await _connection.RunInTransactionAsync(conn =>
            {
                foreach (var item in newItems)
                {
                    conn.Insert(item);
                }
            });

            await TouchPlaylistAsync(playlistId);

            return newItems;
        }

        public Task RemoveItemAsync(int rowId)
        {
            return _connection.ExecuteAsync("DELETE FROM PlaylistItems WHERE RowId = ?", rowId);
        }

        public Task RemoveItemsAsync(IEnumerable<int> rowIds)
        {
            var list = rowIds as IList<int> ?? rowIds.ToList();
            if (list.Count == 0)
                return Task.CompletedTask;

            return _connection.RunInTransactionAsync(conn =>
            {
                foreach (var rowId in list)
                {
                    conn.Execute("DELETE FROM PlaylistItems WHERE RowId = ?", rowId);
                }
            });
        }

        // Strips every reference to a path across ALL playlists. Call this from
        // LibraryRepository whenever a file drops out of the library (deleted,
        // folder removed, etc.) so playlists don't quietly accumulate dead rows.
        public Task RemoveAllReferencesToPathAsync(string path)
        {
            return _connection.ExecuteAsync("DELETE FROM PlaylistItems WHERE Path = ?", path);
        }

        // Persists a full reorder: pass RowIds in their new desired order and
        // Position gets rewritten to match that order, 0-based.
        public async Task ReorderAsync(string playlistId, IReadOnlyList<int> rowIdsInOrder)
        {
            await _connection.RunInTransactionAsync(conn =>
            {
                for (int position = 0; position < rowIdsInOrder.Count; position++)
                {
                    conn.Execute(
                        "UPDATE PlaylistItems SET Position = ? WHERE RowId = ? AND PlaylistId = ?",
                        position, rowIdsInOrder[position], playlistId);
                }
            });

            await TouchPlaylistAsync(playlistId);
        }

        private Task TouchPlaylistAsync(string playlistId)
        {
            return _connection.ExecuteAsync(
                "UPDATE Playlists SET DateModifiedTicks = ? WHERE Id = ?",
                DateTimeOffset.UtcNow.Ticks, playlistId);
        }
    }
}
