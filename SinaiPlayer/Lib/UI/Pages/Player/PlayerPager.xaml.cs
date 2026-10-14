using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using SinaiPlayer.Lib.Services;
using SinaiPlayer.Lib.UI.ViewModels;
using System;
using System.ComponentModel;

namespace SinaiPlayer.Lib.UI.Pages.Player
{
    public sealed partial class PlayerPager : UserControl
    {
        private MediaPlayerService Player => MediaPlayerService.Instance;
        private PlayerViewModel VM => PlayerViewModel.Instance;

        private int _selectedIndex = -1;
        private bool _isInitialized;
        private bool _isUpdatingSelection;
        private bool _isDragging;
        private bool _didDrag;
        private bool _ignoreNextViewChanged;

        private uint _dragPointerId;
        private Point _dragStartPoint;
        private double _dragStartOffset;
        private bool _pointerCaptured;

        public PlayerPager()
        {
            InitializeComponent();

            Loaded += PlayerPager_Loaded;
            Unloaded += PlayerPager_Unloaded;

            // Use handledEventsToo so the pager still receives mouse input when
            // a child control inside the current page handles PointerPressed.
            PagerScrollViewer.AddHandler(
                UIElement.PointerPressedEvent,
                new PointerEventHandler(PagerScrollViewer_PointerPressed),
                true);

            PagerScrollViewer.AddHandler(
                UIElement.PointerMovedEvent,
                new PointerEventHandler(PagerScrollViewer_PointerMoved),
                true);

            PagerScrollViewer.AddHandler(
                UIElement.PointerReleasedEvent,
                new PointerEventHandler(PagerScrollViewer_PointerReleased),
                true);

            PagerScrollViewer.AddHandler(
                UIElement.PointerCanceledEvent,
                new PointerEventHandler(PagerScrollViewer_PointerCanceled),
                true);
        }

        private void PlayerPager_Loaded(object sender, RoutedEventArgs e)
        {
            VM.PropertyChanged += VM_PropertyChanged;
            Player.PropertyChanged += Player_PropertyChanged;

            RefreshUI();
        }

        private void PlayerPager_Unloaded(object sender, RoutedEventArgs e)
        {
            VM.PropertyChanged -= VM_PropertyChanged;
            Player.PropertyChanged -= Player_PropertyChanged;
        }

        private void Player_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Player.QueueChanged))
            {
                RefreshUI();
            }
        }

        private void VM_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VM.CurrentIndex))
            {
                UpdateSelectedPage();
            }
        }

        private void RefreshUI()
        {
            if (!IsLoaded)
                return;

            var queue = VM.Queue;
            var count = queue?.Count ?? 0;

            PagerPanel.Children.Clear();
            _selectedIndex = -1;
            _isInitialized = false;

            if (count <= 0)
                return;

            var template = (DataTemplate)Resources["PagerItemTemplate"];
            var pageWidth = PagerScrollViewer.ActualWidth;
            var pageHeight = PagerScrollViewer.ActualHeight;

            for (int i = 0; i < count; i++)
            {
                var page = new ContentControl
                {
                    Content = queue[i],
                    ContentTemplate = template,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    VerticalContentAlignment = VerticalAlignment.Stretch,
                    Width = pageWidth,
                    Height = pageHeight
                };

                PagerPanel.Children.Add(page);
            }

            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                if (!IsLoaded || PagerPanel.Children.Count == 0)
                    return;

                await UpdateSelectedPage();
                _isInitialized = _selectedIndex >= 0;
            });
        }

        private async System.Threading.Tasks.Task UpdateSelectedPage()
        {
            if (!IsLoaded || VM.Queue == null || VM.Queue.Count == 0)
                return;

            var index = VM.CurrentIndex;

            if (index < 0 || index >= PagerPanel.Children.Count)
                return;

            _selectedIndex = index;
            _isUpdatingSelection = true;
            _ignoreNextViewChanged = true;

            try
            {
                var pageWidth = PagerScrollViewer.ActualWidth;
                if (pageWidth > 0)
                {
                    PagerScrollViewer.ChangeView(
                        index * pageWidth,
                        null,
                        null,
                        true);
                }
            }
            finally
            {
                _isUpdatingSelection = false;
            }

            try
            {
                if (VM.CurrentItem != null)
                {
                    await VM.CurrentItem.EnsureArtworkLoadedAsync();
                }
            }
            catch
            {
                // Artwork loading must not break pager synchronization.
            }
        }

        private void PlayerPager_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResizePages();

            if (_selectedIndex < 0 || PagerScrollViewer.ActualWidth <= 0)
                return;

            _isUpdatingSelection = true;
            _ignoreNextViewChanged = true;

            try
            {
                PagerScrollViewer.ChangeView(
                    _selectedIndex * PagerScrollViewer.ActualWidth,
                    null,
                    null,
                    true);
            }
            finally
            {
                _isUpdatingSelection = false;
            }
        }

        private void ResizePages()
        {
            var width = PagerScrollViewer.ActualWidth;
            var height = PagerScrollViewer.ActualHeight;

            if (width <= 0 || height <= 0)
                return;

            foreach (var child in PagerPanel.Children)
            {
                if (child is FrameworkElement page)
                {
                    page.Width = width;
                    page.Height = height;
                }
            }
        }

        private void PagerScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (!_isInitialized || _isDragging || _isUpdatingSelection || _ignoreNextViewChanged)
            {
                _ignoreNextViewChanged = false;
                return;
            }

            var pageWidth = PagerScrollViewer.ActualWidth;
            if (pageWidth <= 0 || PagerPanel.Children.Count == 0)
                return;

            var index = (int)Math.Round(PagerScrollViewer.HorizontalOffset / pageWidth);
            index = Math.Max(0, Math.Min(index, PagerPanel.Children.Count - 1));

            if (index != _selectedIndex)
            {
                _selectedIndex = index;
            }

            // When the ScrollViewer finishes an externally initiated scroll,
            // snap to an exact page position without changing the playing item.
            if (!e.IsIntermediate)
            {
                var shouldPlay =
                    _selectedIndex != Player.CurrentIndex &&
                    !VM.IsReplacingQueue;

                SnapToIndex(_selectedIndex, playItem: shouldPlay);
            }
        }

        private void PagerScrollViewer_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType != Windows.Devices.Input.PointerDeviceType.Mouse)
                return;

            var properties = e.GetCurrentPoint(PagerScrollViewer).Properties;
            if (!properties.IsLeftButtonPressed)
                return;

            _isDragging = true;
            _didDrag = false;
            _dragPointerId = e.Pointer.PointerId;
            _dragStartPoint = e.GetCurrentPoint(PagerScrollViewer).Position;
            _dragStartOffset = PagerScrollViewer.HorizontalOffset;

            // Do not capture or mark the press handled yet. This keeps buttons
            // and other interactive controls inside the page clickable.
            e.Handled = false;
        }

        private void PagerScrollViewer_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isDragging || e.Pointer.PointerId != _dragPointerId)
                return;

            var currentPoint = e.GetCurrentPoint(PagerScrollViewer).Position;
            var deltaX = currentPoint.X - _dragStartPoint.X;

            if (!_didDrag && Math.Abs(deltaX) >= 4)
                _didDrag = true;

            if (!_didDrag)
                return;

            if (!_pointerCaptured)
            {
                _pointerCaptured = PagerScrollViewer.CapturePointer(e.Pointer);
            }

            var newOffset = _dragStartOffset - deltaX;
            var maxOffset = Math.Max(0, PagerPanel.ActualWidth - PagerScrollViewer.ViewportWidth);
            newOffset = Math.Max(0, Math.Min(newOffset, maxOffset));

            PagerScrollViewer.ChangeView(newOffset, null, null, true);
            e.Handled = true;
        }

        private async void PagerScrollViewer_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (!_isDragging || e.Pointer.PointerId != _dragPointerId)
                return;

            FinishMouseDrag(e.Pointer);
            e.Handled = true;

            if (_didDrag)
            {
                var pageWidth = PagerScrollViewer.ActualWidth;
                if (pageWidth <= 0 || PagerPanel.Children.Count == 0)
                    return;

                var index = (int)Math.Round(PagerScrollViewer.HorizontalOffset / pageWidth);
                index = Math.Max(0, Math.Min(index, PagerPanel.Children.Count - 1));

                await SnapToIndexAsync(index, playItem: _isInitialized);
            }
        }

        private void PagerScrollViewer_PointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            if (!_isDragging || e.Pointer.PointerId != _dragPointerId)
                return;

            FinishMouseDrag(e.Pointer);
        }

        private void FinishMouseDrag(Pointer pointer)
        {
            if (_pointerCaptured)
            {
                PagerScrollViewer.ReleasePointerCapture(pointer);
                _pointerCaptured = false;
            }

            _isDragging = false;
        }

        private void SnapToIndex(int index, bool playItem)
        {
            _ = SnapToIndexAsync(index, playItem);
        }

        private async System.Threading.Tasks.Task SnapToIndexAsync(int index, bool playItem)
        {
            if (index < 0 || index >= PagerPanel.Children.Count)
                return;

            _selectedIndex = index;
            _isUpdatingSelection = true;
            _ignoreNextViewChanged = true;

            try
            {
                var pageWidth = PagerScrollViewer.ActualWidth;
                if (pageWidth > 0)
                {
                    PagerScrollViewer.ChangeView(
                        index * pageWidth,
                        null,
                        null,
                        false);
                }
            }
            finally
            {
                _isUpdatingSelection = false;
            }

            if (playItem && index != Player.CurrentIndex && !VM.IsReplacingQueue)
            {
                await Player.PlayAtIndexAsync(index);
            }
        }
    }
}