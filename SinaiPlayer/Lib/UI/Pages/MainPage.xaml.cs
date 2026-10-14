using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using SinaiPlayer.Lib.Enums;
using SinaiPlayer.Lib.Helpers;
using SinaiPlayer.Lib.Models;
using SinaiPlayer.Lib.Repos;
using SinaiPlayer.Lib.Services;
using SinaiPlayer.Lib.UI.Pages.Player;
using SinaiPlayer.Lib.UI.ViewModels;
using SinaiPlayer.Lib.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using muxc = Microsoft.UI.Xaml.Controls;
using Windows.UI.Core;
using Windows.ApplicationModel.Core;
using System.Collections.Specialized;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SinaiPlayer.Lib.UI.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public static MainPage Current { get; private set; }
        public Frame ContentFrame => frame;

        private MediaPlayerService Player => MediaPlayerService.Instance;

        private MiniPlayerViewModel _miniPlayerViewModel = new MiniPlayerViewModel();

        public ObservableCollection<SearchSuggestionViewModel> SearchSuggestions { get; } = new ObservableCollection<SearchSuggestionViewModel>();
        public MainPage()
        {
            InitializeComponent();
            Current = this;
            AppNameTextBlock.Text = LocalizationHelper.GetString("AppName");
            SearchBox.PlaceholderText = LocalizationHelper.GetString("Search");
            NavItemMusic.Content = LocalizationHelper.GetString("Music");
            NavItemVideos.Content = LocalizationHelper.GetString("Videos");
            NavItemPlayer.Content = LocalizationHelper.GetString("NowPlaying");
            NavItemPlaylists.Content = LocalizationHelper.GetString("Playlists");
            NavItemSettings.Content = LocalizationHelper.GetString("Settings");
            

            DrawUnderStatusBar();

            Window.Current.SizeChanged += Current_SizeChanged;
            _miniPlayerViewModel.PropertyChanged += MiniPlayerViewModelPropertyChanged;
            SearchBox.ItemsSource = SearchSuggestions;

            LibraryRepository.MediaFiles.CollectionChanged += MediaFiles_CollectionChanged;
        }

        private void MediaFiles_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdatingInfoNavItem.Visibility = Visibility.Visible;
            UpdateInfo_TextBlock.Text = LocalizationHelper.GetString("CheckingForNewItems");
            // The e.Action property determines what type of change happened
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    // Handle new items added to the collection
                    foreach (var newItem in e.NewItems)
                    {
                        // Your logic here (e.g., UI updates, subscribing to item events)
                        UpdateInfo_TextBlock.Text = LocalizationHelper.Format("AddingItems", e.NewStartingIndex.ToString(), e.NewItems.Count.ToString());
                    }
                    UpdatingInfoNavItem.Visibility = Visibility.Collapsed;
                    break;

                case NotifyCollectionChangedAction.Remove:
                    // Handle items removed from the collection
                    foreach (var oldItem in e.OldItems)
                    {
                        // Your logic here (e.g., cleanup, unsubscribing from events)
                        UpdateInfo_TextBlock.Text = LocalizationHelper.Format("RemovingItems", e.OldStartingIndex.ToString(), e.OldItems.Count.ToString());
                    }
                    UpdatingInfoNavItem.Visibility = Visibility.Collapsed;
                    break;

                case NotifyCollectionChangedAction.Replace:
                    // Handle an item being swapped out for another
                    // e.OldItems contains the old data, e.NewItems contains the new data
                    break;

                case NotifyCollectionChangedAction.Move:
                    // Handle an item changing its index position in the list
                    // Use e.OldStartingIndex and e.NewStartingIndex
                    break;

                case NotifyCollectionChangedAction.Reset:
                    // The collection was cleared or completely reloaded
                    // e.NewItems and e.OldItems will be null here
                    break;
            }
        }


        // Set initial page on load
        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            
            var tag = PreferenceUtils.GetLastNavTab();

            switch (tag)
            {
                case "Videos":
                    NavView.SelectedItem = NavView.MenuItems[1];
                    ContentFrame.Navigate(typeof(VideosPage));
                    break;
                     
                case "Playlists":
                    NavView.SelectedItem = NavView.MenuItems[3];
                    ContentFrame.Navigate(typeof(PlaylistsPage));
                    break;

                case "Music":
                default:
                    NavView.SelectedItem = NavView.MenuItems[0];
                    ContentFrame.Navigate(typeof(MusicPage));
                    break;

            }

            UpdateUI();
        }

        // Handle item selection
        private void NavView_ItemInvoked(muxc.NavigationView sender, muxc.NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                ContentFrame.Navigate(typeof(SettingsPage));
                return;
            }

            else if (args.InvokedItemContainer != null)
            {
                string tag = args.InvokedItemContainer.Tag?.ToString();

                if (string.IsNullOrEmpty(tag))
                    return;

                switch (tag)
                {
                    case "Music":
                        PreferenceUtils.SetLastNavTab(tag);
                        ContentFrame.Navigate(typeof(MusicPage));
                        break;

                    case "Videos":
                        PreferenceUtils.SetLastNavTab(tag);
                        ContentFrame.Navigate(typeof(VideosPage));
                        break;

                    case "Player":
                        NavigationUtils.Navigate_NowPlayingPage();
                        break;

                    case "Playlists":
                        PreferenceUtils.SetLastNavTab(tag);
                        ContentFrame.Navigate(typeof(PlaylistsPage));
                        break;

                    case "Settings":
                        ContentFrame.Navigate(typeof(SettingsPage));
                        break;
                }
            }
        }

        private void Settings_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
        }

        // Handle Back Button Click
        private void NavView_BackRequested(muxc.NavigationView sender, muxc.NavigationViewBackRequestedEventArgs args)
        {
            if (ContentFrame.CanGoBack)
            {
                ContentFrame.GoBack();
            }
        }

        // Update Back Button visibility based on history
        private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            NavView.IsBackEnabled = ContentFrame.CanGoBack;
        }

        private void MiniPlayerViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_miniPlayerViewModel.MiniPlayerHeight))
            {
                if (!_miniPlayerViewModel.IsMiniPlayerVisible)
                {
                   
                }
            }
        }

        private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            UpdateUI();
        }


        private void UpdateUI()
        {
            bool smallScreen = ScreenHelper.IsMobile;

            if (smallScreen)
            {
                BottomSpacerInNavBar.Height = 0;
                MiniPlayer_Mobile.Visibility = Visibility.Visible;
                MiniPlayer_Tablet.Visibility = Visibility.Collapsed;
            }
            else
            {
                BottomSpacerInNavBar.Height = _miniPlayerViewModel.MiniPlayerHeight;
                MiniPlayer_Mobile.Visibility = Visibility.Collapsed;
                MiniPlayer_Tablet.Visibility = Visibility.Visible;

            }
        }

        // Handle Search Box Submission

        private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            string query = args.QueryText;

            sender.IsSuggestionListOpen = false;

            if (args.ChosenSuggestion is SearchSuggestionViewModel item)
            {
                OnSelectedSearchSuggestionItem(item);
            }
            else
            {
                NavigationUtils.Navigate_SearchPage(query);
            }

        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                UpdateSearchSuggestions(sender.Text);
            }
        }

        private void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            sender.IsSuggestionListOpen = false;
            if (args.SelectedItem is SearchSuggestionViewModel item)
            {
                sender.Text = string.Empty;
                //OnSelectedSearchSuggestionItem(item);
            }
        }

        private async void OnSelectedSearchSuggestionItem(SearchSuggestionViewModel item)
        {
            switch (item.Type)
            {
                case SearchSuggestionType.Song:
                    var songs = LibraryRepository.Songs.ToList();
                    var song = songs.Find(x => x.Id == item.Id);
                    List<MediaFile> songsqueue = new List<MediaFile> { song };
                    if (song != null) await Player.SetQueueAsync(songsqueue);
                    break;
                case SearchSuggestionType.Video:
                    var videos = LibraryRepository.Videos.ToList();
                    var video = videos.Find(x => x.Id == item.Id);
                    List<MediaFile> videosqueue = new List<MediaFile> { video };
                    if (video != null) await Player.SetQueueAsync(videosqueue);
                    break;
                case SearchSuggestionType.Artist:
                    NavigationUtils.Navigate_ArtistDetailsPage(item.Id);
                    break;
                case SearchSuggestionType.Album:
                    NavigationUtils.Navigate_AlbumDetailsPage(item.Id);
                    break;
                case SearchSuggestionType.Playlist:
                    NavigationUtils.Navigate_PlaylistDetailsPage(item.Id);
                    break;
            }
        }

        private void UpdateSearchSuggestions(string query)
        {
            SearchSuggestions.Clear();

            if (string.IsNullOrWhiteSpace(query))
            {
                SearchBox.IsSuggestionListOpen = false;
                return;
            }

            query = query.Trim();

            /*
             * Keep the suggestion list reasonably small.
             * This also prevents the NavigationView search box from becoming
             * overloaded with hundreds/thousands of songs.
             */
            const int maxSuggestions = 12;

            /*
             * ---------------------------------------------------------
             * SONGS
             * ---------------------------------------------------------
             */
            if (SearchSuggestions.Count < maxSuggestions)
            {
                foreach (var song in LibraryRepository.SongItems)
                {
                    if (song == null)
                        continue;

                    if (!Matches(song.Title.Trim(), query))
                        continue;

                    var item = new SearchSuggestionItem
                    {
                        Id = song.Id,
                        Path = song.Path,
                        Title = song.Title,
                        Artist = song.Artist,
                        ArtistId = song.ArtistId,
                        Album = song.Album,
                        AlbumId = song.AlbumId,
                        Type = SearchSuggestionType.Song
                    };

                    var vm = new SearchSuggestionViewModel(item);
                    SearchSuggestions.Add(vm);

                    if (SearchSuggestions.Count >= maxSuggestions)
                        break;
                }
            }

            /*
             * ---------------------------------------------------------
             * VIDEOS
             * ---------------------------------------------------------
             */
            if (SearchSuggestions.Count < maxSuggestions)
            {
                foreach (var video in LibraryRepository.VideoItems)
                {
                    if (video == null)
                        continue;

                    if (!Matches(video.Title.Trim(), query))
                        continue;

                    var item = new SearchSuggestionItem
                    {
                        Id = video.Id,
                        Path = video.Path,
                        Title = video.Title,
                        Artist = "",
                        ArtistId = "",
                        Album = "",
                        AlbumId = "",
                        Type = SearchSuggestionType.Video
                    };

                    var vm = new SearchSuggestionViewModel(item);
                    SearchSuggestions.Add(vm);

                    if (SearchSuggestions.Count >= maxSuggestions)
                        break;
                }
            }

            /*
             * ---------------------------------------------------------
             * ALBUMS
             * ---------------------------------------------------------
             */
            foreach (var album in LibraryRepository.AlbumItems)
            {
                if (album == null)
                    continue;

                if (!Matches(album.Title.Trim(), query))
                    continue;

                var item = new SearchSuggestionItem
                {
                    Id = album.Id,
                    Path = album.Item.Songs.FirstOrDefault().Path,
                    Title = album.Title,
                    Artist = album.Artist,
                    ArtistId = album.ArtistId,
                    Album = album.Title,
                    AlbumId = album.Id,
                    Type = SearchSuggestionType.Album
                };

                var vm = new SearchSuggestionViewModel(item);
                SearchSuggestions.Add(vm);

                if (SearchSuggestions.Count >= maxSuggestions)
                    break;
            }

            /*
             * ---------------------------------------------------------
             * ARTISTS
             * ---------------------------------------------------------
             */
            if (SearchSuggestions.Count < maxSuggestions)
            {
                foreach (var artist in LibraryRepository.ArtistItems)
                {
                    if (artist == null)
                        continue;

                    if (!Matches(artist.Title.Trim(), query))
                        continue;

                    var item = new SearchSuggestionItem
                    {
                        Id = artist.Id,
                        Path = "",
                        Title = artist.Title,
                        Artist = artist.Title,
                        ArtistId = artist.Id,
                        Album = "",
                        AlbumId = "",
                        Type = SearchSuggestionType.Artist
                    };

                    var vm = new SearchSuggestionViewModel(item);
                    SearchSuggestions.Add(vm);

                    if (SearchSuggestions.Count >= maxSuggestions)
                        break;
                }
            }



            /*
             * ---------------------------------------------------------
             * PLAYLISTS
             * ---------------------------------------------------------
             */
            if (SearchSuggestions.Count < maxSuggestions)
            {
                foreach (var playlist in PlaylistRepository.Playlists)
                {
                    if (playlist == null)
                        continue;

                    if (!Matches(playlist.Title.Trim(), query))
                        continue;

                    var item = new SearchSuggestionItem
                    {
                        Id = playlist.Id,
                        Path = "",
                        Title = playlist.Title,
                        Artist = "",
                        ArtistId = "",
                        Album = "",
                        AlbumId = "",
                        Type = SearchSuggestionType.Playlist
                    };

                    var vm = new SearchSuggestionViewModel(item);
                    SearchSuggestions.Add(vm);

                    if (SearchSuggestions.Count >= maxSuggestions)
                        break;
                }
            }

            SearchBox.IsSuggestionListOpen = SearchSuggestions.Count > 0;
        }

        private static bool Matches(string value, string query)
        {
            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(query))
                return false;

            return value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void DrawUnderStatusBar()
        {
            // 1. Extend view into the title bar
            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;

            // 2. Set up transparent system caption buttons
            var appTitleBar = ApplicationView.GetForCurrentView().TitleBar;
            appTitleBar.ButtonBackgroundColor = Colors.Transparent;
            appTitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            // 3. Set the custom XAML element as the draggable title bar
            Window.Current.SetTitleBar(TitleBar);

            // 4. Apply the Mica material backdrop
            //var backdropController = new BackdropMaterial();
            muxc.BackdropMaterial.SetApplyToRootOrPageBackground(this, true);
        }
    }
}
