using SinaiPlayer.Lib.Enums;
using SinaiPlayer.Lib.Helpers;
using SinaiPlayer.Lib.Models;
using SinaiPlayer.Lib.Repos;
using SinaiPlayer.Lib.Services;
using SinaiPlayer.Lib.UI.ViewModels;
using SinaiPlayer.Lib.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Search.Core;
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
    public sealed partial class SearchPage : Page
    {
        private string query;

        private ObservableCollection<SongViewModel> SongItems { get; }
            = new ObservableCollection<SongViewModel>();
        private ObservableCollection<VideoViewModel> VideoItems { get; }
            = new ObservableCollection<VideoViewModel>();
        private ObservableCollection<ArtistViewModel> ArtistItems { get; }
            = new ObservableCollection<ArtistViewModel>();
        private ObservableCollection<AlbumViewModel> AlbumItems { get; }
            = new ObservableCollection<AlbumViewModel>();
        private ObservableCollection<PlaylistViewModel> PlaylistItems { get; }
            = new ObservableCollection<PlaylistViewModel>();

        private MediaPlayerService Player => MediaPlayerService.Instance;

        public SearchPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is string query)
            {
                this.query = query;
                LoadContent(query);
            }

        }

        private void LoadContent(string query)
        {
            LargeTitle.Text = LocalizationHelper.Format("SearchResultsHeader", query);
            SongsListViewHeaderTextBlock.Text = LocalizationHelper.GetString("Songs");
            ArtistsListViewHeaderTextBlock.Text = LocalizationHelper.GetString("Artists");
            AlbumsListViewHeaderTextBlock.Text = LocalizationHelper.GetString("Albums");
            VideosListViewHeaderTextBlock.Text = LocalizationHelper.GetString("Videos");
            PlaylistsListViewHeaderTextBlock.Text = LocalizationHelper.GetString("Playlists");

            MakeSongsList();
            MakeVideosList();
            MakeArtistsList();
            MakeAlbumsList();
            MakePlaylistsList();
        }

        // Make Lists
        private void MakeSongsList()
        {
            foreach (var song in LibraryRepository.SongItems)
            {
                if (song == null)
                    continue;

                if (!Matches(song.Title.Trim()))
                    continue;

                SongItems.Add(song);
            }

            SongsList.Visibility = SongItems.Count != 0 ? Visibility.Visible : Visibility.Collapsed;
            SongsList.ItemsSource = SongItems;
        }

        private void MakeVideosList()
        {
            foreach (var video in LibraryRepository.VideoItems)
            {
                if (video == null)
                    continue;

                if (!Matches(video.Title.Trim()))
                    continue;

                VideoItems.Add(video);
            }

            VideosList.Visibility = VideoItems.Count != 0 ? Visibility.Visible : Visibility.Collapsed;
            VideosList.ItemsSource = VideoItems;
        }
        private void MakeArtistsList()
        {
            foreach (var artist in LibraryRepository.ArtistItems)
            {
                if (artist == null)
                    continue;

                if (!Matches(artist.Title.Trim()))
                    continue;

                ArtistItems.Add(artist);
            }

            ArtistsList.Visibility = ArtistItems.Count != 0 ? Visibility.Visible : Visibility.Collapsed;
            ArtistsList.ItemsSource = ArtistItems;

        }
        private void MakeAlbumsList()
        {
            foreach (var album in LibraryRepository.AlbumItems)
            {
                if (album == null)
                    continue;

                if (!Matches(album.Title.Trim()))
                    continue;

                AlbumItems.Add(album);
            }

            AlbumsList.Visibility = AlbumItems.Count != 0 ? Visibility.Visible : Visibility.Collapsed;
            AlbumsList.ItemsSource = AlbumItems;

        }
        private void MakePlaylistsList()
        {
            foreach (var playlist in PlaylistRepository.Playlists)
            {
                if (playlist == null)
                    continue;

                if (!Matches(playlist.Title.Trim()))
                    continue;

                PlaylistItems.Add(playlist);
            }

            PlaylistsList.Visibility = PlaylistItems.Count != 0 ? Visibility.Visible : Visibility.Collapsed;
            PlaylistsList.ItemsSource = PlaylistItems;

        }


        // Item Click Events
        private async void SongAdapter_ItemClickEvent(object sender, SongViewModel e)
        {
            List<MediaFile> queue = new List<MediaFile>();

            foreach(var song in SongItems)
            {
                queue.Add(song.Item);
            }

            var index = queue.FindIndex(x => x.Id == e.Id);
            await Player.SetQueueAsync(queue, index);
        }

        private async void VideoAdapter_ItemClickEvent(object sender, VideoViewModel e)
        {
            List<MediaFile> queue = new List<MediaFile>();

            foreach (var video in VideoItems)
            {
                queue.Add(video.Item);
            }

            var index = queue.FindIndex(x => x.Id == e.Id);
            await Player.SetQueueAsync(queue, index);
        }

        private void AlbumGridView_ItemClickEvent(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is AlbumViewModel vm)
            {
                NavigationUtils.Navigate_AlbumDetailsPage(vm.Id);
            }
        }

        private void ArtistGridView_ItemClickEvent(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is ArtistViewModel vm)
            {
                NavigationUtils.Navigate_ArtistDetailsPage(vm.Id);
            }
        }

        private void PlaylistGridView_ItemClickEvent(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is PlaylistViewModel vm)
            {
                NavigationUtils.Navigate_PlaylistDetailsPage(vm.Id);
            }
        }

        // Helpers
        private bool Matches(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(query))
                return false;

            return value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        } 
    }
}