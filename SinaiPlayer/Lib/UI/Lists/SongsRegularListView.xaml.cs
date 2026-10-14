using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using SinaiPlayer.Lib.Models;
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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SinaiPlayer.Lib.UI.Lists
{
    public sealed partial class SongsRegularListView : UserControl
    {
        private MediaPlayerService Player => MediaPlayerService.Instance;

        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register(nameof(Items), typeof(List<SongViewModel>), typeof(SongsRegularListView), new PropertyMetadata(new List<SongViewModel>()));

        public List<SongViewModel> Items
        {
            get => (List<SongViewModel>)GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }

        public SongsRegularListView()
        {
            InitializeComponent();
        }
        private async void SongAdapter_ItemClickEvent(object sender, SongViewModel vm)
        {
            if (vm is null)
                return;

            var song = vm.Item;

            if (song is null)
                return;

            var songs = Items
                    .Where(s => s.Item != null)
                    .Select(s => s.Item)
                    .ToList();

            if (songs is null)
                return;

            int index = songs.FindIndex(x => x.Id == song.Id);

            if (index < 0)
                return;

            await Player.SetQueueAsync(songs, index);
        }
    }
}