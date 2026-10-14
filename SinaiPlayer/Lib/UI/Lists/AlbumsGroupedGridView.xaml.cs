using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls; 
using Windows.UI.Xaml.Data; 
using SinaiPlayer.Lib.UI.ViewModels; 
using System; 
using Windows.Foundation.Collections; 

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SinaiPlayer.Lib.UI.Lists
{
    public sealed partial class AlbumsGroupedGridView : UserControl
    {
        public event EventHandler<AlbumViewModel> ItemClickEvent;

        public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
nameof(Items), typeof(ICollectionView), typeof(AlbumsGroupedGridView),
new PropertyMetadata(null));

        public static readonly DependencyProperty ZoomedOutItemsProperty = DependencyProperty.Register(
    nameof(ZoomedOutItems), typeof(IObservableVector<object>), typeof(AlbumsGroupedGridView),
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

        public static readonly DependencyProperty ShowYearProperty =
    DependencyProperty.Register(nameof(ShowYear), typeof(bool), typeof(AlbumsGroupedGridView), new PropertyMetadata(false));

        public bool ShowYear
        {
            get => (bool)GetValue(ShowYearProperty);
            set => SetValue(ShowYearProperty, value);
        }

        public AlbumsGroupedGridView()
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
