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
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
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
    public sealed partial class PlaylistDetailsPage : Page
    {
        private const double HeaderMaxSize = 300;
        private const double HeaderMinSize = 200;
        private const double ArtworkMaxSize = 200;
        private const double ArtworkMinSize = 100;

        private const double HeaderShrinkRange = HeaderMaxSize - HeaderMinSize;
        private const double ArtworkShrinkRange = ArtworkMaxSize - ArtworkMinSize;

        public ObservableCollection<PlaylistItemViewModel> MediaFilesItems { get; }
            = new ObservableCollection<PlaylistItemViewModel>();

        private MediaPlayerService Player => MediaPlayerService.Instance;

        private PlaylistViewModel viewmodel;
        public PlaylistDetailsPage()
        {
            InitializeComponent();

            PlayButtonTextBlock.Text = LocalizationHelper.GetString("Play");
            ShuffleButtonTextBlock.Text = LocalizationHelper.GetString("Shuffle");
            EditButtonTextBlock.Text = LocalizationHelper.GetString("Edit");
            DeleteButtonTextBlock.Text = LocalizationHelper.GetString("Delete");
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is string id)
            {
                LoadListViewData(id);
            }
        }

        private async void LoadListViewData(string id)
        {
            var vm = PlaylistRepository.Playlists.First(x => x.Id == id);
            viewmodel = vm;
            Title_TextBlock.Text = vm.Title;
            SubTitle_TextBlock.Text = vm.SubTitle;

            Items_ListView.ItemsSource = vm.Items.ToList();

            await vm.EnsureArtworkLoadedAsync();
            CoverArtImage.Source = vm.Image;
            if (vm.PaletteColor != Colors.Transparent)
            {
                //BackgroundBrush.TintColor = vm.PaletteColor;
                //BackgroundBrush.TintOpacity = 0.5;
                SolidColorBrush brush = new SolidColorBrush();
                brush.Color = vm.PaletteColor;
                brush.Opacity = 0.7;
                PaletteView.Fill = brush;
            }
        }

        private void BodyScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            double offset = BodyScrollViewer.VerticalOffset;

            // Only the first `ShrinkRange` pixels of scrolling affect the header.
            // Past that, the header simply stays pinned at its minimum size.
            double clampedHeader = Math.Max(0, Math.Min(offset, HeaderShrinkRange));
            double clampedArtwork = Math.Max(0, Math.Min(offset, ArtworkShrinkRange));

            // 0.0 at the top -> 1.0 once fully shrunk
            double progressHeader = clampedHeader / HeaderShrinkRange;
            double progressArtwork = clampedHeader / ArtworkShrinkRange;

            // --- Shrink the album art: 300x300 down to 100x100 ---
            double newHeaderSize = HeaderMaxSize - clampedHeader;
            AppBarHost.Height = newHeaderSize;

            double newArtworkSize = ArtworkMaxSize - clampedArtwork;
            ArtworkBorder.Width = newArtworkSize;
            ArtworkBorder.Height = newArtworkSize;

            // --- Fade the big title/artist out as the art shrinks ---
            //Title_TextBlock.Opacity = 1 - progress;
            //SubTitle_TextBlock.Opacity = 1 - progress;

            // --- Fade the fixed app bar's title in, and solidify its background ---
            //Title_TextBlock.Opacity = progress;

            //byte alpha = (byte)(progress * 255);

            //var brush = new AcrylicBrush();
            //brush.TintColor = Colors.Blue;
            //brush.TintOpacity = alpha;

            //AppBarHost.Background = brush;
        }

        private async void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (viewmodel == null) return;

            var songs = viewmodel.Items
                .Where(s => s.File != null)
                .Select(s => s.File)
                .ToList();

            await Player.SetQueueAsync(songs, 0, true);
        }

        private async void ShuffleButton_Click(object sender, RoutedEventArgs e)
        {
            if (viewmodel == null) return;

            var songs = viewmodel.Items
                .Where(s => s.File != null)
                .Select(s => s.File)
                .ToList();

            await Player.SetQueueAsync(songs, 0, false);
            Player.SetShuffle(true);
            Player.Play();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            DialogUtils.Show_EditPlaylistDialog(viewmodel);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            DialogUtils.Show_DeletePlaylistDialog(viewmodel);
        }

        private async void Adapter_ItemClickEvent(object sender, PlaylistItemViewModel e)
        {
            if (viewmodel == null || e == null || e.File == null) return;

            var songs = viewmodel.Items
                .Where(s => s.File != null)
                .Select(s => s.File)
                .ToList();

            int index = songs.FindIndex(x => x.Id == e.FileId);

            await Player.SetQueueAsync(songs, index);
        }
    }
}