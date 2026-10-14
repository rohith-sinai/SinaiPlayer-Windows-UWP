using SinaiPlayer.Lib.Helpers;
using SinaiPlayer.Lib.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace SinaiPlayer.Lib.UI.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class IntroPage : Page
    {
        public IntroPage()
        {
            this.InitializeComponent();
            DrawUnderStatusBar();
            Heading.Text = LocalizationHelper.GetString("IntroPageHeading");

            FeatureOneTitle.Text = LocalizationHelper.GetString("IntroPageFeatureOneTitle");
            FeatureOneSubTitle.Text = LocalizationHelper.GetString("IntroPageFeatureOneSubTitle");
            FeatureTwoTitle.Text = LocalizationHelper.GetString("IntroPageFeatureTwoTitle");
            FeatureTwoSubTitle.Text = LocalizationHelper.GetString("IntroPageFeatureTwoSubTitle");
            FeatureThreeTitle.Text = LocalizationHelper.GetString("IntroPageFeatureThreeTitle");
            FeatureThreeSubTitle.Text = LocalizationHelper.GetString("IntroPageFeatureThreeSubTitle");

            ContinueButton.Content = LocalizationHelper.GetString("Continue");
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            PreferenceUtils.SetShownIntroPage(true);

            var frame = Window.Current?.Content as Frame;
            if (frame != null && frame.CanGoBack) frame.GoBack();
            else NavigationUtils.Navigate_MainPage();
        }
        private void DrawUnderStatusBar()
        {
            // 1. Extend view into the title bar
            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;

            // 2. Set up transparent system caption buttons
            var appTitleBar = ApplicationView.GetForCurrentView().TitleBar;
            appTitleBar.ButtonBackgroundColor = Colors.Transparent;
            appTitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            // 3. Set the custom XAML element as the draggable title bar
            Window.Current.SetTitleBar(TitleBar);
             
        }
    }
}
