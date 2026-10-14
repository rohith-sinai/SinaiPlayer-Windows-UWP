using Windows.UI.Xaml.Controls; 
using SinaiPlayer.Lib.Repos;
using SinaiPlayer.Lib.UI.ViewModels; 
using System.Collections.ObjectModel;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SinaiPlayer.Lib.Dialogs
{
    public sealed partial class AddMediaFilesToPlaylistDailogContent : UserControl
    {
        public ObservableCollection<PlaylistViewModel> SelectedItems { get; private set; } 
            = new ObservableCollection<PlaylistViewModel>();

        public AddMediaFilesToPlaylistDailogContent()
        {
            this.InitializeComponent();

            var playlists = PlaylistRepository.Playlists.ToList();
            Playlists_ListView.ItemsSource = playlists;
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Clear previous selections
            SelectedItems.Clear();

            // Add the newly selected items casted to your model
            foreach (var item in Playlists_ListView.SelectedItems.Cast<PlaylistViewModel>())
            {
                SelectedItems.Add(item);
            }
        }
    }
}
