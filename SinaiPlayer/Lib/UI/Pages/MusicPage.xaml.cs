using SinaiPlayer.Lib.Enums;
using SinaiPlayer.Lib.Helpers;
using SinaiPlayer.Lib.Repos;
using SinaiPlayer.Lib.Services;
using SinaiPlayer.Lib.Utils;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.AccessControl;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using wct = CommunityToolkit.WinUI.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SinaiPlayer.Lib.UI.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MusicPage : Page
    {
        private MediaPlayerService Player => MediaPlayerService.Instance;

        private int previousSelectedIndex = 0;
        private int currentTabSelectedIndex = 0;
        private bool _suppressSortSelectionChanged;

        public MusicPage()
        {
            InitializeComponent();

            EmptyView_Title.Text = LocalizationHelper.GetString("EmptyViewTitle");
            EmptyView_SubTitle.Text = LocalizationHelper.Format("EmptyViewSubTitle", LocalizationHelper.GetString("Music").ToLower());
            ChooseFoldersButton.Content = LocalizationHelper.GetString("ImportFolders");

            LargeTitle.Text = LocalizationHelper.GetString("Music");
            SongsTab.Content = LocalizationHelper.GetString("Songs");
            ArtistsTab.Content = LocalizationHelper.GetString("Artists");
            AlbumsTab.Content = LocalizationHelper.GetString("Albums");
            SortByTextBlock.Text = LocalizationHelper.GetString("SortBy") + ":";
            SortComboBox.PlaceholderText = LocalizationHelper.GetString("SelectOrder");

            Loaded += MusicPage_Loaded;
            LibraryRepository.SongItems.CollectionChanged += SongItems_CollectionChanged;
        }

        private void SongItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            InitContent();
            RefreshCurrentTabContent();
        }

        private void MusicPage_Loaded(object sender, RoutedEventArgs e)
        {
            int index = PreferenceUtils.GetLastMusicTab();
            currentTabSelectedIndex = index;
            TabBar.SelectedIndex = index;
            InitContent();
            RefreshCurrentTabContent();
        }

        private void InitContent()
        {
            ContentView.Visibility = LibraryRepository.Songs.Count != 0 ?
              Visibility.Visible : Visibility.Collapsed;

            EmptyView.Visibility = LibraryRepository.Songs.Count == 0 ?
                Visibility.Visible : Visibility.Collapsed;
        }

        private void TabBar_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is wct.Segmented segmented && segmented.SelectedItem is wct.SegmentedItem selectedItem)
            {
                int currentSelectedIndex = segmented.SelectedIndex;

                currentTabSelectedIndex = currentSelectedIndex;
                PreferenceUtils.SetLastMusicTab(currentTabSelectedIndex);

                SetPageFromIndex(currentSelectedIndex);

                previousSelectedIndex = currentSelectedIndex;

                PopulateSortOptionsForTab();
                ChangeShuffleButton_UI();
            }

        }

        private void RefreshCurrentTabContent()
        {
            switch (currentTabSelectedIndex)
            {
                case 0:
                    (ContentFrame.Content as SongsPage)?.ApplySortOrder(PreferenceUtils.GetSongsSortOrder());
                    break;

                case 1:
                    (ContentFrame.Content as ArtistsPage)?.Init_GridView();
                    break;

                case 2:
                    (ContentFrame.Content as AlbumsPage)?.ApplySortOrder(PreferenceUtils.GetAlbumsSortOrder());
                    break;
            }
        }

        private void SetPageFromIndex(int currentSelectedIndex)
        {
            System.Type pageType;

            switch (currentSelectedIndex)
            {
                case 0:
                    pageType = typeof(SongsPage);
                    SongsTab.Opacity = 1.0;
                    ArtistsTab.Opacity = 0.5;
                    AlbumsTab.Opacity = 0.5;
                    break;
                case 1:
                    pageType = typeof(ArtistsPage);
                    SongsTab.Opacity = 0.5;
                    ArtistsTab.Opacity = 1.0;
                    AlbumsTab.Opacity = 0.5;
                    break;
                case 2:
                    pageType = typeof(AlbumsPage);
                    SongsTab.Opacity = 0.5;
                    ArtistsTab.Opacity = 0.5;
                    AlbumsTab.Opacity = 1.0;
                    break;
                default:
                    pageType = typeof(SongsPage);
                    SongsTab.Opacity = 1.0;
                    ArtistsTab.Opacity = 0.5;
                    AlbumsTab.Opacity = 0.5;
                    break;
            }

            ContentFrame.Navigate(pageType, null, new EntranceNavigationTransitionInfo());
        }

        private void PopulateSortOptionsForTab()
        {
            _suppressSortSelectionChanged = true;

            List<SortOptionItem> options;
            SortOption current;

            switch (currentTabSelectedIndex)
            {
                case 1: // Artists
                    options = new List<SortOptionItem>
                    {
                        new SortOptionItem(SortOption.Title, LocalizationHelper.GetString("Title")),
                    };
                    current = SortOption.Title;
                    SortComboBox.IsEnabled = true;
                    break;

                case 2: // Albums
                    options = new List<SortOptionItem>
                    {
                        new SortOptionItem(SortOption.DateAdded, LocalizationHelper.GetString("DateAdded")),
                        new SortOptionItem(SortOption.Title, LocalizationHelper.GetString("Title")),
                        new SortOptionItem(SortOption.Year, LocalizationHelper.GetString("ReleaseYear")),
                        new SortOptionItem(SortOption.Artist, LocalizationHelper.GetString("Artist")),
                    };
                    current = PreferenceUtils.GetAlbumsSortOrder();
                    SortComboBox.IsEnabled = true;
                    break;

                case 0: // Songs
                default:
                    options = new List<SortOptionItem>
                    {
                         new SortOptionItem(SortOption.DateAdded, LocalizationHelper.GetString("DateAdded")),
                         new SortOptionItem(SortOption.Title, LocalizationHelper.GetString("Title")),
                         new SortOptionItem(SortOption.Artist, LocalizationHelper.GetString("Artist")),
                         new SortOptionItem(SortOption.Artist, LocalizationHelper.GetString("Album")),
                    };
                    current = PreferenceUtils.GetSongsSortOrder();
                    SortComboBox.IsEnabled = true;
                    break;
            }

            SortComboBox.ItemsSource = options;

            int index = options.FindIndex(o => o.Value == current);
            SortComboBox.SelectedIndex = index >= 0 ? index : 0;

            _suppressSortSelectionChanged = false;
        }

        // Hook this up to SortComboBox's SelectionChanged in XAML.
        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressSortSelectionChanged)
                return;

            if (!(SortComboBox.SelectedItem is SortOptionItem selected))
                return;

            switch (currentTabSelectedIndex)
            {
                case 0:
                    PreferenceUtils.SetSongsSortOrder(selected.Value);
                    (ContentFrame.Content as SongsPage)?.ApplySortOrder(selected.Value);
                    break;

                case 1:
                    break;

                case 2:
                    PreferenceUtils.SetAlbumsSortOrder(selected.Value);
                    (ContentFrame.Content as AlbumsPage)?.ApplySortOrder(selected.Value);
                    break;
            }
        }

        private void ChangeShuffleButton_UI()
        {
            switch (currentTabSelectedIndex)
            {
                case 0:
                    var songs = LibraryRepository.SongItems;
                    ShuffleButtonTextBlock.Text = LocalizationHelper.GetString("ShuffleAll") + $" ({songs.Count})";
                    break;
                case 1:
                    var artists = LibraryRepository.ArtistItems;
                    ShuffleButtonTextBlock.Text = LocalizationHelper.GetString("ShuffleAll") + $" ({artists.Count})";
                    break;
                case 2:
                    var albums = LibraryRepository.AlbumItems;
                    ShuffleButtonTextBlock.Text = LocalizationHelper.GetString("ShuffleAll") + $" ({albums.Count})";
                    break;
            }
        }

        private async void ShuffleAll_Button_Click(object sender, RoutedEventArgs e)
        {
            var songs = LibraryRepository.Songs;
            await Player.SetQueueAsync(songs, 0, false);
            Player.SetShuffle(true);
            await Player.NextAsync();
        }

        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            FolderManager.OpenFolderPicker(sender, e);
        }
    }
}
