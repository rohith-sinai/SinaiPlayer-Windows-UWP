using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using SinaiPlayer.Lib.Repos;
using SinaiPlayer.Lib.Services;
using SinaiPlayer.Lib.UI.ViewModels;
using SinaiPlayer.Lib.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using SinaiPlayer.Lib.Helpers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SinaiPlayer.Lib.UI.Adapters
{
    public sealed partial class ArtistGridAdapter : UserControl
    {
        private MediaPlayerService Player => MediaPlayerService.Instance;

        public ArtistGridAdapter()
        {
            InitializeComponent();
            
            PlayMenuFlyoutSubItem.Text = LocalizationHelper.GetString("Play");
            PlayNowMenuFlyoutItem.Text = LocalizationHelper.GetString("Now");
            PlayNextMenuFlyoutItem.Text = LocalizationHelper.GetString("Next");
            PlayLastMenuFlyoutItem.Text = LocalizationHelper.GetString("Last");
            AddToPlaylistMenuFlyoutItem.Text = LocalizationHelper.GetString("AddToPlaylist"); 
            InfoMenuFlyoutItem.Text = LocalizationHelper.GetString("Info");
            DeleteMenuFlyoutItem.Text = LocalizationHelper.GetString("Delete");

            this.DataContextChanged += Adapter_DataContextChanged;
        }

        private async void Adapter_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            Bindings.Update();

            if (DataContext is ArtistViewModel vm)
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
            if (this.DataContext is ArtistViewModel vm)
            {
                await Player.SetQueueAsync(vm.Item.Songs);
            }
        }

        private void AddToPlaylist_HoverButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is ArtistViewModel vm)
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
                if (menuItem.DataContext is ArtistViewModel clickedItem)
                {
                    if (clickedItem is null)
                        return;

                    var artist = clickedItem.Item;

                    if (artist is null)
                        return;

                    await Player.SetQueueAsync(artist.Songs);
                }
            }
        }

        private void PlayNext_FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the clicked menu item
            if (sender is MenuFlyoutItem menuItem)
            {
                // 2. Locate the flyout container targeting the row
                if (menuItem.DataContext is ArtistViewModel clickedItem)
                {
                    if (clickedItem is null)
                        return;

                    var artist = clickedItem.Item;

                    if (artist is null)
                        return;

                    var songs = artist.Songs;

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
                if (menuItem.DataContext is ArtistViewModel clickedItem)
                {
                    if (clickedItem is null)
                        return;

                    var artist = clickedItem.Item;

                    if (artist is null)
                        return;

                    foreach (var song in artist.Songs)
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
                if (menuItem.DataContext is ArtistViewModel clickedItem)
                {
                    if (clickedItem is null)
                        return;

                    var artist = clickedItem.Item;

                    if (artist is null)
                        return;

                    DialogUtils.Show_AddMediaFilesToPlaylistDialog(artist.Songs);
                }
            }
        }

        private void Properties_FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the clicked menu item
            if (sender is MenuFlyoutItem menuItem)
            {
                // 2. Locate the flyout container targeting the row
                if (menuItem.DataContext is ArtistViewModel clickedItem)
                {
                    //TODO: Implement Properties dialog
                }
            }
        }

        private void Delete_FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the clicked menu item
            if (sender is MenuFlyoutItem menuItem)
            {
                // 2. Locate the flyout container targeting the row
                if (menuItem.DataContext is ArtistViewModel clickedItem)
                {
                    DialogUtils.Show_DeleteMediaFilesDialog(clickedItem.Item.Songs, "Artist");
                }
            }
        }
    }
}
