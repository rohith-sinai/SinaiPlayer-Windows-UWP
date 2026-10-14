using SinaiPlayer.Lib.LastFM;
using SinaiPlayer.Lib.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SinaiPlayer.Lib.UI.ViewModels
{
    public sealed partial class ArtistSlideViewModel : BaseViewModel
    {
        private ObservableCollection<MediaArtwork> items = new ObservableCollection<MediaArtwork>();

        public ObservableCollection<MediaArtwork> Items
        {
            get => items;
            private set
            {
                items = value;
                Notify();
            }
        }

        private bool isLoading;

        public bool IsLoading
        {
            get => isLoading;
            private set
            {
                isLoading = value;
                Notify();
            }
        }

        public async Task LoadAsync(string artistId, string artistName)
        {
            Items = new ObservableCollection<MediaArtwork>();
            IsLoading = true;

            try
            {
                var results = await LastFmService.Instance.FetchArtistMediaArtworksAsync(artistId, artistName);
                 
                Items = new ObservableCollection<MediaArtwork>(results);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
