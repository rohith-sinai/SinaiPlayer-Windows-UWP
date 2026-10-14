using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using SinaiPlayer.Lib.Helpers;
using SinaiPlayer.Lib.Repos;
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

namespace SinaiPlayer.Lib.UI.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ArtistsPage : Page
    {
        private readonly CollectionViewSource _groupedSource = new CollectionViewSource { IsSourceGrouped = true };

        public ArtistsPage()
        {
            InitializeComponent();

            Loaded += ArtistsPage_Loaded;
            //Init_GridView();
            GridViewContainer.ContainerContentChanging += GridView_ContainerContentChanging;
        }

        private void ArtistsPage_Loaded(object sender, RoutedEventArgs e)
        {
            // The page can be constructed before the library finishes populating.
            // Rebuild the view when the cached page becomes visible.
            Init_GridView();
        }

        public void Init_GridView()
        {
            var artists = LibraryRepository.ArtistItems.ToList();

            var groups = artists
                .GroupBy(vm => SortGroupingHelper.GetLetterKey(vm.Item.Name))
                .Select(g => new GroupInfoCollection<ArtistViewModel>(
                    g.Key,
                    g.OrderBy(vm => vm.Item.Name, StringComparer.OrdinalIgnoreCase)))
                .ToList();

            groups.Sort((a, b) => SortGroupingHelper.CompareTextKeys((string)a.Key, (string)b.Key));

            _groupedSource.Source = groups;

            GridViewContainer.ItemsSource = _groupedSource.View;
            ZoomedOutListView.ItemsSource = _groupedSource.View.CollectionGroups;
        }

        private async void GridView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.InRecycleQueue)
                return;

            if (args.Item is ArtistViewModel vm)
            {
                await vm.EnsureArtworkLoadedAsync();
            }
        }

        private void GridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is ArtistViewModel vm)
            {
                NavigationUtils.Navigate_ArtistDetailsPage(vm.Id);
            }
        }
    }
}
