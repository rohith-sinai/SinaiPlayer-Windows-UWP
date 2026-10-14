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
    public sealed partial class VideoGridAdapter : UserControl
    {
        MediaPlayerService Player => MediaPlayerService.Instance;

        public event EventHandler<VideoViewModel> ItemClickEvent;

        public VideoGridAdapter()
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

            if (DataContext is VideoViewModel vm)
            {
                await vm.EnsureArtworkLoadedAsync();
            }
        }
        private void Item_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            // The sender is the framework element containing the data context
            if (sender is FrameworkElement element && element.DataContext is VideoViewModel clickedItem)
            {
                ItemClickEvent?.Invoke(sender, clickedItem);
            }
        }

        private async void PlayNow_FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the clicked menu item
            if (sender is MenuFlyoutItem menuItem)
            {
                // 2. Locate the flyout container targeting the row
                if (menuItem.DataContext is VideoViewModel clickedItem)
                {
                    if (clickedItem is null)
                        return;

                    var video = clickedItem.Item;

                    if (video is null)
                        return;

                    List<MediaFile> queue = new List<MediaFile> { video };

                    await Player.SetQueueAsync(queue);

                    NavigationUtils.Navigate_NowPlayingPage();
                }
            }
        }

        private void PlayNext_FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the clicked menu item
            if (sender is MenuFlyoutItem menuItem)
            {
                // 2. Locate the flyout container targeting the row
                if (menuItem.DataContext is VideoViewModel clickedItem)
                {
                    if (clickedItem is null)
                        return;

                    var video = clickedItem.Item;

                    if (video is null)
                        return;

                    Player.PlayNext(video);
                }
            }
        }

        private void PlayLast_FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the clicked menu item
            if (sender is MenuFlyoutItem menuItem)
            {
                // 2. Locate the flyout container targeting the row
                if (menuItem.DataContext is VideoViewModel clickedItem)
                {
                    if (clickedItem is null)
                        return;

                    var video = clickedItem.Item;

                    if (video is null)
                        return;

                    MediaPlayerService.Instance.AddQueueItemLast(video);
                }
            }
        }

        private void AddToPlaylist_FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the clicked menu item
            if (sender is MenuFlyoutItem menuItem)
            {
                // 2. Locate the flyout container targeting the row
                if (menuItem.DataContext is VideoViewModel clickedItem)
                {
                    if (clickedItem is null)
                        return;

                    var video = clickedItem.Item;

                    if (video is null)
                        return;

                    List<MediaFile> items = new List<MediaFile> { video };

                    DialogUtils.Show_AddMediaFilesToPlaylistDialog(items);
                }
            }
        }

        private void Properties_FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the clicked menu item
            if (sender is MenuFlyoutItem menuItem)
            {
                // 2. Locate the flyout container targeting the row
                if (menuItem.DataContext is VideoViewModel clickedItem)
                {
                    DialogUtils.Show_VideoInfoDialog(clickedItem.Item);
                }
            }
        }

        private void Delete_FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the clicked menu item
            if (sender is MenuFlyoutItem menuItem)
            {
                // 2. Locate the flyout container targeting the row
                if (menuItem.DataContext is VideoViewModel clickedItem)
                {
                    List<MediaFile> items= new List<MediaFile> { clickedItem.Item };
                    DialogUtils.Show_DeleteMediaFilesDialog(items, "Video");
                }
            }
        }
    }
}
