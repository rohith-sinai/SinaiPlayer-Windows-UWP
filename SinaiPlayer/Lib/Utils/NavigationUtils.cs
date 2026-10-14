using SinaiPlayer.Lib.UI.Pages;
using SinaiPlayer.Lib.UI.Pages.Player;
using System; 
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace SinaiPlayer.Lib.Utils
{
    public sealed partial class NavigationUtils
    {
        private static Frame RootFrame => Window.Current?.Content as Frame;
        private static Frame MainPageFrame => MainPage.Current.ContentFrame;

        /// <summary>
        /// Navigates to a specific target page.
        /// </summary>
        /// <param name="pageType">The Type of the target Page.</param>
        /// <param name="parameter">Optional data object to pass to the target page.</param>
        /// <returns>True if navigation succeeded; otherwise, false.</returns>
        public static bool NavigateRootFrame(Type pageType, object parameter = null, NavigationTransitionInfo transitionInfo = null)
        {
            if (RootFrame == null || pageType == null) return false;

            // Avoid navigating to the page we are already on
            if (RootFrame.CurrentSourcePageType == pageType && parameter == null) return false;

            return RootFrame.Navigate(pageType, parameter, transitionInfo);
        }

        public static bool NavigateMainPageFrame(Type pageType, object parameter = null, NavigationTransitionInfo transitionInfo = null)
        {
            if (MainPageFrame == null || pageType == null) return false;

            // Avoid navigating to the page we are already on
            if (MainPageFrame.CurrentSourcePageType == pageType && parameter == null) return false;

            return MainPageFrame.Navigate(pageType, parameter, transitionInfo);
        }

        /// <summary>
        /// Goes back one page in the navigation stack if possible.
        /// </summary>
        public static void GoBack()
        {
             
            if (RootFrame != null && RootFrame.CanGoBack)
            {
                RootFrame.GoBack();
            }
            else if (MainPageFrame != null && MainPageFrame.CanGoBack)
            {
                MainPageFrame.GoBack();
            }
            else
            {
                return;
            }
        }

        /// <summary>
        /// Goes forward one page in the navigation stack if possible.
        /// </summary>
        public static void GoForward()
        {
            if (RootFrame != null && RootFrame.CanGoForward)
            {
                RootFrame.GoForward();
            }
            else if (MainPageFrame != null && MainPageFrame.CanGoForward)
            {
                MainPageFrame.GoForward();
            }
            else
            {
                return;
            }
        }

        private static void OnBackRequested(object sender, BackRequestedEventArgs e)
        {
            if (RootFrame != null && RootFrame.CanGoBack && !e.Handled)
            {
                e.Handled = true;
                GoBack();
            }
            else if (MainPageFrame != null && MainPageFrame.CanGoBack && !e.Handled)
            {
                e.Handled = true;
                GoBack();
            }
        }
         

        public static void Navigate_SearchPage(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return;

            NavigateMainPageFrame(typeof(SearchPage), query);
        }

        public static void Navigate_AlbumDetailsPage(string albumId)
        {
            if (string.IsNullOrWhiteSpace(albumId))
                return;

            NavigateMainPageFrame(typeof(AlbumDetailsPage), albumId, new DrillInNavigationTransitionInfo());
        }

        public static void Navigate_ArtistDetailsPage(string artistId)
        {
            if (string.IsNullOrWhiteSpace(artistId))
                return;

            NavigateMainPageFrame(typeof(ArtistDetailsPage), artistId);
        }

        public static void Navigate_PlaylistDetailsPage(string playlistId)
        {
            if (string.IsNullOrWhiteSpace(playlistId))
                return;

            NavigateMainPageFrame(typeof(PlaylistDetailsPage), playlistId, new DrillInNavigationTransitionInfo());
        }

        public static void Navigate_NowPlayingPage()
        {
            NavigateRootFrame(typeof(NowPlayingPage), null, new EntranceNavigationTransitionInfo());
        }

        public static void Navigate_MainPage()
        {
            NavigateRootFrame(typeof(MainPage), null, new EntranceNavigationTransitionInfo());
        }

        public static void Navigate_IntroPage()
        {
            NavigateRootFrame(typeof(IntroPage), null, new EntranceNavigationTransitionInfo());
        }
    }
}
