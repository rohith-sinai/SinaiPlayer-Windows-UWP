using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Core;
using SinaiPlayer.Lib.UI.Adapters;
using SinaiPlayer.Lib.Utils;
using SinaiPlayer.Lib.Utils.Animations;
using System;
using System.Diagnostics;
using Windows.System;
using SinaiPlayer.Lib.Helpers;

namespace SinaiPlayer.Lib.UI.Pages.Player
{
    public sealed partial class NowPlayingPage : Page
    {
        // ============================================================
        // INACTIVITY
        // ============================================================

        private readonly DispatcherTimer _inactivityTimer;


        // ============================================================
        // PLAYER CONTROLS SPRING
        // ============================================================

        private SpringAnimator _controlsHeightSpring;

        private double _controlsFullHeight = -1;

        private Stopwatch _controlsSpringClock;

        private bool _controlsSpringRunning;


        // ============================================================
        // EXPANDED VIEW SPRING
        // ============================================================

        private readonly SpringAnimator _expandedViewSpring;

        private Stopwatch _expandedViewSpringClock;

        private bool _expandedViewSpringRunning;

        private ExpandedViewMode _currentExpandedMode =
            ExpandedViewMode.None;


        // ============================================================
        // EXPANDED LAYOUT STATE
        // ============================================================

        /*
         * Height of PlayerPager once Queue/Lyrics is opened.
         *
         * IMPORTANT:
         *
         * This is the value that becomes the fixed PagerRow height.
         *
         * It is NOT recalculated while Controls are hiding/showing.
         */
        private double _expandedPagerHeight = -1;


        /*
         * Used only while opening/closing the expanded view.
         *
         * Once the expanded state has settled, PagerRow remains fixed
         * at _expandedPagerHeight.
         */
        private double _expandedPagerStartHeight = -1;


        /*
         * Current expanded row target.
         */
        private double _expandedTargetHeight;


        /*
         * Prevents a second layout operation from entering while
         * Grid row values are being changed.
         */
        private bool _isUpdatingExpandedLayout;


        /*
         * Indicates whether the page has already been initialized.
         */
        private bool _isInitialized;


        // ============================================================
        // CONSTANTS
        // ============================================================

        /*
         * Percentage of the Main_Container that remains occupied by
         * PlayerPager when Queue/Lyrics is opened.
         *
         * 0.50 = 50%
         */
        private const double ExpandedPagerHeight = 180;

        // ============================================================
        // CONSTRUCTOR
        // ============================================================
        private WindowManager _windowManager;

        public NowPlayingPage()
        {
            InitializeComponent();
            AppNameTextBlock.Text = LocalizationHelper.GetString("AppName");
            _windowManager = new WindowManager();
            _windowManager.FullScreenChanged += WindowManager_FullScreenChanged;


            // --------------------------------------------------------
            // Inactivity timer
            // --------------------------------------------------------

            _inactivityTimer =
                new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(3)
                };

            _inactivityTimer.Tick +=
                InactivityTimer_Tick;


            // --------------------------------------------------------
            // Controls measurement
            // --------------------------------------------------------

            Controls_Container.SizeChanged +=
                Controls_Container_SizeChanged;


            // --------------------------------------------------------
            // Expanded view
            // --------------------------------------------------------

            _expandedViewSpring =
                new SpringAnimator(0);

            PlayerControls_UserControl
                .ExpandedViewModeChanged +=
                PlayerControls_ExpandedViewModeChanged;


            // --------------------------------------------------------
            // Page lifetime
            // --------------------------------------------------------

            Loaded +=
                NowPlayingPage_Loaded;

            Unloaded +=
                NowPlayingPage_Unloaded;
        }


        // ============================================================
        // PAGE LOADED
        // ============================================================

        private void NowPlayingPage_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            if (_isInitialized)
                return;

            _isInitialized = true;

            _inactivityTimer.Start();
        }


        // ============================================================
        // PAGE UNLOADED
        // ============================================================

        private void NowPlayingPage_Unloaded(
            object sender,
            RoutedEventArgs e)
        {
            _inactivityTimer.Stop();

            _windowManager.FullScreenChanged -= WindowManager_FullScreenChanged;

            StopControlsSpring();

            StopExpandedViewSpring();

            CompositionTarget.Rendering -=
                ControlsSpring_Rendering;

            CompositionTarget.Rendering -=
                ExpandedViewSpring_Rendering;
        }


        // ============================================================
        // POINTER MOVEMENT
        // ============================================================

        private void Grid_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            PlayerBackground_UserControl.ResetState();
            PlayerControls_UserControl.ResetState();
            NowPlayingPagerAdapter.ResetState();
            ResetTitleBarState();
            ShowControls();
            _inactivityTimer.Stop();
            _inactivityTimer.Start();
        }


        // ============================================================
        // INACTIVITY
        // ============================================================

        private void InactivityTimer_Tick(object sender, object e)
        {
            _inactivityTimer.Stop();
            PlayerBackground_UserControl.TriggerInactivityAnimation();
            PlayerControls_UserControl.TriggerInactivityAnimation();
            NowPlayingPagerAdapter.TriggerInactivityAnimation();

            // The title bar and mouse cursor are a full-screen-only affordance - in windowed
            // mode both are expected to stay put regardless of inactivity.
            if (_windowManager.IsFullScreen)
            {
                TitleBarFadeOutStoryboard.Begin();
                SetCursorVisible(false);
            }

            HideControls();
        }


        // ============================================================
        // FULL SCREEN
        // ============================================================

        private void WindowManager_FullScreenChanged(object sender, bool isFullScreen)
        {
            // Dropping out of full screen (e.g. via the transport bar's toggle button) while
            // the title bar/cursor are mid-fade or already faded should bring both back
            // immediately, since the fade only ever makes sense while full screen.
            if (!isFullScreen)
            {
                ResetTitleBarState();
            }
        }

        private void ResetTitleBarState()
        {
            TitleBarFadeOutStoryboard.Stop();
            //TitleBar.Visibility = Visibility.Visible;
            TitleBarFadeInStoryboard.Begin();

            SetCursorVisible(true);
        }

        private void TitleBarFadeOutStoryboard_Completed(object sender, object e)
        {
            //TitleBar.Visibility = Visibility.Collapsed;
        }

        private void SetCursorVisible(bool visible)
        {
            var window = CoreWindow.GetForCurrentThread();

            if (window == null)
                return;

            // Setting PointerCursor to null hides it entirely; a fresh arrow cursor brings it
            // back. Guard against redundantly reassigning the same state every pointer move.
            if (visible && window.PointerCursor == null)
            {
                window.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
            }
            else if (!visible && window.PointerCursor != null)
            {
                window.PointerCursor = null;
            }
        }


        // ============================================================
        // CONTROLS MEASUREMENT
        // ============================================================

        private void Controls_Container_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            /*
             * We only need the natural height once.
             */
            if (_controlsFullHeight >= 0)
                return;


            if (e.NewSize.Height <= 0)
                return;


            _controlsFullHeight = e.NewSize.Height;


            _controlsHeightSpring = new SpringAnimator(_controlsFullHeight);


            /*
             * We no longer need SizeChanged after the first
             * measurement because the spring owns Height.
             */
            Controls_Container.SizeChanged -= Controls_Container_SizeChanged;
        }


        // ============================================================
        // SHOW CONTROLS
        // ============================================================

        private void ShowControls()
        {
            if (_controlsHeightSpring == null)
                return;

            _controlsHeightSpring.SetTarget(_controlsFullHeight);

            StartControlsSpring();
        }


        // ============================================================
        // HIDE CONTROLS
        // ============================================================

        private void HideControls()
        {
            if (_controlsHeightSpring == null)
                return;


            _controlsHeightSpring.SetTarget(0);


            StartControlsSpring();
        }


        // ============================================================
        // START CONTROLS SPRING
        // ============================================================

        private void StartControlsSpring()
        {
            if (_controlsSpringRunning)
                return;


            _controlsSpringRunning = true;


            _controlsSpringClock = Stopwatch.StartNew();


            CompositionTarget.Rendering += ControlsSpring_Rendering;
        }


        // ============================================================
        // STOP CONTROLS SPRING
        // ============================================================

        private void StopControlsSpring()
        {
            if (!_controlsSpringRunning)
                return;


            CompositionTarget.Rendering -= ControlsSpring_Rendering;


            _controlsSpringRunning = false;


            _controlsSpringClock?.Stop();
        }


        // ============================================================
        // CONTROLS SPRING FRAME
        // ============================================================

        private void ControlsSpring_Rendering(object sender, object e)
        {
            if (_controlsHeightSpring == null)
            {
                StopControlsSpring();
                return;
            }


            double deltaSeconds = _controlsSpringClock.Elapsed.TotalSeconds;


            _controlsSpringClock.Restart();


            /*
             * Protect the spring from huge time jumps.
             */
            deltaSeconds = Math.Clamp(deltaSeconds, 0.001, 0.05);


            double height = _controlsHeightSpring.Step(deltaSeconds);


            /*
             * Validate the value before sending it to XAML.
             */
            if (double.IsNaN(height) || double.IsInfinity(height))
            {
                StopControlsSpring();

                Controls_Container.Height = _controlsFullHeight;

                return;
            }


            height = Math.Clamp(height, 0, _controlsFullHeight);


            Controls_Container.Height = height;

            if (_controlsHeightSpring.IsSettled)
            {
                StopControlsSpring();
            }
        }


        // ============================================================
        // QUEUE / LYRICS MODE CHANGED
        // ============================================================

        private void PlayerControls_ExpandedViewModeChanged(object sender, ExpandedViewMode mode)
        {
            _currentExpandedMode = mode;

            bool isExpanded = _currentExpandedMode != ExpandedViewMode.None;

            PlayerBackground_UserControl.IsExpanded = isExpanded;
            NowPlayingPagerAdapter.SetIsExpanded(isExpanded);

            if (mode == ExpandedViewMode.None)
            {
                CloseExpandedView();
                return;
            }


            /*
             * Show the requested panel before the animation.
             */
            Lyrics_Container.Visibility =
                mode == ExpandedViewMode.Lyrics ? Visibility.Visible : Visibility.Collapsed;


            Queue_Container.Visibility =
                mode == ExpandedViewMode.Queue ? Visibility.Visible : Visibility.Collapsed;


            /*
             * Start the expanded layout.
             */
            OpenExpandedView();
        }


        // ============================================================
        // OPEN EXPANDED VIEW
        // ============================================================

        private void OpenExpandedView()
        {
            double mainHeight =
                Main_Container.ActualHeight;

            if (double.IsNaN(mainHeight) ||
                double.IsInfinity(mainHeight) ||
                mainHeight <= 0)
            {
                //DispatcherQueue.TryEnqueue(OpenExpandedView);
                OpenExpandedView();
                return;
            }

            /*
             * If we are already expanded, keep the existing fixed
             * pager height. This is important when switching between
             * Queue and Lyrics.
             */
            if (_expandedPagerHeight <= 0)
            {
                _expandedPagerHeight =
                    Math.Min(
                        ExpandedPagerHeight,
                        mainHeight);
            }

            _expandedPagerStartHeight =
                Pager_Container.ActualHeight;

            if (double.IsNaN(_expandedPagerStartHeight) ||
                double.IsInfinity(_expandedPagerStartHeight) ||
                _expandedPagerStartHeight <= 0)
            {
                _expandedPagerStartHeight =
                    mainHeight;
            }

            /*
             * PlayerPager always ends at 120px when expanded.
             */
            _expandedViewSpring.SetTarget(
                _expandedPagerHeight);

            ExpandedView_Container.IsHitTestVisible =
                true;

            StartExpandedViewSpring();
        }


        // ============================================================
        // CLOSE EXPANDED VIEW
        // ============================================================

        private void CloseExpandedView()
        {
            /*
             * Current pager height is the current position.
             *
             * The expanded spring will animate the pager back
             * to the full Main_Container height.
             */
            double currentPagerHeight = Pager_Container.ActualHeight;


            if (double.IsNaN(currentPagerHeight) || double.IsInfinity(currentPagerHeight) || currentPagerHeight <= 0)
            {
                currentPagerHeight = _expandedPagerHeight;
            }


            /*
             * Temporarily keep PagerRow pixel-sized while closing.
             */
            PagerRow.Height = new GridLength(Math.Max(0, currentPagerHeight), GridUnitType.Pixel);


            /*
             * Target = full Main_Container height.
             */
            double target = Main_Container.ActualHeight;


            if (double.IsNaN(target) || double.IsInfinity(target) || target <= 0)
            {
                return;
            }


            /*
             * Reuse the same spring.
             *
             * The spring now represents PagerRow height.
             */
            _expandedViewSpring.SetTarget(target);


            StartExpandedViewSpring();
        }


        // ============================================================
        // START EXPANDED SPRING
        // ============================================================

        private void StartExpandedViewSpring()
        {
            if (_expandedViewSpringRunning)
                return;


            _expandedViewSpringRunning = true;


            _expandedViewSpringClock = Stopwatch.StartNew();


            CompositionTarget.Rendering += ExpandedViewSpring_Rendering;
        }


        // ============================================================
        // STOP EXPANDED SPRING
        // ============================================================

        private void StopExpandedViewSpring()
        {
            if (!_expandedViewSpringRunning)
                return;


            CompositionTarget.Rendering -=
                ExpandedViewSpring_Rendering;


            _expandedViewSpringRunning = false;


            _expandedViewSpringClock?.Stop();
        }


        // ============================================================
        // EXPANDED SPRING FRAME
        // ============================================================

        private void ExpandedViewSpring_Rendering(
            object sender,
            object e)
        {
            if (_isUpdatingExpandedLayout)
                return;


            _isUpdatingExpandedLayout = true;


            try
            {
                double deltaSeconds =
                    _expandedViewSpringClock
                        .Elapsed
                        .TotalSeconds;


                _expandedViewSpringClock.Restart();


                deltaSeconds =
                    Math.Clamp(
                        deltaSeconds,
                        0.001,
                        0.05);


                /*
                 * ====================================================
                 * OPENING
                 * ====================================================
                 *
                 * Spring value represents PAGER HEIGHT.
                 *
                 * As Pager gets smaller:
                 *
                 *     ExpandedView gets bigger.
                 */
                if (_currentExpandedMode !=
                    ExpandedViewMode.None)
                {
                    double pagerHeight =
                        _expandedViewSpring
                            .Step(deltaSeconds);


                    if (double.IsNaN(pagerHeight) ||
                        double.IsInfinity(pagerHeight))
                    {
                        StopExpandedViewSpring();

                        RestoreNormalLayout();

                        return;
                    }


                    double mainHeight =
                        Main_Container.ActualHeight;


                    if (double.IsNaN(mainHeight) ||
                        double.IsInfinity(mainHeight) ||
                        mainHeight <= 0)
                    {
                        return;
                    }


                    /*
                     * Pager cannot be larger than Main_Container.
                     */
                    pagerHeight =
                        Math.Clamp(
                            pagerHeight,
                            0,
                            mainHeight);


                    /*
                     * ------------------------------------------------
                     * PAGER
                     * ------------------------------------------------
                     *
                     * Explicit pixel height.
                     */
                    PagerRow.Height = new GridLength(pagerHeight, GridUnitType.Pixel);


                    /*
                     * ------------------------------------------------
                     * EXPANDED VIEW
                     * ------------------------------------------------
                     *
                     * It gets the remaining area automatically.
                     *
                     * We don't animate ExpandedView.Height.
                     */
                    ExpandedViewRow.Height = new GridLength(1, GridUnitType.Star);


                    /*
                     * ------------------------------------------------
                     * PROGRESS
                     * ------------------------------------------------
                     *
                     * 0 = full pager
                     * 1 = expanded pager size
                     */
                    double startHeight = Math.Max(_expandedPagerStartHeight, _expandedPagerHeight);


                    double progress =
                        startHeight >
                        _expandedPagerHeight
                            ? 1.0 -
                              ((pagerHeight -
                                _expandedPagerHeight) /
                               (startHeight -
                                _expandedPagerHeight))
                            : 1.0;


                    progress = Math.Clamp(progress, 0, 1);


                    ExpandedView_Container.Opacity = progress;


                    /*
                     * Keep artwork animation synchronized.
                     */
                    NowPlayingPagerAdapter.SetExpansionProgress(progress);


                    /*
                     * ------------------------------------------------
                     * OPEN ANIMATION COMPLETE
                     * ------------------------------------------------
                     */
                    if (_expandedViewSpring.IsSettled)
                    {
                        /*
                         * Freeze pager at its final height.
                         *
                         * This is the important state.
                         */
                        _expandedPagerHeight = pagerHeight;


                        PagerRow.Height = new GridLength(_expandedPagerHeight, GridUnitType.Pixel);


                        /*
                         * ExpandedView gets ALL remaining space.
                         */
                        ExpandedViewRow.Height = new GridLength(1, GridUnitType.Star);


                        ExpandedView_Container.Opacity = 1;


                        NowPlayingPagerAdapter
                            .SetExpansionProgress(1);


                        StopExpandedViewSpring();
                    }


                    return;
                }


                /*
                 * ====================================================
                 * CLOSING
                 * ====================================================
                 *
                 * Spring value represents PAGER HEIGHT and moves
                 * back toward the full Main_Container height.
                 */
                double closingPagerHeight = _expandedViewSpring.Step(deltaSeconds);


                if (double.IsNaN(closingPagerHeight) || double.IsInfinity(closingPagerHeight))
                {
                    StopExpandedViewSpring();

                    RestoreNormalLayout();

                    return;
                }


                double closingMainHeight = Main_Container.ActualHeight;


                if (double.IsNaN(closingMainHeight) || double.IsInfinity(closingMainHeight) || closingMainHeight <= 0)
                {
                    return;
                }


                closingPagerHeight = Math.Clamp(closingPagerHeight, 0, closingMainHeight);


                /*
                 * Pager grows upward/downward from its TOP edge.
                 *
                 * Since Grid row starts at the top, its top edge
                 * never changes.
                 */
                PagerRow.Height = new GridLength(closingPagerHeight, GridUnitType.Pixel);


                /*
                 * ExpandedView remains directly below Pager.
                 */
                ExpandedViewRow.Height = new GridLength(1, GridUnitType.Star);


                /*
                 * Fade expanded content while closing.
                 */
                double closingStart = Math.Max(_expandedPagerHeight, 1);


                double closeProgress = closingPagerHeight / closingStart;


                closeProgress = Math.Clamp(closeProgress, 0, 1);


                ExpandedView_Container.Opacity = closeProgress;


                NowPlayingPagerAdapter.SetExpansionProgress(closeProgress);


                /*
                 * ------------------------------------------------
                 * CLOSE COMPLETE
                 * ------------------------------------------------
                 */
                if (_expandedViewSpring.IsSettled)
                {
                    RestoreNormalLayout();

                    StopExpandedViewSpring();
                }
            }
            finally
            {
                _isUpdatingExpandedLayout = false;
            }
        }


        // ============================================================
        // RESTORE NORMAL LAYOUT
        // ============================================================

        private void RestoreNormalLayout()
        {
            /*
             * Expanded panel disappears.
             */
            ExpandedViewRow.Height = new GridLength(0, GridUnitType.Pixel);


            ExpandedView_Container.Opacity = 0;


            ExpandedView_Container.IsHitTestVisible = false;


            /*
             * Return pager to flexible full-height state.
             */
            PagerRow.Height = new GridLength(1, GridUnitType.Star);


            /*
             * Hide both pages after the collapse has finished.
             */
            Lyrics_Container.Visibility = Visibility.Collapsed;


            Queue_Container.Visibility = Visibility.Collapsed;


            /*
             * Reset pager artwork.
             */
            NowPlayingPagerAdapter.SetExpansionProgress(0);


            /*
             * Clear expanded layout state.
             */
            _expandedPagerHeight = -1;

            _expandedPagerStartHeight = -1;

            _expandedTargetHeight = 0;
        }

        private void BackButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (_windowManager.IsFullScreen)
            {
                _windowManager.ExitFullScreen();
            }

            NavigationUtils.GoBack();
        }
    }
}