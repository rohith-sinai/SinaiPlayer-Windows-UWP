using Windows.UI.Xaml;
using SinaiPlayer.Lib.Enums;
using SinaiPlayer.Lib.Helpers;
using SinaiPlayer.Lib.Models;
using System;
using System.Linq;

namespace SinaiPlayer.Lib.UI.ViewModels
{
    public sealed partial class PlaylistItemViewModel : BaseViewModel
    {
        public PlaylistItem Item { get; }

        public PlaylistItemViewModel(PlaylistItem item)
        {
            Item = item;

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

        // RowId is the stable handle for removing/reordering this exact entry -
        // Path alone isn't unique since a file can appear twice in one playlist.
        public string PlaylistId => Item.PlaylistId;

        public int RowId => Item.RowId;

        public int Position => Item.Position;

        public string Path => Item.Path;

        public MediaFile File => Item.File;

        public string FileId => File is null ? Guid.NewGuid().ToString() : File.Id;

        public string FilePath => File is null ? "unknown" : File.Path;

        public string AlbumId => File is null ? "unknown" : File.AlbumId;

        public string AlbumArtistId => File is null ? "unknown" : File.AlbumArtistId;

        public Enums.MediaType FileType => File is null ? MediaType.Unknown : File.Type;

        public bool IsVideo => File is null ? false : Item.IsVideo;

        public bool IsMissing => File is null ? true : Item.IsMissing;

        public string Title => File != null ? string.IsNullOrEmpty(File.Title) ? LocalizationHelper.Format("UnTitled", LocalizationHelper.GetString("Item").ToLower()) : File.Title : LocalizationHelper.GetString("DeletedFile");

        public Visibility TitleVisibility =>
            ScreenHelper.FullWidth > 600 ? Visibility.Visible : Visibility.Collapsed;

        public Visibility CombinedTitleVisibility =>
            ScreenHelper.FullWidth < 600 ? Visibility.Visible : Visibility.Collapsed;

        public string SubTitle =>  $"{Artist} • {Album}";

        public string Artist => File != null ? string.IsNullOrEmpty(File.Artist) ? LocalizationHelper.GetString("UnknownArtist") : File.Artist : LocalizationHelper.GetString("UnknownArtist"); 

        public string Album => File != null ? string.IsNullOrEmpty(File.Album) ? LocalizationHelper.GetString("UnknownAlbum") : File.Album : LocalizationHelper.GetString("UnknownAlbum"); 


        public string Year => File != null ? File.Year.ToString() == "0" ? "1990" : File.Year.ToString() : "1990";

        public string Genre => File != null 
            ? string.IsNullOrWhiteSpace(File.Genres.FirstOrDefault()) ? LocalizationHelper.GetString("Unknown") : File.Genres.FirstOrDefault() : LocalizationHelper.GetString("Unknown");

        public string Duration => File != null ? FormatHelper.FormatTimeToDisplayMillis(File.Duration) : "00:00";
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
            ScreenHelper.FullWidth > 600 ? "*" : "Auto";
        public string GridColumnDefination3Width =>
            ScreenHelper.FullWidth > 800 ? "*" : "Auto";
        public string GridColumnDefination4Width =>
            ScreenHelper.FullWidth > 1200 ? "80" : "Auto";
        public string GridColumnDefination5Width =>
            ScreenHelper.FullWidth > 1400 ? "140" : "Auto";
    }
}
