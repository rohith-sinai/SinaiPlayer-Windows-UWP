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
using SinaiPlayer.Lib.Enums;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SinaiPlayer.Lib.UI.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AlbumsPage : Page
    {
        private readonly CollectionViewSource _groupedSource = new CollectionViewSource { IsSourceGrouped = true };

        public AlbumsPage()
        {
            InitializeComponent();
            Loaded += AlbumsPage_Loaded;
            //ApplySortOrder(PreferenceUtils.GetAlbumsSortOrder());
        }

        private void AlbumsPage_Loaded(object sender, RoutedEventArgs e)
        {
            // The page can be constructed before the library finishes populating.
            // Rebuild the view when the cached page becomes visible.
            ApplySortOrder(PreferenceUtils.GetAlbumsSortOrder());
        }

        public void ApplySortOrder(SortOption option)
        {
            var albums = LibraryRepository.AlbumItems.ToList();

            if (option == SortOption.DateAdded)
            {
                // Stand-in signal: most recently modified song in the album.
                // See the note in SongsPage about a real DateAdded column later.
                var flat = albums
                    .OrderByDescending(vm => vm.Item.Songs != null && vm.Item.Songs.Count > 0
                        ? vm.Item.Songs.Max(s => s.DateModified)
                        : DateTimeOffset.MinValue)
                    .ToList();

                RegularGridView.Items = flat;
                RegularGridView.ShowYear = false;

                // GroupedGridView shares the same Connected.ListItemKey; leaving its old
                // items behind while hidden could make the toolkit resolve the wrong
                // (invisible) container for the connected animation.
                GroupedGridView.Items = null;
                GroupedGridView.ZoomedOutItems = null;

                RegularGridView.Visibility = Visibility.Visible;
                GroupedGridView.Visibility = Visibility.Collapsed;
            }
            else
            {

                var groups = BuildGroups(albums, option);

                _groupedSource.Source = groups;

                GroupedGridView.Items = _groupedSource.View;
                GroupedGridView.ShowYear = option == SortOption.Artist;
                GroupedGridView.ZoomedOutItems = _groupedSource.View.CollectionGroups;

                // Same reasoning in reverse.
                RegularGridView.Items = new List<AlbumViewModel>();

                RegularGridView.Visibility = Visibility.Collapsed;
                GroupedGridView.Visibility = Visibility.Visible;
            }

        }

        private static List<GroupInfoCollection<AlbumViewModel>> BuildGroups(List<AlbumViewModel> albums, SortOption option)
        {
            Func<AlbumViewModel, string> keySelector;
            Comparison<string> keyComparer;

            switch (option)
            {
                case SortOption.Artist:
                    keySelector = vm => string.IsNullOrWhiteSpace(vm.Item.Artist) ? "Unknown Artist" : vm.Item.Artist;
                    keyComparer = (a, b) => string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
                    break;

                case SortOption.Year:
                    keySelector = vm => vm.Item.Year > 0 ? vm.Item.Year.ToString() : "1990";
                    keyComparer = SortGroupingHelper.CompareYearKeys;
                    break;

                case SortOption.Title:
                default:
                    keySelector = vm => SortGroupingHelper.GetLetterKey(vm.Item.Name);
                    keyComparer = SortGroupingHelper.CompareTextKeys;
                    break;
            }

            var groups = albums
                .GroupBy(keySelector)
                .Select(g => new GroupInfoCollection<AlbumViewModel>(
                    g.Key,
                    g.OrderBy(vm => vm.Item.Name, StringComparer.OrdinalIgnoreCase)))
                .ToList();

            groups.Sort((a, b) => keyComparer((string)a.Key, (string)b.Key));

            return groups;
        }

        private void GridView_ItemClickEvent(object sender, AlbumViewModel item)
        {
            NavigationUtils.Navigate_AlbumDetailsPage(item.Id);
        }
    }
}
