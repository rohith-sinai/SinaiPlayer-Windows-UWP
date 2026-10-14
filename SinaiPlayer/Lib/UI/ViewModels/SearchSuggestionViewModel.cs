using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;
using SinaiPlayer.Lib.Models;
using SinaiPlayer.Lib.Thumbnails;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SinaiPlayer.Lib.UI.ViewModels
{
    public sealed partial class SearchSuggestionViewModel : BaseViewModel
    {
        public SearchSuggestionItem Item { get; }

        public SearchSuggestionViewModel(SearchSuggestionItem item)
        {
            this.Item = item;
        }

        public string Title => Item.Title;

        public string SubTitle => Item.Type == Enums.SearchSuggestionType.Album || Item.Type == Enums.SearchSuggestionType.Song ? string.IsNullOrEmpty(Item.Artist) ? Item.Type.ToString() : Item.Type.ToString() + " • " + Item.Artist : Item.Type.ToString();

        public string Id => Item.Id;

        public Enums.SearchSuggestionType Type => Item.Type;

        public Visibility DefaultArtworkVisibility => Item.Type != Enums.SearchSuggestionType.Artist ? Visibility.Visible : Visibility.Collapsed;

        public Visibility ArtistArtworkVisibility => Item.Type == Enums.SearchSuggestionType.Artist ? Visibility.Visible : Visibility.Collapsed;

        public int ArtworkHeight => Item.Type == Enums.SearchSuggestionType.Video ? 20 : 40;
        public int ArtworkWidth => 40;

        private BitmapImage image = null;

        public BitmapImage Image
        {
            get => image;
            set
            {
                image = value;
                Notify();
            }
        }

        public async Task LoadArtworkAsync()
        {
            if (Item == null) return;

            switch (Item.Type)
            {
                case Enums.SearchSuggestionType.Song:
                case Enums.SearchSuggestionType.Album:
                    await LoadSongArtworkAsync();
                    break;
                case Enums.SearchSuggestionType.Artist:
                    await LoadArtistArtworkAsync();
                    break;
                case Enums.SearchSuggestionType.Video:
                    await LoadVideoArtworkAsync();
                    break;
            }
        }

        private async Task LoadSongArtworkAsync()
        {
            var request = new SongThumbnailRequest(Item.AlbumId, Item.Path);

            var art = await request.LoadAsync();

            if (art == null) return;

            Image = art.Image;
        }

        private async Task LoadArtistArtworkAsync()
        {
            var request = new ArtistThumbnailRequest(Item.ArtistId, Item.Artist);

            var art = await request.LoadAsync();

            if (art == null) return;

            Image = art.Image;
        }

        private async Task LoadVideoArtworkAsync()
        {
            var request = new VideoThumbnailRequest(Item.Id, Item.Path);

            var art = await request.LoadAsync();

            if (art == null) return;

            Image = art.Image;
        }
    }
}
