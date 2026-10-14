using SinaiPlayer.Lib.Dialogs;
using SinaiPlayer.Lib.Helpers;
using SinaiPlayer.Lib.Loaders;
using SinaiPlayer.Lib.Models;
using SinaiPlayer.Lib.Repos;
using SinaiPlayer.Lib.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Playlists;
using Windows.Services.Store;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SinaiPlayer.Lib.Utils
{
    public sealed partial class DialogUtils
    {
        // How long to wait before (re-)showing the feedback dialog.
        // TESTING: 1 minute, so you can verify the flow quickly.
        // PRODUCTION: change this to TimeSpan.FromDays(2) before release.
        private static readonly TimeSpan FeedbackDialogInterval = TimeSpan.FromDays(3);

        /// <summary>
        /// Call this from app startup / main page load. It schedules the first feedback
        /// prompt on first run, and shows the dialog once the interval has elapsed -
        /// as long as the user hasn't opted out via "Don't show this again".
        /// </summary>
        public static async Task CheckAndShowFeedbackDialogAsync()
        {
            // User opted out permanently - never show it again.
            if (PreferenceUtils.GetShowFeedbackDialog() == false || PreferenceUtils.GetShownIntroPage() == false)
                return;

            var nextShowDate = PreferenceUtils.GetFeedbackDialogNextShowDate();

            // First time ever this check runs: schedule the first prompt for
            // "FeedbackDialogInterval" from now, but don't show it yet.
            if (nextShowDate == null)
            {
                PreferenceUtils.SetFeedbackDialogNextShowDate(DateTimeOffset.Now.Add(FeedbackDialogInterval));
                return;
            }

            // Not due yet.
            if (DateTimeOffset.Now < nextShowDate.Value)
                return;

            await Show_FeedbackDialog();
        }

        public static async void Show_NewPlaylistDialog(List<MediaFile> files = null)
        {
            var dialogContent = new NewPlaylistDialogContent();
            ContentDialog dialog = new ContentDialog();

            dialog.Title = LocalizationHelper.GetString("CreatePlaylist");
            dialog.PrimaryButtonText = LocalizationHelper.GetString("Add");
            dialog.CloseButtonText = LocalizationHelper.GetString("Cancel");
            dialog.DefaultButton = ContentDialogButton.Primary;
            dialog.Content = dialogContent;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                string title = dialogContent.PlaylistName;
                var image = dialogContent.ImageContent;

                if (string.IsNullOrEmpty(title))
                    return;

                var playlist = await PlaylistRepository.CreatePlaylistAsync(title);

                if (files != null && files.Count > 0)
                {
                    await PlaylistRepository.AddFilesAsync(playlist, files);
                }
            }
        }

        public static async void Show_AddMediaFilesToPlaylistDialog(List<MediaFile> files)
        {
            var playlists = PlaylistRepository.Playlists.ToList();
            if (playlists.Count == 0)
            {
                Show_NewPlaylistDialog(files);
            }
            else
            {
                var dialogContent = new AddMediaFilesToPlaylistDailogContent();
                ContentDialog dialog = new ContentDialog();

                dialog.Title = LocalizationHelper.GetString("AddToPlaylist");
                dialog.PrimaryButtonText = LocalizationHelper.GetString("Done");
                dialog.CloseButtonText = LocalizationHelper.GetString("Cancel");
                dialog.DefaultButton = ContentDialogButton.Primary;
                dialog.Content = dialogContent;

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    var mySelections = dialogContent.SelectedItems;

                    if (mySelections.Count != 0)
                    {
                        foreach (var item in mySelections)
                        {
                            await PlaylistRepository.AddFilesAsync(item, files);
                        }
                    }

                }
            }
        }

        public static async void Show_SongInfoDialog(MediaFile song)
        {
            var dialogContent = new SongInfoDialogContent(song);
            ContentDialog dialog = new ContentDialog();

            dialog.Title = LocalizationHelper.GetString("SongInfo");
            dialog.CloseButtonText = LocalizationHelper.GetString("Close");
            dialog.Content = dialogContent;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {

            }
        }

        public static async void Show_VideoInfoDialog(MediaFile video)
        {
            var dialogContent = new VideoInfoDialogContent(video);
            ContentDialog dialog = new ContentDialog();

            dialog.Title = LocalizationHelper.GetString("VideoInfo");
            dialog.CloseButtonText = LocalizationHelper.GetString("Close");
            dialog.Content = dialogContent;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {

            }
        }

        public static async void Show_EditAlbumTagsDialog(Album album)
        {
            if (album == null)
                return;

            var dialogContent = new EditAlbumTagsDialogContent(album);

            ContentDialog dialog = new ContentDialog();


            dialog.Title = LocalizationHelper.GetString("AlbumInfoEditor");
            dialog.PrimaryButtonText = LocalizationHelper.GetString("Save");
            dialog.CloseButtonText = LocalizationHelper.GetString("Cancel");
            dialog.DefaultButton = ContentDialogButton.Primary;
            dialog.Content = dialogContent;

            var result = await dialog.ShowAsync();

            if (result != ContentDialogResult.Primary)
                return;

            if (!dialogContent.HasChanges)
                return;

            try
            {
                await TagEditor.SaveAlbumAsync(
                    dialogContent.Album,
                    dialogContent.AlbumName,
                    dialogContent.AlbumArtistName,
                    dialogContent.ReleaseYear,
                    dialogContent.SelectedArtworkBytes);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Save album tags failed: " + ex);
            }
        }

        public static async void Show_EditSongTagsDialog(MediaFile song)
        {
            if (song == null)
                return;

            var dialogContent = new EditSongTagsDialogContent(song);

            ContentDialog dialog = new ContentDialog();


            dialog.Title = LocalizationHelper.GetString("SongInfoEditor");
            dialog.PrimaryButtonText = LocalizationHelper.GetString("Save");
            dialog.CloseButtonText = LocalizationHelper.GetString("Cancel");
            dialog.DefaultButton = ContentDialogButton.Primary;
            dialog.Content = dialogContent;

            var result = await dialog.ShowAsync();

            if (result != ContentDialogResult.Primary)
                return;

            if (!dialogContent.HasChanges)
                return;

            try
            {
                await TagEditor.SaveSongAsync(dialogContent.Song, dialogContent.TrackNumberValue, dialogContent.TitleValue, dialogContent.ArtistValue, dialogContent.AlbumValue, dialogContent.AlbumArtistValue, dialogContent.YearValue, dialogContent.GenreValue);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Save song tags failed: " + ex);
            }
        }

        public static async void Show_EditPlaylistDialog(PlaylistViewModel vm)
        {
            var dialogContent = new NewPlaylistDialogContent();
            ContentDialog dialog = new ContentDialog();

            dialog.Title = LocalizationHelper.GetString("PlaylistInfoEditor");
            dialog.Content = dialogContent;
            dialog.PrimaryButtonText = LocalizationHelper.GetString("Save");
            dialog.CloseButtonText = LocalizationHelper.GetString("Cancel");
            dialog.DefaultButton = ContentDialogButton.Primary;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                string title = dialogContent.PlaylistName;
                var image = dialogContent.ImageContent;

                if (vm == null)
                    return;

                if (string.IsNullOrEmpty(title) || title == vm.Title)
                    return;

                await PlaylistRepository.RenamePlaylistAsync(vm, title);
            }
        }

        public static async void Show_DeletePlaylistDialog(PlaylistViewModel vm)
        {
            ContentDialog dialog = new ContentDialog();

            dialog.Title = LocalizationHelper.GetString("DeletePlaylistDialogTitle");
            dialog.Content = LocalizationHelper.Format("DeletePlaylistDialogMessage", vm.Title);
            dialog.PrimaryButtonText = LocalizationHelper.GetString("Delete");
            dialog.CloseButtonText = LocalizationHelper.GetString("Cancel");
            dialog.DefaultButton = ContentDialogButton.Close;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                if (vm == null)
                    return;

                await PlaylistRepository.DeletePlaylistAsync(vm);
            }
        }

        public static async void Show_DeleteMediaFilesDialog(List<MediaFile> files, string type)
        {
            ContentDialog dialog = new ContentDialog();

            dialog.Title = LocalizationHelper.Format("Delete", LocalizationHelper.GetString(type));
            dialog.Content = GetDeleteDialogMessage(files, type);
            dialog.PrimaryButtonText = LocalizationHelper.GetString("Delete");
            dialog.CloseButtonText = LocalizationHelper.GetString("Cancel");
            dialog.DefaultButton = ContentDialogButton.Close;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                if (files == null || files.Count == 0)
                    return;

                await LibraryRepository.DeleteFilesAsync(files);
            }
        }

        public static async void Show_RemoveFileFromPlaylistDialog(PlaylistViewModel playlistVM, PlaylistItemViewModel itemVM)
        {
            ContentDialog dialog = new ContentDialog();

            dialog.Title = LocalizationHelper.Format("RemoveItemFromPlaylistDialogTitle", playlistVM.Title);
            dialog.Content = LocalizationHelper.Format("RemoveItemFromPlaylistDialogMessage", itemVM.Title);
            dialog.PrimaryButtonText = LocalizationHelper.GetString("Remove");
            dialog.CloseButtonText = LocalizationHelper.GetString("Cancel");
            dialog.DefaultButton = ContentDialogButton.Close;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                if (playlistVM == null || itemVM == null)
                    return;

                await PlaylistRepository.RemoveItemAsync(playlistVM, itemVM);
            }
        }

        public static async void Show_RemoveFolderDialog(MediaFolder folder)
        {
            ContentDialog dialog = new ContentDialog();

            dialog.Title = LocalizationHelper.GetString("RemoveFolderDialogTitle");
            dialog.Content = LocalizationHelper.Format("RemoveFolderDialogMessage", folder.Path);
            dialog.PrimaryButtonText = LocalizationHelper.GetString("Remove");
            dialog.CloseButtonText = LocalizationHelper.GetString("Cancel");
            dialog.DefaultButton = ContentDialogButton.Close;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                if (folder == null)
                    return;

                await FolderManager.RemoveFolder(folder.Id);
            }

        }

        private static string GetDeleteDialogMessage(List<MediaFile> files, string type)
        {
            switch (type)
            {
                case "Song":
                    return files.Count == 1 ? LocalizationHelper.Format("DeleteDialogMessageSingular", LocalizationHelper.GetString("Song").ToLower(), files.FirstOrDefault().Title) : LocalizationHelper.Format("DeleteDialogMessagePlural", files.Count.ToString() + LocalizationHelper.GetString("Songs").ToLower());

                case "Album":
                    var albums = AlbumLoader.LoadAlbums(files);
                    return albums.Count == 1 ? LocalizationHelper.Format("DeleteDialogMessageSingular", LocalizationHelper.GetString("Album").ToLower(), albums.FirstOrDefault().Name) : LocalizationHelper.Format("DeleteDialogMessagePlural", albums.Count.ToString() + LocalizationHelper.GetString("Albums").ToLower());

                case "Artist":
                    var artists = ArtistLoader.LoadArtists(files);
                    return artists.Count == 1 ? LocalizationHelper.Format("DeleteDialogMessageSingular", LocalizationHelper.GetString("Artist").ToLower(), artists.FirstOrDefault().Name) : LocalizationHelper.Format("DeleteDialogMessagePlural", artists.Count.ToString() + LocalizationHelper.GetString("Artists").ToLower());

                case "Video":
                    return files.Count == 1 ? LocalizationHelper.Format("DeleteDialogMessageSingular", LocalizationHelper.GetString("Video").ToLower(), files.FirstOrDefault().Title) : LocalizationHelper.Format("DeleteDialogMessagePlural", files.Count.ToString() + LocalizationHelper.GetString("Videos").ToLower());

                default:
                    return LocalizationHelper.GetString("DeleteDialogMessageDefault");
            }
        }

        public static async void Show_CantFindDialog(string title)
        {
            ContentDialog dialog = new ContentDialog();

            dialog.Title = LocalizationHelper.GetString("ItemMissingDialogTitle");
            dialog.CloseButtonText = LocalizationHelper.GetString("Close");
            dialog.Content = LocalizationHelper.Format("ItemMissingDialogMessage", title);
            dialog.DefaultButton = ContentDialogButton.Primary;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {

            }
        }

        public static async void Show_DeveloperProfileDialog()
        {
            var dialogContent = new DeveloperProfileDialogContent();
            ContentDialog dialog = new ContentDialog();

            dialog.Title = LocalizationHelper.GetString("DeveloperProfileDialogTitle");
            dialog.CloseButtonText = LocalizationHelper.GetString("Close");
            dialog.Content = dialogContent;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {

            }
        }

        public static async Task Show_FeedbackDialog()
        {
            var dialogContent = new FeedbackPromptDialogContent();
            ContentDialog dialog = new ContentDialog();

            dialog.Title = LocalizationHelper.GetString("FeedbackPromptDialogTitle");
            dialog.Content = dialogContent;
            dialog.PrimaryButtonText = LocalizationHelper.GetString("Donate");
            dialog.SecondaryButtonText = LocalizationHelper.GetString("RateUs");
            dialog.CloseButtonText = LocalizationHelper.GetString("Later");

            var result = await dialog.ShowAsync();

            if (dialogContent.DontShowAgain)
            {
                // "Don't show this again" was checked - stop showing the dialog forever.
                PreferenceUtils.SetShowFeedbackDialog(false);
            }
            else
            {
                // Whatever button was pressed (Donate / Rate Us / Later), the user has
                // "seen" the prompt - push the next attempt out by the interval again.
                PreferenceUtils.SetFeedbackDialogNextShowDate(DateTimeOffset.Now.Add(FeedbackDialogInterval));
            }

            switch (result)
            {
                case ContentDialogResult.Primary:
                    var uri = new Uri("https://apps.rohithsinai.com/media-player/donate");
                    await Windows.System.Launcher.LaunchUriAsync(uri);
                    break;
                case ContentDialogResult.Secondary:
                    ShowStoreRatingDialog();
                    break;
            }
        }

        public static async void ShowStoreRatingDialog()
        {
            // Get the StoreContext instance for the current user
            StoreContext storeContext = StoreContext.GetDefault();

            // Request the native rating and review dialog
            StoreRateAndReviewResult result = await storeContext.RequestRateAndReviewAppAsync();

            // Check the user response
            switch (result.Status)
            {
                case StoreRateAndReviewStatus.Succeeded:
                    // User submitted a rating or review
                    break;

                case StoreRateAndReviewStatus.CanceledByUser:
                    // User dismissed the dialog without rating
                    break;

                case StoreRateAndReviewStatus.NetworkError:
                    // Failed due to network connection
                    break;

                case StoreRateAndReviewStatus.Error:
                default:
                    // Failed due to an unknown error or app is running locally (unpublished)
                    break;
            }
        }
    }
}