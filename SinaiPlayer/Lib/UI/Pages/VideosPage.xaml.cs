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
    public sealed partial class VideosPage : Page
    {
        private readonly CollectionViewSource _groupedSource = new CollectionViewSource { IsSourceGrouped = true };

        private MediaPlayerService Player => MediaPlayerService.Instance;

        public VideosPage()
        {
            InitializeComponent();
            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;

            EmptyView_Title.Text = LocalizationHelper.GetString("EmptyViewTitle");
            EmptyView_SubTitle.Text = LocalizationHelper.Format("EmptyViewSubTitle", LocalizationHelper.GetString("Videos").ToLower());
            ChooseFoldersButton.Content = LocalizationHelper.GetString("ImportFolders");

            LargeTitle.Text = LocalizationHelper.GetString("Videos");
            SortComboBox.PlaceholderText = LocalizationHelper.GetString("SelectOrder");
            SortByTextBlock.Text = LocalizationHelper.GetString("SortBy") + ":";

            //ApplySortOrder(PreferenceUtils.GetVideosSortsOrder());
            //Init_SortOptionsAndShuffleButton_UI();

        }

        private void InitContent()
        {
            ContentView.Visibility = LibraryRepository.Videos.Count != 0 ?
              Visibility.Visible : Visibility.Collapsed;

            EmptyView.Visibility = LibraryRepository.Videos.Count == 0 ?
                Visibility.Visible : Visibility.Collapsed;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Rebuild the page after navigation/startup so data that was loaded
            // after the page constructor is reflected immediately.
            ApplySortOrder(PreferenceUtils.GetVideosSortsOrder());
            Init_SortOptionsAndShuffleButton_UI();
            InitContent();

            LibraryRepository.VideoItems.CollectionChanged -= Repository_CollectionChanged;
            LibraryRepository.VideoItems.CollectionChanged += Repository_CollectionChanged;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            LibraryRepository.VideoItems.CollectionChanged -= Repository_CollectionChanged;
        }

        private void Repository_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ApplySortOrder(PreferenceUtils.GetVideosSortsOrder());
            Init_SortOptionsAndShuffleButton_UI();
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
            var videos = LibraryRepository.VideoItems.ToList();

            if (option == SortOption.DateAdded)
            {
                // Stand-in signal: most recently modified song in the album.
                // See the note in SongsPage about a real DateAdded column later.
                var flat = videos
                    .OrderByDescending(vm => vm.Item.DateModified)
                    .ToList();

                RegularGridView.Items = flat;

                RegularGridView.Visibility = Visibility.Visible;
                GroupedGridView.Visibility = Visibility.Collapsed;
            }
            else
            {
                var groups = BuildGroups(videos, option);

                _groupedSource.Source = groups;

                GroupedGridView.Items = _groupedSource.View;
                GroupedGridView.ZoomedOutItems = _groupedSource.View.CollectionGroups;

                RegularGridView.Visibility = Visibility.Collapsed;
                GroupedGridView.Visibility = Visibility.Visible;
            }
        }

        private static List<GroupInfoCollection<VideoViewModel>> BuildGroups(List<VideoViewModel> videos, SortOption option)
        {
            Func<VideoViewModel, string> keySelector;
            Comparison<string> keyComparer;

            switch (option)
            {
                case SortOption.Title:
                default:
                    keySelector = vm => SortGroupingHelper.GetLetterKey(vm.Title);
                    keyComparer = SortGroupingHelper.CompareTextKeys;
                    break;
            }

            var groups = videos
                .GroupBy(keySelector)
                .Select(g => new GroupInfoCollection<VideoViewModel>(
                    g.Key,
                    g.OrderBy(vm => vm.Title, StringComparer.OrdinalIgnoreCase)))
                .ToList();

            groups.Sort((a, b) => keyComparer((string)a.Key, (string)b.Key));

            return groups;
        }

        private void Init_SortOptionsAndShuffleButton_UI()
        {
            var videos = LibraryRepository.VideoItems;
            ShuffleButtonTextBlock.Text = LocalizationHelper.GetString("ShuffleAll") + $" ({videos.Count})";

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

        private async void ShuffleAll_Button_Click(object sender, RoutedEventArgs e)
        {
            var videos = LibraryRepository.Videos;
            await Player.SetQueueAsync(videos, 0, false);
            Player.SetShuffle(true);
            await Player.NextAsync();
        }

        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            FolderManager.OpenFolderPicker(sender, e);
        }
    }
}
