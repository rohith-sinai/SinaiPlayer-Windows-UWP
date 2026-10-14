using SinaiPlayer.Lib.Helpers;
using SinaiPlayer.Lib.Models;
using SinaiPlayer.Lib.Repos;
using SinaiPlayer.Lib.Services;
using SinaiPlayer.Lib.UI.Lists;
using SinaiPlayer.Lib.UI.ViewModels;
using SinaiPlayer.Lib.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

namespace SinaiPlayer.Lib.UI.Adapters
{
    public sealed partial class AlbumGridAdapter : UserControl
    {
        public static readonly DependencyProperty ShowYearInsteadOfArtistProperty =
            DependencyProperty.Register(
                nameof(ShowYearInsteadOfArtist),
                typeof(bool),
                typeof(AlbumGridAdapter),
                new PropertyMetadata(false, OnShowYearInsteadOfArtistChanged));

        public bool ShowYearInsteadOfArtist
        {
            get => (bool)GetValue(ShowYearInsteadOfArtistProperty);
            set => SetValue(ShowYearInsteadOfArtistProperty, value);
        }

        private static void OnShowYearInsteadOfArtistChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if (d is AlbumGridAdapter adapter)
                adapter.UpdateYearArtistVisibility();
        }

        private void UpdateYearArtistVisibility()
        {
            bool showYear = ShowYearInsteadOfArtist;

            ArtistTextBlock.Visibility = showYear
                ? Visibility.Collapsed
                : Visibility.Visible;

            YearTextBlock.Visibility = showYear
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private MediaPlayerService Player => MediaPlayerService.Instance;

        public AlbumGridAdapter()
        {
            InitializeComponent();

            UpdateYearArtistVisibility();

            PlayMenuFlyoutSubItem.Text = LocalizationHelper.GetString("Play");
            PlayNowMenuFlyoutItem.Text = LocalizationHelper.GetString("Now");
            PlayNextMenuFlyoutItem.Text = LocalizationHelper.GetString("Next");
            PlayLastMenuFlyoutItem.Text = LocalizationHelper.GetString("Last");
            AddToPlaylistMenuFlyoutItem.Text = LocalizationHelper.GetString("AddToPlaylist");
            ShowArtistMenuFlyoutItem.Text = LocalizationHelper.GetString("ShowArtist");
            EditMenuFlyoutItem.Text = LocalizationHelper.GetString("Edit");
            InfoMenuFlyoutItem.Text = LocalizationHelper.GetString("Info");
            DeleteMenuFlyoutItem.Text = LocalizationHelper.GetString("Delete");

            DataContextChanged += Adapter_DataContextChanged;
        }

        private async void Adapter_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            Bindings.Update();

            if (DataContext is AlbumViewModel vm)
            {
                await vm.EnsureArtworkLoadedAsync();
            }
        }

        private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "HoverState", true);
        }

        private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "NormalState", true);
        }

        private async void Play_HoverButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is AlbumViewModel vm)
            {
                await Player.SetQueueAsync(vm.Item.Songs);
            }
        }

        private void AddToPlaylist_HoverButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is AlbumViewModel vm)
            {
                DialogUtils.Show_AddMediaFilesToPlaylistDialog(vm.Item.Songs);
            }
        }

        private async void PlayNow_FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the clicked menu item
            if (sender is MenuFlyoutItem menuItem)
            {
                // 2. Locate the flyout container targeting the row
                if (menuItem.DataContext is AlbumViewModel clickedItem)
                {
                    if (clickedItem is null)
                        return;

                    var album = clickedItem.Item;

                    if (album is null)
                        return;

                    await Player.SetQueueAsync(album.Songs);
                }
            }
        }

        private void PlayNext_FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the clicked menu item
            if (sender is MenuFlyoutItem menuItem)
            {
                // 2. Locate the flyout container targeting the row
                if (menuItem.DataContext is AlbumViewModel clickedItem)
                {
                    if (clickedItem is null)
                        return;

                    var album = clickedItem.Item;

                    if (album is null)
                        return;

                    var songs = album.Songs;

                    songs.Reverse();

                    foreach (var song in songs)
                    {
                        //TODO: Reverse the list
                        MediaPlayerService.Instance.PlayNext(song);
                    }
                }
            }
        }

        private void PlayLast_FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the clicked menu item
            if (sender is MenuFlyoutItem menuItem)
            {
                // 2. Locate the flyout container targeting the row
                if (menuItem.DataContext is AlbumViewModel clickedItem)
                {
                    if (clickedItem is null)
                        return;

                    var album = clickedItem.Item;

                    if (album is null)
                        return;

                    foreach (var song in album.Songs)
                    {
                        MediaPlayerService.Instance.AddQueueItemLast(song);
                    }

                }
            }
        }

        private void AddToPlaylist_FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the clicked menu item
            if (sender is MenuFlyoutItem menuItem)
            {
                // 2. Locate the flyout container targeting the row
                if (menuItem.DataContext is AlbumViewModel clickedItem)
                {
                    if (clickedItem is null)
                        return;

                    var album = clickedItem.Item;

                    if (album is null)
                        return;

                    DialogUtils.Show_AddMediaFilesToPlaylistDialog(album.Songs);
                }
            }
        }

        private void ShowArtist_FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the clicked menu item
            if (sender is MenuFlyoutItem menuItem)
            {
                // 2. Locate the flyout container targeting the row
                if (menuItem.DataContext is AlbumViewModel clickedItem)
                {
                    if (clickedItem is null)
                        return;

                    var album = clickedItem.Item;

                    if (album is null)
                        return;

                    NavigationUtils.Navigate_ArtistDetailsPage(album.ArtistId);
                }
            }
        }

        private void Properties_FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the clicked menu item
            if (sender is MenuFlyoutItem menuItem)
            {
                // 2. Locate the flyout container targeting the row
                if (menuItem.DataContext is AlbumViewModel clickedItem)
                {
                    //TODO: Implement Properties dialog
                }
            }
        }

        private void Edit_FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the clicked menu item
            if (sender is MenuFlyoutItem menuItem)
            {
                // 2. Locate the flyout container targeting the row
                if (menuItem.DataContext is AlbumViewModel clickedItem)
                {
                    DialogUtils.Show_EditAlbumTagsDialog(clickedItem.Item);
                }
            }
        }

        private void Delete_FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the clicked menu item
            if (sender is MenuFlyoutItem menuItem)
            {
                // 2. Locate the flyout container targeting the row
                if (menuItem.DataContext is AlbumViewModel clickedItem)
                {
                    DialogUtils.Show_DeleteMediaFilesDialog(clickedItem.Item.Songs, "Album");
                }
            }
        }
    }
}
