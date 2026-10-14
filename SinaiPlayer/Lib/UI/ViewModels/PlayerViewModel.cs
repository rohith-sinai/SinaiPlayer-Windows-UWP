using SinaiPlayer.Lib.Helpers;
using SinaiPlayer.Lib.LastFM;
using SinaiPlayer.Lib.Models;
using SinaiPlayer.Lib.Services;
using SinaiPlayer.Lib.Thumbnails;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

namespace SinaiPlayer.Lib.UI.ViewModels
{
    public sealed partial class PlayerViewModel : BaseViewModel
    {
        public static PlayerViewModel Instance { get; } = new PlayerViewModel();

        public ObservableCollection<QueueViewModel> Queue { get; } = new ObservableCollection<QueueViewModel>();
        public ObservableCollection<MediaArtwork> Artworks = new ObservableCollection<MediaArtwork>();

        private bool isLoading;
        private int currentIndex;

        private PlayerViewModel() { }

        public bool IsLoading
        {
            get => isLoading;
            private set
            {
                isLoading = value;
                Notify();
            }
        }

        public int CurrentIndex
        {
            get => currentIndex;

            set
            {
                currentIndex = value;
                Notify();
                Notify(nameof(CurrentItem));
            }
        }

        public QueueViewModel CurrentItem
        {
            get
            {
                if (CurrentIndex < 0)
                    return null;

                if (CurrentIndex >= Queue.Count)
                {
                    return null;
                }

                return Queue[CurrentIndex];
            }
        }

        public bool IsReplacingQueue { get; private set; }

        public void ReplaceQueue(IList<MediaFile> list)
        {
            IsReplacingQueue = true;
            try
            {
                Queue.Clear();

                if (list != null && list.Count != 0)
                {
                    foreach (var item in list)
                    {
                        Queue.Add(new QueueViewModel(item));
                    }
                }
            }
            finally
            {
                IsReplacingQueue = false;
            }
        }

        public async Task LoadSlideshowArtworksAsync(string artistId, string artistName)
        {
            Artworks = new ObservableCollection<MediaArtwork>();
            IsLoading = true;

            try
            {
                var results = await LastFmService.Instance.FetchArtistMediaArtworksAsync(artistId, artistName);
                results.Shuffle();
                Artworks = new ObservableCollection<MediaArtwork>(results);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Reorders the global queue and keeps every queue consumer synchronized.
        /// </summary>
        public void ReorderQueueItem(int oldIndex, int newIndex)
        {
            if (oldIndex < 0 || oldIndex >= Queue.Count)
                return;

            if (newIndex < 0 || newIndex >= Queue.Count)
                return;

            MediaPlayerService.Instance.MoveQueueItem(
                Queue[oldIndex].Item,
                newIndex);
        }

        /// <summary>
        /// Reorders the global queue using the item reference.
        /// </summary>
        public void ReorderQueueItem(MediaFile item, int newIndex)
        {
            if (item == null)
                return;

            MediaPlayerService.Instance.MoveQueueItem(item, newIndex);
        }

    }
}
