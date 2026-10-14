using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls; 
using Windows.UI.Xaml.Media.Imaging; 
using SinaiPlayer.Lib.Models;
using System; 
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks; 
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Storage.Pickers;
using SinaiPlayer.Lib.Helpers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SinaiPlayer.Lib.Dialogs
{
    public sealed partial class EditAlbumTagsDialogContent : UserControl
    {
        private readonly Album album;
        private byte[] selectedArtworkBytes;
        private BitmapImage selectedArtworkImage;
        private readonly string originalName;
        private readonly string originalArtist;
        private readonly uint originalYear;

        public EditAlbumTagsDialogContent(Album item)
        {
            this.InitializeComponent();

            AlbumTitleTextBlock.Text = LocalizationHelper.GetString("Title");
            AlbumArtistTextBlock.Text = LocalizationHelper.GetString("Artist");
            ReleaseYearTextBlock.Text = LocalizationHelper.GetString("ReleaseYear");


            album = item ?? throw new ArgumentNullException(nameof(item));

            originalName = Normalize(item.Name);
            originalArtist = Normalize(item.Artist);
            originalYear = item.Year;

            AlbumTitle.Text = originalName;
            AlbumArtist.Text = originalArtist;

            AlbumReleaseYear.Text =
                originalYear > 0
                    ? originalYear.ToString()
                    : string.Empty;
        }

        public Album Album => album;

        public string AlbumName
        {
            get
            {
                return Normalize(AlbumTitle.Text);
            }
        }

        public string AlbumArtistName
        {
            get
            {
                return Normalize(AlbumArtist.Text);
            }
        }

        public int ReleaseYear
        {
            get
            {
                if (int.TryParse(
                    AlbumReleaseYear.Text?.Trim(),
                    out int value))
                {
                    return Math.Max(0, value);
                }

                return 0;
            }
        }

        public bool HasArtworkChange =>
            selectedArtworkBytes != null &&
            selectedArtworkBytes.Length > 0;

        public bool HasMetadataChanges
        {
            get
            {
                return
                    !string.Equals(
                        AlbumName,
                        originalName,
                        StringComparison.Ordinal) ||

                    !string.Equals(
                        AlbumArtistName,
                        originalArtist,
                        StringComparison.Ordinal) ||

                    ReleaseYear != originalYear;
            }
        }

        public bool HasChanges =>
            HasMetadataChanges || HasArtworkChange;

        public byte[] SelectedArtworkBytes =>
            selectedArtworkBytes;

        public BitmapImage SelectedArtworkImage => selectedArtworkImage;

        private async void ArtworkButton_Click(object sender, RoutedEventArgs e)
        {
            await SelectArtworkAsync();
        }

        private async Task SelectArtworkAsync()
        {
            var picker = new FileOpenPicker();

            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.ViewMode = PickerViewMode.Thumbnail;

            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            var item = await picker.PickSingleFileAsync();

            if (item == null)
                return;

            try
            {
                var file = await StorageFile.GetFileFromPathAsync(item.Path);
                
                if (file == null)
                    return;

                byte[] bytes =
                    await FileIO.ReadBufferAsync(file)
                        .AsTask()
                        .ContinueWith(t =>
                        {
                            IBuffer buffer = t.Result;

                            byte[] data =
                                new byte[buffer.Length];

                            DataReader
                                .FromBuffer(buffer)
                                .ReadBytes(data);

                            return data;
                        });

                if (bytes == null || bytes.Length == 0)
                    return;

                /*
                 * Decode immediately so the user sees the new artwork
                 * before pressing Save.
                 */
                var bitmap = new BitmapImage();

                using (var stream =
                    new InMemoryRandomAccessStream())
                {
                    await stream.WriteAsync(
                        bytes.AsBuffer());

                    stream.Seek(0);

                    await bitmap.SetSourceAsync(stream);
                }

                selectedArtworkBytes = bytes;
                selectedArtworkImage = bitmap;

                ArtworkImage.Source = bitmap; 
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "Album artwork selection failed: " + ex);
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
