using CommunityToolkit.WinUI;
using Microsoft.UI;
using SinaiPlayer.Lib.Enums;
using SinaiPlayer.Lib.Helpers;
using SinaiPlayer.Lib.Models;
using SinaiPlayer.Lib.Repos;
using SinaiPlayer.Lib.Services;
using SinaiPlayer.Lib.UI.ViewModels;
using SinaiPlayer.Lib.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SinaiPlayer.Lib.UI.Pages.Player
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class QueuePage : Page
    {
        private MediaPlayerService Player => MediaPlayerService.Instance;

        private bool _syncing;

        public ObservableCollection<QueueViewModel> QueueItems { get; } =
            new ObservableCollection<QueueViewModel>();

        public QueuePage()
        {
            InitializeComponent();
            QueueListHeaderTextBlock.Text = LocalizationHelper.GetString("QueueList");
            Loaded += QueuePage_Loaded;
            Unloaded += QueuePage_Unloaded;
            RefreshAllUI();
        }

        private void QueuePage_Loaded(object sender, RoutedEventArgs e)
        {
            Player.QueueChanged -= Player_QueueChanged;
            Player.QueueChanged += Player_QueueChanged;
            Player.PropertyChanged -= Player_PropertyChanged;
            Player.PropertyChanged += Player_PropertyChanged;
        }

        private void QueuePage_Unloaded(object sender, RoutedEventArgs e)
        {
            Player.QueueChanged -= Player_QueueChanged;
            Player.PropertyChanged -= Player_PropertyChanged;
        }

        private void Player_QueueChanged(object sender, EventArgs e)
        {
            Dispatcher.RunAsync(CoreDispatcherPriority.Normal, RefreshQueue);
        }

        private void Player_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Player.IsShuffled):
                    UpdateShuffleButton();
                    break;

                case nameof(Player.RepeatMode):
                    UpdateRepeatButton();
                    break;

                case nameof(Player.CurrentIndex):
                    UpdateCurrentIndex();
                    break;

                case nameof(Player.Queue):
                    RefreshQueue();
                    break;
            }
        }

        private void RefreshAllUI()
        {
            RefreshQueue();
            UpdateShuffleButton();
            UpdateRepeatButton();
        }

        private void RefreshQueue()
        {
            if (_syncing)
                return;

            _syncing = true;
            try
            {
                QueueItems.Clear();
                foreach (var item in Player.Queue)
                    QueueItems.Add(new QueueViewModel(item));
                QueueListView.ItemsSource = QueueItems.ToList();
            }
            finally
            {
                _syncing = false;
                UpdateCurrentIndex();
            }
        }

        private async void QueueListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var queueItem = e.ClickedItem as QueueViewModel;
            if (queueItem?.Item == null)
                return;

            await Player.PlayFileAsync(queueItem.Item);
        }

        private void QueueListView_DragItemsCompleted(
            ListViewBase sender,
            DragItemsCompletedEventArgs args)
        {
            if (_syncing || args.Items == null || args.Items.Count == 0)
                return;

            var movedItem = args.Items[0] as QueueViewModel;
            if (movedItem == null)
                return;

            int newIndex = QueueItems.IndexOf(movedItem);
            if (newIndex < 0)
                return;

            // The ListView has already moved its local visual item.
            // Commit the reorder to the GLOBAL queue through QueueViewModel /
            // PlayerViewModel. QueueChanged then refreshes every queue consumer.
            QueueViewModel.ReorderItem(movedItem.Item, newIndex);
        }

        private void UpdateShuffleButton()
        {
            if (Player.IsShuffled)
            {
                ShuffleIcon.Opacity = 1;
                ShuffleDisabledLine.Opacity = 0;
            }
            else
            {
                ShuffleIcon.Opacity = 0.5;
                ShuffleDisabledLine.Opacity = 0.5;
            }
        }

        private void UpdateRepeatButton()
        {
            switch (Player.RepeatMode)
            {
                // repeat none
                case RepeatModeType.None:
                    RepeatIcon.Glyph = "\uE8EE";
                    RepeatIcon.Opacity = 0.5;
                    RepeatDisabledLine.Opacity = 0.5;
                    break;
                // repeat all
                case RepeatModeType.All:
                    RepeatIcon.Glyph = "\uE8EE";
                    RepeatIcon.Opacity = 1;
                    RepeatDisabledLine.Opacity = 0;
                    break;
                // repeat one
                case RepeatModeType.One:
                    RepeatIcon.Glyph = "\uE8ED";
                    RepeatIcon.Opacity = 1;
                    RepeatDisabledLine.Opacity = 0;
                    break;
            }
        }

        private async void UpdateCurrentIndex()
        {
            int index = Player.CurrentIndex;

            if (index > -1)
            {
                if (index + 1 == QueueItems.ToList().Count)
                    await QueueListView.SmoothScrollIntoViewWithIndexAsync(index);
                else
                    await QueueListView.SmoothScrollIntoViewWithIndexAsync(index + 1);
            }

        }

        private void RepeatButton_Click(object sender, RoutedEventArgs e)
        {
            Player.CycleRepeatMode();
        }

        private void ShuffleButton_Click(object sender, RoutedEventArgs e)
        {
            Player.ToggleShuffle();
        }
    }
}