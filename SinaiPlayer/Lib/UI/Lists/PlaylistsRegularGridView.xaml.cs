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
    public sealed partial class PlaylistsRegularGridView : UserControl
    {
        public event EventHandler<PlaylistViewModel> ItemClickEvent;

        public static readonly DependencyProperty ItemsProperty =
    DependencyProperty.Register(nameof(Items), typeof(List<PlaylistViewModel>), typeof(PlaylistsRegularGridView), new PropertyMetadata(new List<PlaylistViewModel>()));

        public List<PlaylistViewModel> Items
        {
            get => (List<PlaylistViewModel>)GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }

        public PlaylistsRegularGridView()
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
