using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using SinaiPlayer.Lib.Helpers;
using SinaiPlayer.Lib.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SinaiPlayer.Lib.Dialogs
{
    public sealed partial class VideoInfoDialogContent : UserControl
    {
        private MediaFile Video;

        public VideoInfoDialogContent(MediaFile video)
        {
            InitializeComponent();
            TitleTextBlock.Text = LocalizationHelper.GetString("Title");
            GenresTextBlock.Text = LocalizationHelper.GetString("Genre");
            ReleaseYearTextBlock.Text = LocalizationHelper.GetString("ReleaseYear");
            FormatTextBlock.Text = LocalizationHelper.GetString("Format");
            BitrateTextBlock.Text = LocalizationHelper.GetString("BitRate");
            DurationTextBlock.Text = LocalizationHelper.GetString("Duration");
            FileSizeTextBlock.Text = LocalizationHelper.GetString("FileSize");
            FileLocationTextBlock.Text = LocalizationHelper.GetString("FileLocation");
            ShowInFolderButton.Content = LocalizationHelper.GetString("ShowInFolder");

            this.Video = video;

            Title.Text = video.Title;  
            Genre.Text = video.Genres.Count != 0 ? string.Join(",", video.Genres) : "Unknown genre";
            Year.Text = video.Year.ToString(); 
            Duration.Text = FormatHelper.FormatTimeToDisplayMillis(video.Duration);
            Type.Text = video.Extension.Replace(".", "");
            Bitrate.Text = $"{video.Bitrate / 1000:N0} kbps";
            Location.Text = video.Path;
            Size.Text = FormatHelper.FormatFileSize(video.Size);
        }

        private async void ShowInFolder(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Video?.Path))
                    return;

                if (!System.IO.File.Exists(Video.Path))
                    return;

                string directoryPath = System.IO.Path.GetDirectoryName(Video.Path);

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
