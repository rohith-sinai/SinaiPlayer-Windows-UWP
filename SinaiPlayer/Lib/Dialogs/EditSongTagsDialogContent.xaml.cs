using SinaiPlayer.Lib.Helpers;
using SinaiPlayer.Lib.Models;
using System;
using System.Linq;
using Windows.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SinaiPlayer.Lib.Dialogs
{
    public sealed partial class EditSongTagsDialogContent : UserControl
    {

        private readonly MediaFile song;

        // Original values are retained so we can determine whether
        // the user actually changed anything.
        private readonly uint originalTrackNumber;
        private readonly string originalTitle;
        private readonly string originalArtist;
        private readonly string originalAlbum;
        private readonly string originalAlbumArtist;
        private readonly uint originalYear;
        private readonly string originalGenre;

        public EditSongTagsDialogContent(MediaFile item)
        {
            this.InitializeComponent();

            SongNumberTextBlock.Text = LocalizationHelper.GetString("Position");
            TitleTextBlock.Text = LocalizationHelper.GetString("Title");
            SongArtistTextBlock.Text = LocalizationHelper.GetString("SongArtist");
            AlbumTextBlock.Text = LocalizationHelper.GetString("Album");
            AlbumArtistTextBlock.Text = LocalizationHelper.GetString("AlbumArtist");
            ReleaseYearTextBlock.Text = LocalizationHelper.GetString("ReleaseYear");
            GenreTextBlock.Text = LocalizationHelper.GetString("Genre");

            song = item ?? throw new ArgumentNullException(nameof(item));

            originalTrackNumber = item.TrackNumber;
            originalTitle = Normalize(item.Title);
            originalArtist = Normalize(item.Artist);
            originalAlbum = Normalize(item.Album);
            originalAlbumArtist = Normalize(item.AlbumArtist);
            originalYear = item.Year;
            originalGenre = Normalize(item.Genres?.FirstOrDefault());

            TrackNumber.Text =
                originalTrackNumber > 0
                    ? originalTrackNumber.ToString()
                    : string.Empty;

            Title.Text = originalTitle;
            Artist.Text = originalArtist;
            Album.Text = originalAlbum;
            AlbumArtist.Text = originalAlbumArtist;

            Year.Text =
                originalYear > 0
                    ? originalYear.ToString()
                    : string.Empty;

            Genre.Text = originalGenre;
        }

        public MediaFile Song => song;

        public int TrackNumberValue
        {
            get
            {
                if (int.TryParse(TrackNumber.Text?.Trim(), out int value))
                    return Math.Max(0, value);

                return 0;
            }
        }

        public string TitleValue => Normalize(Title.Text);
        public string ArtistValue => Normalize(Artist.Text);
        public string AlbumValue => Normalize(Album.Text);
        public string AlbumArtistValue => Normalize(AlbumArtist.Text);
        public string GenreValue => Normalize(Genre.Text);

        public int YearValue
        {
            get
            {
                if (int.TryParse(Year.Text?.Trim(), out int value))
                    return Math.Max(0, value);

                return 0;
            }
        }

        /// <summary>
        /// True when at least one editable tag is different from
        /// the value with which the dialog was opened.
        /// </summary>
        public bool HasChanges
        {
            get
            {
                return
                    TrackNumberValue != originalTrackNumber ||
                    !string.Equals(TitleValue, originalTitle, StringComparison.Ordinal) ||
                    !string.Equals(ArtistValue, originalArtist, StringComparison.Ordinal) ||
                    !string.Equals(AlbumValue, originalAlbum, StringComparison.Ordinal) ||
                    !string.Equals(
                        AlbumArtistValue,
                        originalAlbumArtist,
                        StringComparison.Ordinal) ||
                    YearValue != originalYear ||
                    !string.Equals(GenreValue, originalGenre, StringComparison.Ordinal);
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
        }
    }
}
