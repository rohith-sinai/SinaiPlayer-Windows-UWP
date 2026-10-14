using SinaiPlayer.Lib.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SinaiPlayer.Lib.Dialogs
{
    public sealed partial class NewPlaylistDialogContent : UserControl
    {
        public string PlaylistName => PlaylistName_TextBox.Text; // Read text
        public ImageSource ImageContent => PickedImage.Source; // Read image

        public NewPlaylistDialogContent()
        {
            InitializeComponent();
            PlaylistName_TextBox.PlaceholderText = LocalizationHelper.GetString("Title");
        }
    }
}
