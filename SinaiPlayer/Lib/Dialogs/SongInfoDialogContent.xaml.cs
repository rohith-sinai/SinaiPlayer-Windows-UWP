using SinaiPlayer.Lib.Helpers;
using SinaiPlayer.Lib.Models;
using System;
using System.Diagnostics; 
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls; 

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SinaiPlayer.Lib.Dialogs
{
    public sealed partial class SongInfoDialogContent : UserControl
    {

        private MediaFile Song;

        public SongInfoDialogContent(MediaFile song)
        {
            InitializeComponent();
            TitleTextBlock.Text = LocalizationHelper.GetString("Title");
            SongArtistTextBlock.Text = LocalizationHelper.GetString("SongArtist");
            AlbumTextBlock.Text = LocalizationHelper.GetString("Album");
            SongNumberTextBlock.Text = LocalizationHelper.GetString("SongNumber");
            AlbumArtistTextBlock.Text = LocalizationHelper.GetString("AlbumArtist");
            ComposersTextBlock.Text = LocalizationHelper.GetString("Composers");
            GenresTextBlock.Text = LocalizationHelper.GetString("Genre");
            ReleaseYearTextBlock.Text = LocalizationHelper.GetString("ReleaseYear");
            FormatTextBlock.Text = LocalizationHelper.GetString("Format");
            BitrateTextBlock.Text = LocalizationHelper.GetString("BitRate");
            DurationTextBlock.Text = LocalizationHelper.GetString("Duration");
            FileSizeTextBlock.Text = LocalizationHelper.GetString("FileSize");
            FileLocationTextBlock.Text = LocalizationHelper.GetString("FileLocation");
            ShowInFolderButton.Content = LocalizationHelper.GetString("ShowInFolder");

            this.Song = song;
            
            Title.Text = song.Title;
            Artist.Text = song.Artist;
            Album.Text = song.Album;
            AlbumArtist.Text = song.AlbumArtist;
            Genre.Text = song.Genres.Count != 0 ? string.Join(",", song.Genres) : "Unknown genre";
            Year.Text = song.Year.ToString();
            Composers.Text = song.Composers.Count != 0 ? string.Join(",", song.Composers) : "Unknown composers";
            SongNumber.Text = song.TrackNumber.ToString();
            Duration.Text = FormatHelper.FormatTimeToDisplayMillis(song.Duration);
            Type.Text = song.Extension.Replace(".", "");
            Bitrate.Text = $"{song.Bitrate / 1000:N0} kbps";
            Location.Text = song.Path;
            Size.Text = FormatHelper.FormatFileSize(song.Size);
        }

        private async void ShowInFolder(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Song?.Path))
                    return;

                if (!System.IO.File.Exists(Song.Path))
                    return;

                string directoryPath = System.IO.Path.GetDirectoryName(Song.Path);

                if (string.IsNullOrWhiteSpace(directoryPath))
                    return;

                var folder =
                    await Windows.Storage.StorageFolder.GetFolderFromPathAsync(directoryPath);

                await Windows.System.Launcher.LaunchFolderAsync(folder);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"File opening in explorer failed: {ex}");
            }
        }
         
    }
}
