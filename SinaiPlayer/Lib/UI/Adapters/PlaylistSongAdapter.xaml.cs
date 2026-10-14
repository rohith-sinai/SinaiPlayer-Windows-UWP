using SinaiPlayer.Lib.Models;
using SinaiPlayer.Lib.Repos;
using SinaiPlayer.Lib.Services;
using SinaiPlayer.Lib.UI.ViewModels;
using SinaiPlayer.Lib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input; 

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SinaiPlayer.Lib.UI.Adapters
{
    public sealed partial class PlaylistSongAdapter : UserControl
    {
        public event EventHandler<PlaylistItemViewModel> ItemClickEvent;

        private MediaPlayerService Player => MediaPlayerService.Instance;

        public PlaylistSongAdapter()
        {
            InitializeComponent();
            this.DataContextChanged += Adapter_DataContextChanged;
        }
        private async void Adapter_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            Bindings.Update();
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
            if (sender is FrameworkElement element && element.DataContext is PlaylistItemViewModel clickedItem)
            {
                ItemClickEvent?.Invoke(sender, clickedItem);
            }
        }

        private async void Play_HoverButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is PlaylistItemViewModel vm)
            {
                var file = vm.File;
                List<MediaFile> queue = new List<MediaFile> { file };
                if(file != null) await Player.SetQueueAsync(queue);
            }
        }

        private void RemoveFromPlaylist_HoverButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is PlaylistItemViewModel vm)
            {
                var file = vm.File;

                if (file is null)
                    return;

                var playlistviewmodel = PlaylistRepository.Playlists.First(x => x.Id == vm.Item.PlaylistId);

                DialogUtils.Show_RemoveFileFromPlaylistDialog(playlistviewmodel, vm);
            }
        }
    }
}
