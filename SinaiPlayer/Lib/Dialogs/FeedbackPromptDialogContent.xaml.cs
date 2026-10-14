using SinaiPlayer.Lib.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace SinaiPlayer.Lib.Dialogs
{
    public sealed partial class FeedbackPromptDialogContent : UserControl
    {
        public FeedbackPromptDialogContent()
        {
            this.InitializeComponent();

            DialogMessage.Text = LocalizationHelper.GetString("FeedbackPromptDialogMessage");
            DontShowCheckBox.Content = LocalizationHelper.GetString("DontShowThisAgain");
        }

        // True if the user checked "Don't show this again" before closing the dialog.
        public bool DontShowAgain => DontShowCheckBox.IsChecked == true;
    }
}