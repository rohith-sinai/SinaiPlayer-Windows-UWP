using CommunityToolkit.WinUI.Controls;
using SinaiPlayer.Lib.Helpers;
using SinaiPlayer.Lib.Models;
using SinaiPlayer.Lib.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SinaiPlayer.Lib.UI.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        private bool IsFetchMetadataEnabled = PreferenceUtils.GetIsEnabledFetchMetadata();
        private bool IsNowPlayingSlideshowEnabled = PreferenceUtils.GetIsEnabledNowPlayingSlideshow();

        public SettingsPage()
        {
            InitializeComponent();

            LargeTitle.Text = LocalizationHelper.GetString("Settings");

            GeneralTextBlock.Text = LocalizationHelper.GetString("General");
            SettingsExpanded_MediaFolders.Header = LocalizationHelper.GetString("SettingsExpandedMediaFoldersHeader");
            SettingsExpanded_MediaFolders.Description = LocalizationHelper.GetString("SettingsExpandedMediaFoldersDescription");
            AddFolderButton.Content = LocalizationHelper.GetString("AddFolder");
            SettingsCard_ThemeMode.Header = LocalizationHelper.GetString("SettingsCardThemeModeHeader");
            ThemeModeComboBoxItem_System.Content = LocalizationHelper.GetString("System");
            ThemeModeComboBoxItem_Light.Content = LocalizationHelper.GetString("Light");
            ThemeModeComboBoxItem_Dark.Content = LocalizationHelper.GetString("Dark");

            MetadataTextBlock.Text = LocalizationHelper.GetString("Metadata");
            SettingsCard_FetchMetadata.Header = LocalizationHelper.GetString("SettingsCardFetchMetadataHeader");

            NowPlayingTextBlock.Text = LocalizationHelper.GetString("NowPlaying");
            SettingsCard_ArtistSlideshow.Header = LocalizationHelper.GetString("SettingsCardArtistSlideshowHeader");

            AboutTextBlock.Text = LocalizationHelper.GetString("About");
            SettingsExpander_About.Header = LocalizationHelper.GetString("AppName");
            SettingsExpander_About.Description = LocalizationHelper.GetString("SettingsExpanderAboutDescription");
            PrivacyPolicyHyperlinkButton.Content = LocalizationHelper.GetString("PrivacyPolicy");
            OpenSourceLicensesHyperlinkButton.Content = LocalizationHelper.GetString("OpenSourceLicenses");
            DeveloperProfileHyperlinkButton.Content = LocalizationHelper.GetString("DeveloperProfile");
            DonateHyperlinkButton.Content = LocalizationHelper.GetString("Donate");
            SendFeedbackHyperlinkButton.Content = LocalizationHelper.GetString("SendFeedback");

            LoadContent();
        }

        private void LoadContent()
        {
            LoadThemeModeComboBox();
            FetchMetadataToggleButton.IsOn = IsFetchMetadataEnabled;
            NowPlayingSlideshowToggleButton.IsOn = IsNowPlayingSlideshowEnabled;
            SettingsExpanded_MediaFolders.ItemsSource = FolderManager.GetFolders();
            VersionTextBlock.Text = GetAppVersionString();
        }



        private void LoadThemeModeComboBox()
        {
            switch (PreferenceUtils.GetTheme())
            {
                case "light":
                    ThemeModeComboBox.SelectedIndex = 1;
                    break;
                case "dark":
                    ThemeModeComboBox.SelectedIndex = 2;
                    break;
                default:
                case "auto":
                    ThemeModeComboBox.SelectedIndex = 0;
                    break;
            }
        }

        private void ThemeMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var root = (Frame)Window.Current.Content;

            switch (ThemeModeComboBox.SelectedIndex)
            {
                case 1:
                    root.RequestedTheme = ElementTheme.Light;
                    PreferenceUtils.SetTheme("light");
                    break;
                case 2:
                    root.RequestedTheme = ElementTheme.Dark;
                    PreferenceUtils.SetTheme("dark");
                    break;
                default:
                case 0:
                    root.RequestedTheme = ElementTheme.Default;
                    PreferenceUtils.SetTheme("auto");
                    break;
            }
        }

        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            FolderManager.OpenFolderPicker(sender, e);
        }

        private void RemoveFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                if (button.Tag is MediaFolder folder)
                {
                    DialogUtils.Show_RemoveFolderDialog(folder);
                }
            }
        }

        private async void RescanFolders_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Toggle_FetchMetadata(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggleSwitch)
            {
                PreferenceUtils.SetFetchMetadataEnabled(toggleSwitch.IsOn ? true : false);
            }
        }

        private void Toggle_NowPlayingAnimatedArtistImages(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggleSwitch)
            {
                PreferenceUtils.SetNowPlayingSlideshowEnabled(toggleSwitch.IsOn ? true : false);
            }
        }

        private async void OpenSourceLicensesButton_Click(object sender, RoutedEventArgs e)
        {
            var uri = new Uri("https://apps.rohithsinai.com/media-player/licenses");
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }

        private async void PrivacyPolicyButton_Click(object sender, RoutedEventArgs e)
        {
            var uri = new Uri("https://apps.rohithsinai.com/media-player/privacy-policy");
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }

        private async void DeveloperProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var uri = new Uri("https://www.rohithsinai.com");
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }

        private async void DonateButton_Click(object sender, RoutedEventArgs e)
        {
            var uri = new Uri("https://apps.rohithsinai.com/media-player/donate");
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }

        private void SendFeedbackButton_Click(object sender, RoutedEventArgs e)
        {
            DialogUtils.ShowStoreRatingDialog();
        }

        public string GetAppVersionString()
        {
            var packageId = Package.Current.Id;
            PackageVersion version = packageId.Version;

            return LocalizationHelper.GetString("Version") + $" {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }
    }
}