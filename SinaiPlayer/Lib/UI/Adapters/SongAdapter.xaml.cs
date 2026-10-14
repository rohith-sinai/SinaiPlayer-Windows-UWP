using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using SinaiPlayer.Lib.Helpers;
using SinaiPlayer.Lib.Models;
using SinaiPlayer.Lib.Repos;
using SinaiPlayer.Lib.Services;
using SinaiPlayer.Lib.UI.ViewModels;
using SinaiPlayer.Lib.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SinaiPlayer.Lib.UI.Adapters
{
    public sealed partial class SongAdapter : UserControl
    {
        public event EventHandler<SongViewModel> ItemClickEvent;

        public static readonly DependencyProperty ShowIndexProperty =
           DependencyProperty.Register(
               nameof(ShowIndex),
               typeof(bool),
               typeof(SongAdapter),
               new PropertyMetadata(false, OnShowIndexChanged));

        public bool ShowIndex
        {
            get => (bool)GetValue(ShowIndexProperty);
            set => SetValue(ShowIndexProperty, value);
        }

        private static void OnShowIndexChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if (d is SongAdapter adapter)
                adapter.UpdateIndexVisibility();
        }

        public static readonly DependencyProperty GridColumnDefination3StringProperty =
            DependencyProperty.Register(
                nameof(GridColumnDefination3String),
                typeof(string),
                typeof(SongAdapter),
                new PropertyMetadata("Auto"));

        public string GridColumnDefination3String
        {
            get => (string)GetValue(GridColumnDefination3StringProperty);
            private set => SetValue(GridColumnDefination3StringProperty, value);
        }

        public static readonly DependencyProperty AlbumTextBlockVisibilityProperty =
            DependencyProperty.Register(
                nameof(AlbumTextBlockVisibility),
                typeof(Visibility),
                typeof(SongAdapter),
                new PropertyMetadata(Visibility.Visible));

        public Visibility AlbumTextBlockVisibility
        {
            get => (Visibility)GetValue(AlbumTextBlockVisibilityProperty);
            private set => SetValue(AlbumTextBlockVisibilityProperty, value);
        }

        private void UpdateIndexVisibility()
        {
            bool showIndex = ShowIndex;

            if (TrackNumberTextBlock != null)
            {
                TrackNumberTextBlock.Visibility = showIndex
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            if (DataContext is SongViewModel vm)
            {
                GridColumnDefination3String = showIndex
                    ? "Auto"
                    : vm.GridColumnDefination3Width;

                AlbumTextBlockVisibility = showIndex
                    ? Visibility.Collapsed
                    : vm.AlbumVisibility;
            }
            else
            {
                GridColumnDefination3String = "Auto";
                AlbumTextBlockVisibility = Visibility.Collapsed;
            }

            Bindings.Update();
        }

        private MediaPlayerService Player => MediaPlayerService.Instance;

        Color AccentColor = (Color)Application.Current.Resources["SystemAccentColor"];

        Color SecondaryColor = (Color)Application.Current.Resources["SystemBaseHighColor"];

        public SongAdapter()
        {
            InitializeComponent();

            PlayMenuFlyoutSubItem.Text = LocalizationHelper.GetString("Play");
            PlayNowMenuFlyoutItem.Text = LocalizationHelper.GetString("Now");
            PlayNextMenuFlyoutItem.Text = LocalizationHelper.GetString("Next");
            PlayLastMenuFlyoutItem.Text = LocalizationHelper.GetString("Last");
            AddToPlaylistMenuFlyoutItem.Text = LocalizationHelper.GetString("AddToPlaylist");
            ShowArtistMenuFlyoutItem.Text = LocalizationHelper.GetString("ShowArtist");
            ShowAlbumMenuFlyoutItem.Text = LocalizationHelper.GetString("ShowAlbum");
            EditMenuFlyoutItem.Text = LocalizationHelper.GetString("Edit");
            InfoMenuFlyoutItem.Text = LocalizationHelper.GetString("Info");
            DeleteMenuFlyoutItem.Text = LocalizationHelper.GetString("Delete");

            this.DataContextChanged += Adapter_DataContextChanged;
            Player.PropertyChanged += PlayerPropertyChanged;
        }
        private void PlayerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Player.CurrentMedia):
                    UpdateTextColors();
                    break;
                case nameof(Player.QueueChanged):
                    UpdateTextColors();
                    break;
                case nameof(Player.IsPlaying):

                    break;
            }
        }

        private void UpdateTextColors()
        {
            if (this.DataContext is SongViewModel vm)
            {
                var item = Player.CurrentMedia;

                if (item != null && item.Id == vm.Id)
                {
                    TitleTextBlock.Foreground = new SolidColorBrush { Color = AccentColor };
                }
                else
                {
                    TitleTextBlock.Foreground = new SolidColorBrush { Color = SecondaryColor };
                }
            }

        }

        private void Adapter_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            UpdateIndexVisibility();
            Bindings.Update();
            UpdateTextColors();
        }

        private void Item_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "HoverState", true);
        }

        private void Item_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "NormalState", true);
        }

        private void Item_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            // The sender is the framework element containing the data context
            if (sender is FrameworkElement element && element.DataContext is SongViewModel clickedItem)
            {
                ItemClickEvent?.Invoke(sender, clickedItem);
            }
        }

        private async void Play_HoverButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is SongViewModel vm)
            {
                List<MediaFile> queue = new List<MediaFile> { vm.Item };
                await Player.SetQueueAsync(queue);
            }
        }

        private void AddToPlaylist_HoverButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is SongViewModel vm)
            {
                if (vm is null)
                    return;

                var song = vm.Item;

                if (song is null)
                    return;

                List<MediaFile> items = new List<MediaFile> { song };

                DialogUtils.Show_AddMediaFilesToPlaylistDialog(items);
            }
        }

        private async void PlayNow_FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the clicked menu item
            if (sender is MenuFlyoutItem menuItem)
            {
                // 2. Locate the flyout container targeting the row
                if (menuItem.DataContext is SongViewModel clickedItem)
                {
                    if (clickedItem is null)
                        return;

                    var song = clickedItem.Item;

                    if (song is null)
                        return;

                    List<MediaFile> queue = new List<MediaFile> { song };
                    await MediaPlayerService.Instance.SetQueueAsync(queue);
                }
            }
        }

        private void PlayNext_FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the clicked menu item
            if (sender is MenuFlyoutItem menuItem)
            {
                // 2. Locate the flyout container targeting the row
                if (menuItem.DataContext is SongViewModel clickedItem)
                {
                    if (clickedItem is null)
                        return;

                    var song = clickedItem.Item;

                    if (song is null)
                        return;

                    MediaPlayerService.Instance.PlayNext(song);
                }
            }
        }

        private void PlayLast_FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the clicked menu item
            if (sender is MenuFlyoutItem menuItem)
            {
                // 2. Locate the flyout container targeting the row
                if (menuItem.DataContext is SongViewModel clickedItem)
                {
                    if (clickedItem is null)
                        return;

                    var song = clickedItem.Item;

                    if (song is null)
                        return;

                    MediaPlayerService.Instance.AddQueueItemLast(song);
                }
            }
        }

        private void AddToPlaylist_FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the clicked menu item
            if (sender is MenuFlyoutItem menuItem)
            {
                // 2. Locate the flyout container targeting the row
                if (menuItem.DataContext is SongViewModel clickedItem)
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

        private void ShowArtist_FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the clicked menu item
            if (sender is MenuFlyoutItem menuItem)
            {
                // 2. Locate the flyout container targeting the row
                if (menuItem.DataContext is SongViewModel clickedItem)
                {
                    if (clickedItem is null)
                        return;

                    var song = clickedItem.Item;

                    if (song is null)
                        return;

                    NavigationUtils.Navigate_ArtistDetailsPage(song.AlbumArtistId);
                }
            }
        }

        private void ShowAlbum_FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the clicked menu item
            if (sender is MenuFlyoutItem menuItem)
            {
                // 2. Locate the flyout container targeting the row
                if (menuItem.DataContext is SongViewModel clickedItem)
                {
                    if (clickedItem is null)
                        return;

                    var song = clickedItem.Item;

                    if (song is null)
                        return;

                    NavigationUtils.Navigate_AlbumDetailsPage(song.AlbumId);
                }
            }
        }

        private void Properties_FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the clicked menu item
            if (sender is MenuFlyoutItem menuItem)
            {
                // 2. Locate the flyout container targeting the row
                if (menuItem.DataContext is SongViewModel clickedItem)
                {
                    DialogUtils.Show_SongInfoDialog(clickedItem.Item);
                }
            }
        }

        private void Edit_FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the clicked menu item
            if (sender is MenuFlyoutItem menuItem)
            {
                // 2. Locate the flyout container targeting the row
                if (menuItem.DataContext is SongViewModel clickedItem)
                {
                    DialogUtils.Show_EditSongTagsDialog(clickedItem.Item);
                }
            }
        }

        private void Delete_FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the clicked menu item
            if (sender is MenuFlyoutItem menuItem)
            {
                // 2. Locate the flyout container targeting the row
                if (menuItem.DataContext is SongViewModel clickedItem)
                {
                    List<MediaFile> items = new List<MediaFile> { clickedItem.Item };
                    DialogUtils.Show_DeleteMediaFilesDialog(items, "Song");
                }
            }
        }
    }
}
