using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
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
    public sealed partial class PlaylistsGroupedGridView : UserControl
    {

        public event EventHandler<PlaylistViewModel> ItemClickEvent;

        public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
nameof(Items), typeof(ICollectionView), typeof(PlaylistsGroupedGridView),
new PropertyMetadata(null));

        public static readonly DependencyProperty ZoomedOutItemsProperty = DependencyProperty.Register(
    nameof(ZoomedOutItems), typeof(IObservableVector<object>), typeof(PlaylistsGroupedGridView),
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
        public PlaylistsGroupedGridView()
        {
            InitializeComponent();

            mGridView.ContainerContentChanging += GridView_ContainerContentChanging;
        }

        private async void GridView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.InRecycleQueue)
                return;

            if (args.Item is PlaylistViewModel vm)
            {
                await vm.EnsureArtworkLoadedAsync();
            }
        }

        private void GridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is PlaylistViewModel vm)
            {
                ItemClickEvent?.Invoke(sender, vm);
            }
        }
    }
}
