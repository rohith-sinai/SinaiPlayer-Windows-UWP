using SinaiPlayer.Lib.Helpers;
using SinaiPlayer.Lib.Models;
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
    public sealed partial class PlaylistGridAdapter : UserControl
    {
        private MediaPlayerService Player => MediaPlayerService.Instance;
        public PlaylistGridAdapter()
        {
            InitializeComponent();

            PlayMenuFlyoutSubItem.Text = LocalizationHelper.GetString("Play");
            PlayNowMenuFlyoutItem.Text = LocalizationHelper.GetString("Now");
            PlayNextMenuFlyoutItem.Text = LocalizationHelper.GetString("Next");
            PlayLastMenuFlyoutItem.Text = LocalizationHelper.GetString("Last"); 
            EditMenuFlyoutItem.Text = LocalizationHelper.GetString("Edit"); 
            DeleteMenuFlyoutItem.Text = LocalizationHelper.GetString("Delete");

            this.DataContextChanged += Adapter_DataContextChanged;
        }

        private async void Adapter_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            Bindings.Update();

            if (DataContext is PlaylistViewModel vm)
            {
                await vm.EnsureArtworkLoadedAsync();
            }
        }

        private async void PlayNow_FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the clicked menu item
            if (sender is MenuFlyoutItem menuItem)
            {
                // 2. Locate the flyout container targeting the row
                if (menuItem.DataContext is PlaylistViewModel clickedItem)
                {
                    if (clickedItem is null)
                        return;

                    var playlist = clickedItem.Item;

                    if (playlist is null)
                        return;

                    List<MediaFile> items = new List<MediaFile>();

                    foreach(var i in playlist.Items)
                    {
                        var file = i.File;

                        if(file != null)
                        {
                            items.Add(file);
                        }
                    }

                    await Player.SetQueueAsync(items);
                }
            }
        }

        private void PlayNext_FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the clicked menu item
            if (sender is MenuFlyoutItem menuItem)
            {
                // 2. Locate the flyout container targeting the row
                if (menuItem.DataContext is PlaylistViewModel clickedItem)
                {
                    if (clickedItem is null)
                        return;

                    var playlist = clickedItem.Item;

                    if (playlist is null)
                        return;

                    List<MediaFile> items = new List<MediaFile>();

                    foreach (var i in playlist.Items)
                    {
                        var file = i.File;

                        if (file != null)
                        {
                            items.Add(file);
                        }
                    }
                    
                    items.Reverse();

                    foreach (var i in items)
                    {
                        Player.PlayNext(i);
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
                if (menuItem.DataContext is PlaylistViewModel clickedItem)
                {
                    if (clickedItem is null)
                        return;

                    var playlist = clickedItem.Item;

                    if (playlist is null)
                        return;

                    List<MediaFile> items = new List<MediaFile>();

                    foreach (var i in playlist.Items)
                    {
                        var file = i.File;

                        if (file != null)
                        {
                            items.Add(file);
                        }
                    }

                    foreach (var i in items)
                    {
                        Player.AddQueueItemLast(i);
                    }

                }
            }
        }

        private void Edit_FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the clicked menu item
            if (sender is MenuFlyoutItem menuItem)
            {
                // 2. Locate the flyout container targeting the row
                if (menuItem.DataContext is PlaylistViewModel clickedItem)
                {
                    DialogUtils.Show_EditPlaylistDialog(clickedItem);
                }
            }
        }

        private void Delete_FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the clicked menu item
            if (sender is MenuFlyoutItem menuItem)
            {
                // 2. Locate the flyout container targeting the row
                if (menuItem.DataContext is PlaylistViewModel clickedItem)
                {
                    DialogUtils.Show_DeletePlaylistDialog(clickedItem);
                }
            }
        }
    }
}
