using SinaiPlayer.Lib.Loaders;
using SinaiPlayer.Lib.Models;
using SinaiPlayer.Lib.Repos;
using SinaiPlayer.Lib.Services;
using SinaiPlayer.Lib.UI.Pages;
using SinaiPlayer.Lib.Utils;
using System;
using System.Collections.Generic; 
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation; 
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls; 
using Windows.UI.Xaml.Navigation;

namespace SinaiPlayer
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        // Inside App.xaml.cs 
        protected override void OnWindowCreated(WindowCreatedEventArgs args)
        {
            args.Window.CoreWindow.SizeChanged += CoreWindow_SizeChanged;
            base.OnWindowCreated(args);
        }

        private void CoreWindow_SizeChanged(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.WindowSizeChangedEventArgs args)
        {
            double minWidth = 800;
            double minHeight = 600;

            if (args.Size.Width < minWidth || args.Size.Height < minHeight)
            {
                var targetSize = new Size(
                    Math.Max(args.Size.Width, minWidth),
                    Math.Max(args.Size.Height, minHeight)
                );
                // Force the window to stay above your custom limits
                ApplicationView.GetForCurrentView().TryResizeView(targetSize);
            }
        }


        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected async override void OnLaunched(LaunchActivatedEventArgs e)
        {
            try
            {
                Frame rootFrame = await InitializeApplicationAsync();

                if (e.PrelaunchActivated)
                    return;

                // If there is already a page loaded, don't recreate it.
                if (rootFrame.Content == null)
                {
                    if (PreferenceUtils.GetShownIntroPage() == false)
                    {
                        rootFrame.Navigate(typeof(IntroPage), e.Arguments);
                    }
                    else
                    {
                        rootFrame.Navigate(typeof(MainPage), e.Arguments);
                    }
                }

                await DialogUtils.CheckAndShowFeedbackDialogAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnLaunched failed: {ex}");
            }
        }

        private async Task<Frame> InitializeApplicationAsync()
        {
            // Initialize MediaPlayerService before anything tries to use it.
            MediaPlayerService.Initialize(Window.Current.Dispatcher);

            Frame rootFrame = Window.Current.Content as Frame;

            if (rootFrame == null)
            {
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                Window.Current.Content = rootFrame;

                InitTheme(rootFrame);
            }

            await LibraryRepository.InitializeAsync();
            await LibraryRepository.LoadFromCacheAsync();

            await PlaylistRepository.InitializeAsync();
            await PlaylistRepository.LoadAsync();

            _ = LibraryRepository.SyncAsync();

            // Ensure the current window is active
            Window.Current.Activate();

            return rootFrame;
        }
        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            SuspendingDeferral deferral = e.SuspendingOperation.GetDeferral();

            try
            {
                // TODO:
                // Save application state and stop background activity.
            }
            finally
            {
                deferral.Complete();
            }
        }

        protected override async void OnFileActivated(FileActivatedEventArgs args)
        {
            try
            {
                base.OnFileActivated(args);

                if (args.Files == null || args.Files.Count == 0)
                    return;

                // IMPORTANT:
                // File activation can happen without OnLaunched().
                // Therefore initialize the application first.
                Frame rootFrame = await InitializeApplicationAsync();

                var queue = new List<MediaFile>();

                foreach (var item in args.Files)
                {
                    if (item is StorageFile file)
                    {
                        try
                        {
                            var media =
                                await MediaLoader.GenerateMediaFileFromStorageFile(file);

                            if (media != null)
                            {
                                queue.Add(media);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"Failed to load file '{file.Name}': {ex}");
                        }
                    }                    
                }

                if (queue.Count == 0)
                    return;

                // If the application doesn't currently have a page,
                // create the normal MainPage first.
                if (rootFrame.Content == null)
                {
                    rootFrame.Navigate(typeof(MainPage));
                }

                // Give navigation a chance to complete before trying
                // to navigate to the player.
                await Task.Yield();

                // Replace the current queue with the files supplied by
                // Windows File Explorer.
                await MediaPlayerService.Instance.SetQueueAsync(queue);

                // Navigate to the Now Playing page.
                NavigationUtils.Navigate_NowPlayingPage();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnFileActivated failed: {ex}");
            }
        }

        private void InitTheme(Frame root)
        {
            switch (PreferenceUtils.GetTheme())
            {
                case "light":
                    root.RequestedTheme = ElementTheme.Light;
                    break;
                case "dark":
                    root.RequestedTheme = ElementTheme.Dark;
                    break;
                default:
                case "auto":
                    root.RequestedTheme = ElementTheme.Default;
                    break;
            }
        }
    }
}
