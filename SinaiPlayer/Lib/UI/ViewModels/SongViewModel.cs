using Windows.UI.Xaml;
using SinaiPlayer.Lib.Helpers;
using SinaiPlayer.Lib.Models;
using System;
using System.Linq;

namespace SinaiPlayer.Lib.UI.ViewModels
{
    public sealed partial class SongViewModel : BaseViewModel
    {
        public MediaFile Item { get; }

        public SongViewModel(MediaFile Song)
        {
            Item = Song;

            ScreenHelper.ScreenSizeChanged += ScreenSizeChanged;
        }

        private void ScreenSizeChanged(object sender, EventArgs e)
        {
            Notify(nameof(CombinedTitleVisibility));
            Notify(nameof(TitleVisibility));
            Notify(nameof(ArtistVisibility));
            Notify(nameof(AlbumVisibility));
            Notify(nameof(YearVisibility));
            Notify(nameof(GenreVisibility));
            Notify(nameof(GridColumnDefination2Width));
            Notify(nameof(GridColumnDefination3Width));
            Notify(nameof(GridColumnDefination4Width));
            Notify(nameof(GridColumnDefination5Width));
        }

        public string Id => Item.Id;

        public string Path => Item.Path;

        public string TrackNumber => Item.TrackNumber.ToString("D2") + ".";

        public string Title => string.IsNullOrEmpty(Item.Title) ? LocalizationHelper.Format("UnTitled", LocalizationHelper.GetString("Song").ToLower()) : Item.Title;

        public Visibility TitleVisibility =>
            ScreenHelper.FullWidth > 600 ? Visibility.Visible : Visibility.Collapsed;

        public Visibility CombinedTitleVisibility =>
            ScreenHelper.FullWidth < 600 ? Visibility.Visible : Visibility.Collapsed;

        public string SubTitle => $"{Artist} • {Album}";


        public string Artist => string.IsNullOrEmpty(Item.Artist) ? LocalizationHelper.GetString("UnknownArtist") : Item.Artist;

        public string ArtistId => Item.ArtistId;

        public string AlbumArtist => string.IsNullOrEmpty(Item.AlbumArtist) ? LocalizationHelper.GetString("UnknownArtist") : Item.AlbumArtist;


        public string AlbumArtistId => Item.AlbumArtistId;

        public string Album => string.IsNullOrEmpty(Item.Album) ? LocalizationHelper.GetString("UnknownAlbum") : Item.Album;

        public string AlbumId => Item.AlbumId;

        public string Year => Item.Year.ToString() == "0" ? "1990" : Item.Year.ToString();

        public string Genre => string.IsNullOrWhiteSpace(Item.Genres.FirstOrDefault()) ? LocalizationHelper.GetString("Unknown") : Item.Genres.FirstOrDefault();

        public string Duration => FormatHelper.FormatTimeToDisplayMillis(Item.Duration);


        // Visibilities of Grid Columns

        public Visibility ArtistVisibility =>
            ScreenHelper.FullWidth > 600 ? Visibility.Visible : Visibility.Collapsed;

        public Visibility AlbumVisibility =>
            ScreenHelper.FullWidth > 800 ? Visibility.Visible : Visibility.Collapsed;

        public Visibility YearVisibility =>
            ScreenHelper.FullWidth > 1200 ? Visibility.Visible : Visibility.Collapsed;

        public Visibility GenreVisibility =>
            ScreenHelper.FullWidth > 1400 ? Visibility.Visible : Visibility.Collapsed;

        public string GridColumnDefination2Width =>
            ScreenHelper.FullWidth > 600 ? "220" : "Auto";
        public string GridColumnDefination3Width =>
            ScreenHelper.FullWidth > 800 ? "220" : "Auto";        
        public string GridColumnDefination4Width =>
            ScreenHelper.FullWidth > 1200 ? "80" : "Auto";
        public string GridColumnDefination5Width =>
            ScreenHelper.FullWidth > 1400 ? "140" : "Auto";
    }
}
