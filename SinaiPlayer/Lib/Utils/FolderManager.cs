using Windows.UI.Xaml; 
using Newtonsoft.Json;
using SinaiPlayer.Lib.Models;
using SinaiPlayer.Lib.Repos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache; 
using Windows.Storage.Pickers; 

namespace SinaiPlayer.Lib.Utils
{
    public static class FolderManager
    {
        private const string Key = "MediaFolders";

        public static readonly string[] AudioExtensions =
        {
            ".mp3", ".m4a", ".aac", ".wav", ".flac", ".wma"
        };

        public static readonly string[] VideoExtensions =
        {
            ".mp4", ".mkv", ".avi", ".mov", ".wmv"
        };

        public static List<MediaFolder> GetFolders()
        {
            var json = ApplicationData.Current .LocalSettings.Values[Key] as string;

            if (string.IsNullOrEmpty(json))
            {
                return new List<MediaFolder>();
            }

            return JsonConvert.DeserializeObject<List<MediaFolder>>(json)
                   ?? new List<MediaFolder>();
        }

        // Just persists the list now - no longer touches the library.
        // Each caller below decides exactly what library work is actually needed.
        private static void PersistFolders(List<MediaFolder> folders)
        {
            ApplicationData.Current.LocalSettings.Values[Key] = 
                JsonConvert.SerializeObject(folders);
        }

        private static async Task AddFolder(string token, string title, string path)
        {
            var folders = GetFolders();

            if (folders.Exists(x => x.Path == path))
            {
                return;
            }

            var newFolder = new MediaFolder
            {
                Id = Guid.NewGuid().ToString(),

                Token = token,

                Title = title,

                Path = path,

                DateAdded = DateTimeOffset.Now
            };

            folders.Add(newFolder);
            PersistFolders(folders);

            // Only the new folder needs scanning - every other folder is already
            // cached, so there's no reason to re-diff the whole library here.
            await LibraryRepository.SyncFolderAsync(newFolder);
        }

        public static async Task RemoveFolder(string id)
        {
            var folders = GetFolders();

            folders.RemoveAll(x => x.Id == id);

            PersistFolders(folders);

            // No disk access needed - just drop this folder's cached rows and
            // pull its songs/videos out of the UI collections.
            await LibraryRepository.RemoveFolderAsync(id);
        }

        public static async Task RemoveFolders(IEnumerable<string> ids)
        {
            var idList = ids.ToList();

            var folders = GetFolders();

            folders.RemoveAll(x => idList.Contains(x.Id));

            PersistFolders(folders);

            foreach (var id in idList)
            {
                await LibraryRepository.RemoveFolderAsync(id);
            }
        }

        public static async void OpenFolderPicker(object sender, RoutedEventArgs e)
        {
            var folderPicker = new FolderPicker(); 

            folderPicker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
            foreach (var item in AudioExtensions)
            {
                folderPicker.FileTypeFilter.Add(item);
            }
            foreach (var item in VideoExtensions)
            {
                folderPicker.FileTypeFilter.Add(item);
            }
             

            var folder = await folderPicker.PickSingleFolderAsync();


            if (folder == null)
            {
                return;
            }

            string token = StorageApplicationPermissions.FutureAccessList.Add(folder);

            await AddFolder(token, folder.DisplayName, folder.Path);
        } 
    }
}
