using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls; 
using SinaiPlayer.Lib.UI.ViewModels; 
using System;
using System.Collections.Generic;  

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SinaiPlayer.Lib.UI.Lists
{
    public sealed partial class AlbumsRegularGridView : UserControl
    {

        public event EventHandler<AlbumViewModel> ItemClickEvent;

        public static readonly DependencyProperty ItemsProperty =
    DependencyProperty.Register(nameof(Items), typeof(List<AlbumViewModel>), typeof(AlbumsRegularGridView), new PropertyMetadata(new List<AlbumViewModel>()));

        public List<AlbumViewModel> Items
        {
            get => (List<AlbumViewModel>)GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }

        public static readonly DependencyProperty ShowYearProperty =
    DependencyProperty.Register(nameof(ShowYear), typeof(bool), typeof(AlbumsRegularGridView), new PropertyMetadata(false));

        public bool ShowYear
        {
            get => (bool)GetValue(ShowYearProperty);
            set => SetValue(ShowYearProperty, value);
        }


        public AlbumsRegularGridView()
        {
            InitializeComponent();
        }

        private void GridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is AlbumViewModel vm)
            {
                ItemClickEvent?.Invoke(sender, vm);
            }
        }
    }
}
