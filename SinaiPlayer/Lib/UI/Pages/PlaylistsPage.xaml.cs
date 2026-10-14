using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using SinaiPlayer.Lib.Dialogs;
using SinaiPlayer.Lib.Enums;
using SinaiPlayer.Lib.Helpers;
using SinaiPlayer.Lib.Repos;
using SinaiPlayer.Lib.UI.ViewModels;
using SinaiPlayer.Lib.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
    public sealed partial class PlaylistsPage : Page
    {
        private readonly CollectionViewSource _groupedSource = new CollectionViewSource { IsSourceGrouped = true };

        public PlaylistsPage()
        {
            InitializeComponent();
            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;
            
            EmptyView_Title.Text = LocalizationHelper.GetString("EmptyPlaylistViewTitle");
            EmptyView_SubTitle.Text = LocalizationHelper.GetString("EmptyPlaylistViewSubTitle");
            NewPlaylistFromEmptyViewButton.Content = LocalizationHelper.GetString("NewPlaylist");

            LargeTitle.Text = LocalizationHelper.GetString("Playlists");
            NewPlaylistTextBlock.Text = LocalizationHelper.GetString("NewPlaylist");
            SortByTextBlock.Text = LocalizationHelper.GetString("SortBy") + ":";
            SortComboBox.PlaceholderText = LocalizationHelper.GetString("SelectOrder");

           // ApplySortOrder(PreferenceUtils.GetPlaylistsSortsOrder());
           // Init_SortOptions();
        }
        private void InitContent()
        {
            ContentView.Visibility = PlaylistRepository.Playlists.Count != 0 ?
              Visibility.Visible : Visibility.Collapsed;

            EmptyView.Visibility = PlaylistRepository.Playlists.Count == 0 ?
                Visibility.Visible : Visibility.Collapsed;
        }
        private void Init_SortOptions()
        {

            List<SortOptionItem> options;

            SortOption current;
            current = PreferenceUtils.GetVideosSortsOrder();

            options = new List<SortOptionItem>
                    {
                        new SortOptionItem(SortOption.DateAdded, LocalizationHelper.GetString("DateAdded")),
                         new SortOptionItem(SortOption.Title, LocalizationHelper.GetString("Title")),
                    };

            SortComboBox.ItemsSource = options;

            int index = options.FindIndex(o => o.Value == current);
            SortComboBox.SelectedIndex = index >= 0 ? index : 0;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Rebuild the page after navigation/startup so data that was loaded
            // after the page constructor is reflected immediately.
            ApplySortOrder(PreferenceUtils.GetPlaylistsSortsOrder());
            Init_SortOptions();
            InitContent();

            PlaylistRepository.Playlists.CollectionChanged -= Repository_CollectionChanged;
            PlaylistRepository.Playlists.CollectionChanged += Repository_CollectionChanged;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            PlaylistRepository.Playlists.CollectionChanged -= Repository_CollectionChanged;
        }

        private void Repository_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ApplySortOrder(PreferenceUtils.GetPlaylistsSortsOrder());
            Init_SortOptions(); 
            InitContent();
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(SortComboBox.SelectedItem is SortOptionItem selected))
                return;

            PreferenceUtils.SetVideosSortOrder(selected.Value);
            ApplySortOrder(selected.Value);
        }

        public void ApplySortOrder(SortOption option)
        {
            var playlists = PlaylistRepository.Playlists.ToList();

            if (option == SortOption.DateAdded)
            {
                var flat = playlists
                    .OrderByDescending(vm => vm.Item.DateModified)
                    .ToList();

                RegularGridView.Items = flat;

                RegularGridView.Visibility = Visibility.Visible;
                GroupedGridView.Visibility = Visibility.Collapsed;
            }
            else
            {
                var groups = BuildGroups(playlists, option);

                _groupedSource.Source = groups;

                GroupedGridView.Items = _groupedSource.View;
                GroupedGridView.ZoomedOutItems = _groupedSource.View.CollectionGroups;

                RegularGridView.Visibility = Visibility.Collapsed;
                GroupedGridView.Visibility = Visibility.Visible;
            }
        }

        private static List<GroupInfoCollection<PlaylistViewModel>> BuildGroups(List<PlaylistViewModel> videos, SortOption option)
        {
            Func<PlaylistViewModel, string> keySelector;
            Comparison<string> keyComparer;

            switch (option)
            {
                case SortOption.Title:
                default:
                    keySelector = vm => SortGroupingHelper.GetLetterKey(vm.Name);
                    keyComparer = SortGroupingHelper.CompareTextKeys;
                    break;
            }

            var groups = videos
                .GroupBy(keySelector)
                .Select(g => new GroupInfoCollection<PlaylistViewModel>(
                    g.Key,
                    g.OrderBy(vm => vm.Name, StringComparer.OrdinalIgnoreCase)))
                .ToList();

            groups.Sort((a, b) => keyComparer((string)a.Key, (string)b.Key));

            return groups;
        }

        private void NewPlaylist_Button_Click(object sender, RoutedEventArgs e)
        {
            DialogUtils.Show_NewPlaylistDialog();
        }

        private void GridView_ItemClickEvent(object sender, PlaylistViewModel item)
        {
            NavigationUtils.Navigate_PlaylistDetailsPage(item.Id);
        }
    }
}
