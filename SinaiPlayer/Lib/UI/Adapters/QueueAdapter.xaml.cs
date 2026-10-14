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
    public sealed partial class QueueAdapter : UserControl
    {
        MediaPlayerService Player => MediaPlayerService.Instance;

        public QueueAdapter()
        {
            InitializeComponent(); 
            PlayNowMenuFlyoutItem.Text = LocalizationHelper.GetString("Play"); 
            AddToPlaylistMenuFlyoutItem.Text = LocalizationHelper.GetString("AddToPlaylist"); 
            RemoveMenuFlyoutItem.Text = LocalizationHelper.GetString("Remove");
            this.DataContextChanged += Adapter_DataContextChanged;
        }

        private async void Adapter_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            Bindings.Update();
        }

        private async void PlayNow_FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the clicked menu item
            if (sender is MenuFlyoutItem menuItem)
            {
                // 2. Locate the flyout container targeting the row
                if (menuItem.DataContext is QueueViewModel clickedItem)
                {
                    if (clickedItem is null)
                        return;

                    var song = clickedItem.Item;

                    if (song is null)
                        return;

                    var index = Player.Queue.ToList().FindIndex(x => x.Id == song.Id);

                    await Player.PlayAtIndexAsync(index);
                }
            }
        }

        private void AddToPlaylist_FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the clicked menu item
            if (sender is MenuFlyoutItem menuItem)
            {
                // 2. Locate the flyout container targeting the row
                if (menuItem.DataContext is QueueViewModel clickedItem)
                {
                    if (clickedItem is null)
                        return;

                    var song = clickedItem.Item;

                    if (song is null)
                        return;

                    List<MediaFile> items = new List<MediaFile> { song };

                    DialogUtils.Show_AddMediaFilesToPlaylistDialog(items);
                }
            }
        }

        private async void Remove_FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the clicked menu item
            if (sender is MenuFlyoutItem menuItem)
            {
                // 2. Locate the flyout container targeting the row
                if (menuItem.DataContext is QueueViewModel clickedItem)
                {
                    if (clickedItem is null)
                        return;

                    var song = clickedItem.Item;

                    if (song is null)
                        return;

                    var index = Player.Queue.ToList().FindIndex(x => x.Id == song.Id);

                    await Player.RemoveQueueItemAtAsync(index);
                }
            }
        }

    }
}
