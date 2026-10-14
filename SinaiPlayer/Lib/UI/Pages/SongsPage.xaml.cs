using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using SinaiPlayer.Lib.Enums;
using SinaiPlayer.Lib.Helpers;
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

namespace SinaiPlayer.Lib.UI.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SongsPage : Page
    {
        private readonly CollectionViewSource _groupedSource = new CollectionViewSource { IsSourceGrouped = true };

        public SongsPage()
        {
            InitializeComponent();
            Loaded += SongsPage_Loaded;
            //ApplySortOrder(PreferenceUtils.GetSongsSortOrder());
        }

        private void SongsPage_Loaded(object sender, RoutedEventArgs e)
        {
            // The page can be constructed before the library finishes populating.
            // Rebuild the view when the cached page becomes visible.
            ApplySortOrder(PreferenceUtils.GetSongsSortOrder());
        }


        public void ApplySortOrder(SortOption option)
        {
            var songs = LibraryRepository.SongItems.ToList();

            if (option == SortOption.DateAdded)
            {
                // NOTE: using DateModified as a stand-in for "date added to library"
                // for now - true first-seen tracking would need a small DB addition
                // (a DateAdded column preserved across re-syncs). Happy to add that
                // if you want an exact "recently added" feel later.
                var flat = songs
                    .OrderByDescending(vm => vm.Item.DateModified)
                    .ToList();

                RegularListView.Items = flat;

                RegularListView.Visibility = Visibility.Visible;
                GroupedListView.Visibility = Visibility.Collapsed;
            }
            else
            {
                var groups = BuildGroups(songs, option);

                _groupedSource.Source = groups;

                GroupedListView.Items = _groupedSource.View;
                GroupedListView.ZoomedOutItems = _groupedSource.View.CollectionGroups;

                RegularListView.Visibility = Visibility.Collapsed;
                GroupedListView.Visibility = Visibility.Visible;
            }
        }

        private static List<GroupInfoCollection<SongViewModel>> BuildGroups(List<SongViewModel> songs, SortOption option)
        {
            Func<SongViewModel, string> keySelector;
            Comparison<string> keyComparer;

            switch (option)
            {
                case SortOption.Artist:
                    keySelector = vm => string.IsNullOrWhiteSpace(vm.Item.Artist) ? "Unknown Artist" : vm.Item.Artist;
                    keyComparer = (a, b) => string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
                    break;

                case SortOption.Album:
                    keySelector = vm => string.IsNullOrWhiteSpace(vm.Item.Album) ? "Unknown Album" : vm.Item.Album;
                    keyComparer = (a, b) => string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
                    break;

                case SortOption.Title:
                default:
                    keySelector = vm => SortGroupingHelper.GetLetterKey(vm.Item.Title);
                    keyComparer = SortGroupingHelper.CompareTextKeys;
                    break;
            }

            var groups = songs
                .GroupBy(keySelector)
                .Select(g => new GroupInfoCollection<SongViewModel>(
                    g.Key,
                    g.OrderBy(vm => vm.Item.Title, StringComparer.OrdinalIgnoreCase)))
                .ToList();

            groups.Sort((a, b) => keyComparer((string)a.Key, (string)b.Key));

            return groups;
        }
    }
}
