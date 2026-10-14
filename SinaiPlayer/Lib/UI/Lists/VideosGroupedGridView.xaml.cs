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
    public sealed partial class VideosGroupedGridView : UserControl
    {
        private MediaPlayerService Player => MediaPlayerService.Instance;
        public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
nameof(Items), typeof(ICollectionView), typeof(VideosGroupedGridView),
new PropertyMetadata(null));

        public static readonly DependencyProperty ZoomedOutItemsProperty = DependencyProperty.Register(
    nameof(ZoomedOutItems), typeof(IObservableVector<object>), typeof(VideosGroupedGridView),
    new PropertyMetadata(null));

        public ICollectionView Items
        {
            get => (ICollectionView)GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }

        public IObservableVector<object> ZoomedOutItems
        {
            get => (IObservableVector<object>)GetValue(ZoomedOutItemsProperty);
            set => SetValue(ZoomedOutItemsProperty, value);
        }
        public VideosGroupedGridView()
        {
            InitializeComponent();
        } 

        private async void Adapter_ItemClickEvent(object sender, VideoViewModel vm)
        {
            if (vm is null)
                return;

            var video = vm.Item;

            if (video is null)
                return;

            if (Items?.CollectionGroups is null)
                return;

            var videos = Items.CollectionGroups
                    .Cast<ICollectionViewGroup>()
                    .SelectMany(g => g.GroupItems)
                    .OfType<VideoViewModel>()
                    .Where(s => s.Item != null)
                    .Select(s => s.Item)
                    .ToList();

            if (videos is null || videos.Count == 0)
                return;

            int index = videos.FindIndex(x => x.Id == video.Id);
            if (index < 0)
                return;
            await Player.SetQueueAsync(videos, index);
        }
    }
}
