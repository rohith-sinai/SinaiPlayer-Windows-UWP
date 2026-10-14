using SinaiPlayer.Lib.Models; 
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SinaiPlayer.Lib.Data
{
    public sealed class MediaDatabase
    {
        private SQLiteAsyncConnection _connection;

        private const int UpsertBatchSize = 500;
        private const int DeleteBatchSize = 500;

        public async Task InitializeAsync()
        {

            string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "library.db");

            _connection = new SQLiteAsyncConnection(dbPath);

            // WAL = far better write throughput for the batched upserts below.
            // NOTE: "PRAGMA journal_mode=WAL" returns the resulting mode as a result
            // row (not a plain non-query), so it must be read via ExecuteScalarAsync -
            // calling ExecuteAsync on it throws SQLiteException("Row").
            await _connection.ExecuteScalarAsync<string>("PRAGMA journal_mode=WAL;");

            await _connection.CreateTableAsync<MediaRecord>();
        }

        public Task<List<MediaRecord>> GetAllAsync()
        {
            return _connection.Table<MediaRecord>().ToListAsync();
        }

        // Cheap: only pulls Path + timestamp for one folder, used to diff
        // against a fresh disk listing without touching any other columns.
        public async Task<Dictionary<string, long>> GetSnapshotForFolderAsync(string folderId)
        {
            var rows = await _connection.QueryAsync<PathTicks>(
                "SELECT Path, FileDateModifiedTicks FROM MediaFiles WHERE FolderId = ?",
                folderId);

            return rows.ToDictionary(r => r.Path, r => r.FileDateModifiedTicks);
        }

        public async Task UpsertBatchAsync(IEnumerable<MediaRecord> records)
        {
            var list = records as IList<MediaRecord> ?? records.ToList();

            for (int i = 0; i < list.Count; i += UpsertBatchSize)
            {
                var batch = list.Skip(i).Take(UpsertBatchSize).ToList();

                await _connection.RunInTransactionAsync(conn =>
                {
                    foreach (var record in batch)
                    {
                        conn.InsertOrReplace(record);
                    }
                });
            }
        }

        public async Task DeleteByPathsAsync(IEnumerable<string> paths)
        {
            var list = paths as IList<string> ?? paths.ToList();

            for (int i = 0; i < list.Count; i += DeleteBatchSize)
            {
                var batch = list.Skip(i).Take(DeleteBatchSize).ToList();

                await _connection.RunInTransactionAsync(conn =>
                {
                    foreach (var path in batch)
                    {
                        conn.Execute("DELETE FROM MediaFiles WHERE Path = ?", path);
                    }
                });
            }
        }

        public Task DeleteByFolderIdAsync(string folderId)
        {
            return _connection.ExecuteAsync("DELETE FROM MediaFiles WHERE FolderId = ?", folderId);
        }

        public Task<MediaRecord> GetByPathAsync(string path)
        {
            return _connection
                .Table<MediaRecord>()
                .Where(x => x.Path == path)
                .FirstOrDefaultAsync();
        }

        public Task UpsertAsync(MediaRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            return _connection.InsertOrReplaceAsync(record);
        }

        private class PathTicks
        {
            public string Path { get; set; }
            public long FileDateModifiedTicks { get; set; }
        }
    }
}
