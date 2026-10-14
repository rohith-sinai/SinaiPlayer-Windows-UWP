using Microsoft.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using SinaiPlayer.Lib.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System.Display;
using Windows.UI;
using Windows.UI.Composition;
using Microsoft.Graphics.Canvas.Effects;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SinaiPlayer.Lib.UI.Pages.Player
{
    public sealed partial class ArtistSlideshowContainer : UserControl
    {
        // MARK: - Timing
        private const double DisplayDuration = 30; // seconds - one image's full zoom, 1.0 -> 1.5
        private const double FadeDuration = 5;     // seconds - crossfade (image opacity + palette color)

        // How long a "current" image displays solo before the next crossfade
        // begins - same relative point as the Swift version's
        // (displayDuration - transitionDuration * 0.5).
        private const double Period = DisplayDuration - (FadeDuration * 0.5);

        private const double StartScale = 1.0;
        private const double EndScale = 1.2;

        // Peak Gaussian blur (px) applied to the outgoing image while it fades out.
        private const float MaxBlurAmount = 20f;

        // The blur only ramps up during the final stretch of the fade, not the
        // whole thing - image stays sharp for the first part of the crossfade,
        // then blurs in over just this many seconds, ending exactly as the fade
        // (and the crossfade as a whole) completes.
        private const double BlurRampDuration = 4;

        private DisplayRequest _displayRequest;
        private bool _displayRequestActive;

        private int currentIndex;
        private int nextIndex;

        // Bumped every time Restart() runs. AnimateAsync captures the value it
        // was started with and bails out the moment it no longer matches -
        // simpler and more robust than a shared "isRunning" bool when Restart
        // can be called again (new Items) while a previous loop is mid-delay.
        private int animationGeneration;

        // One persistent Storyboard per animated target+property, keyed by a
        // fixed per-slot id (NOT by "current"/"next", which flips every cycle) -
        // so Stop() always finds and releases the right one before a manual
        // property assignment. A completed Storyboard otherwise keeps holding
        // its property (FillBehavior.HoldEnd) and would silently override a
        // plain "target.Property = value" set.
        private readonly Dictionary<string, Storyboard> _activeStoryboards = new Dictionary<string, Storyboard>();

        // A physical (Image, ScaleTransform) pair. There are exactly two,
        // permanently tied to the two XAML elements - what changes each cycle
        // is only which one _current/_next POINT AT, not the objects themselves.
        // That's what lets an animation started on a slot while it was "next"
        // keep running, uninterrupted, after it becomes "current".
        private sealed class ImageSlot
        {
            public Image Image { get; }
            public ScaleTransform Scale { get; }
            public string ScaleXKey { get; }
            public string ScaleYKey { get; }
            public string OpacityKey { get; }

            // Composition-layer blur brush for this slot's Image - created lazily
            // the first time it's blurred (see EnsureBlurBrush). Separate from the
            // Storyboard-driven properties above since blur isn't a XAML DP.
            public CompositionEffectBrush BlurBrush { get; set; }

            public ImageSlot(Image image, ScaleTransform scale, string id)
            {
                Image = image;
                Scale = scale;
                ScaleXKey = id + "_scaleX";
                ScaleYKey = id + "_scaleY";
                OpacityKey = id + "_opacity";
            }
        }

        private ImageSlot _slotA;
        private ImageSlot _slotB;
        private ImageSlot _current;
        private ImageSlot _next;

        private readonly SolidColorBrush _overlayBrush = new SolidColorBrush(Colors.Black);

        public ArtistSlideshowContainer()
        {
            InitializeComponent();

            _slotA = new ImageSlot(CurrentImage, CurrentScaleTransform, "A");
            _slotB = new ImageSlot(NextImage, NextScaleTransform, "B");
            _current = _slotA;
            _next = _slotB;

            OverlayRectangle.Fill = _overlayBrush;

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        #region Dependency Properties

        public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
            nameof(Items), typeof(IList<MediaArtwork>), typeof(ArtistSlideshowContainer),
            new PropertyMetadata(null, OnItemsChanged));

        public IList<MediaArtwork> Items
        {
            get => (IList<MediaArtwork>)GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }

        public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(
            nameof(IsExpanded), typeof(bool), typeof(ArtistSlideshowContainer),
            new PropertyMetadata(false, OnIsExpandedChanged));

        public bool IsExpanded
        {
            get => (bool)GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        public static readonly DependencyProperty IsSlidingPanelExpandedProperty = DependencyProperty.Register(
            nameof(IsSlidingPanelExpanded), typeof(bool), typeof(ArtistSlideshowContainer),
            new PropertyMetadata(false, OnIsSlidingPanelExpandedChanged));

        public bool IsSlidingPanelExpanded
        {
            get => (bool)GetValue(IsSlidingPanelExpandedProperty);
            set => SetValue(IsSlidingPanelExpandedProperty, value);
        }

        private static void OnItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ArtistSlideshowContainer)d).Restart();
        }

        // Only toggles the overlay's presence (opacity) - independent of the
        // per-image palette color crossfade, which runs on its own during
        // each image transition regardless of IsExpanded.
        private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ArtistSlideshowContainer)d;

            bool isExpanded = (bool)e.NewValue;


            // ============================================================
            // NORMAL DARK OVERLAY
            // ============================================================

            double overlayTarget = isExpanded ? 0.8 : 0.4;


            control.AnimateDouble(
                control.OverlayRectangle,
                "overlayOpacity",
                nameof(Opacity),
                control.OverlayRectangle.Opacity,
                overlayTarget,
                0.1);


            // ============================================================
            // ACRYLIC BLUR
            // ============================================================

            double acrylicTarget = isExpanded ? 1.0 : 0.0;


            control.AnimateDouble(
                control.ExpandedAcrylicRectangle,
                "expandedAcrylicOpacity",
                nameof(Opacity),
                control.ExpandedAcrylicRectangle.Opacity,
                acrylicTarget,
                0.1);
        }

        private static void OnIsSlidingPanelExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ArtistSlideshowContainer)d;

            if ((bool)e.NewValue)
                control.RequestDisplayActive();
            else
                control.ReleaseDisplayActive();
        }

        #endregion

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Restart();

            // Disable the screen idle timer while the slideshow's panel is
            // expanded - same intent as UIApplication.shared.isIdleTimerDisabled.
            if (IsSlidingPanelExpanded)
            {
                RequestDisplayActive();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // Invalidate any in-flight loop and stop every live animation so
            // nothing keeps ticking (or holding properties) after unload.
            animationGeneration++;

            foreach (var storyboard in _activeStoryboards.Values)
                storyboard.Stop();

            // Re-enable the idle timer when navigating away, to save battery -
            // same as the Swift version's onDisappear.
            ReleaseDisplayActive();
        }

        private void RequestDisplayActive()
        {
            if (_displayRequestActive) return;

            if (_displayRequest == null)
                _displayRequest = new DisplayRequest();

            _displayRequest.RequestActive();
            _displayRequestActive = true;
        }

        private void ReleaseDisplayActive()
        {
            if (!_displayRequestActive || _displayRequest == null) return;

            _displayRequest.RequestRelease();
            _displayRequestActive = false;
        }

        private static Color BlendedPalette(MediaArtwork artwork)
        {
            var p = artwork.Palette;

            // Blend the palette color 50% toward black - same intent as the
            // Swift version's palette.opacity(0.7).blend(with: .black, percentage: 0.5).
            return Color.FromArgb(255, (byte)(p.R), (byte)(p.G), (byte)(p.B));
        }

        // MARK: - Animation

        private void Restart()
        {
            // Invalidates whatever loop is currently running.
            animationGeneration++;
            int generation = animationGeneration;

            if (Items == null || Items.Count == 0)
                return;

            currentIndex = 0;
            nextIndex = Items.Count > 1 ? 1 : 0;

            _current = _slotA;
            _next = _slotB;

            StopAllSlideshowAnimations();
            ResetBlur(_slotA);
            ResetBlur(_slotB);

            _current.Image.Source = Items[currentIndex].Image;
            _current.Image.Opacity = 1;
            _current.Scale.ScaleX = StartScale;
            _current.Scale.ScaleY = StartScale;

            _next.Image.Source = Items.Count > 1 ? Items[nextIndex].Image : null;
            _next.Image.Opacity = 0;
            _next.Scale.ScaleX = StartScale;
            _next.Scale.ScaleY = StartScale;

            _overlayBrush.Color = BlendedPalette(Items[currentIndex]);
            OverlayRectangle.Opacity = IsExpanded ? 0.8 : 0.4;

            if (Items.Count < 2)
                return; // single image - nothing to crossfade to

            // Kick off the very first zoom - every subsequent image's zoom
            // instead gets started mid-crossfade, inside the loop below.
            StartZoom(_current);

            _ = AnimateAsync(generation);
        }

        private async Task AnimateAsync(int generation)
        {
            bool isFirstIteration = true;

            while (generation == animationGeneration && Items != null && Items.Count > 1)
            {
                // The first wait is a full Period, measured from Restart()'s
                // StartZoom call above. Every wait after that is shorter by
                // FadeDuration, because the slot now playing "current" already
                // had its zoom/fade-in running for FadeDuration seconds during
                // the previous crossfade - this keeps every image's own
                // crossfade trigger exactly Period seconds after ITS zoom began.
                double waitTime = isFirstIteration ? Period : Math.Max(0, Period - FadeDuration);
                isFirstIteration = false;

                if (waitTime > 0)
                    await Task.Delay(TimeSpan.FromSeconds(waitTime));

                if (generation != animationGeneration) return;

                // MARK: - TRANSITION
                // Fade, zoom, and palette color all start together, so the
                // next image is already zooming and its overlay color already
                // blending in by the time it's visible - no dead pause.
                StartZoom(_next);
                FadeOpacity(_next, _next.Image.Opacity, 1, FadeDuration);
                FadeOpacity(_current, _current.Image.Opacity, 0, FadeDuration);
                AnimateBlur(_current, 0, MaxBlurAmount, BlurRampDuration, delay: Math.Max(0, FadeDuration - BlurRampDuration));

                Color fromColor = BlendedPalette(Items[currentIndex]);
                Color toColor = BlendedPalette(Items[nextIndex]);
                AnimateColor(_overlayBrush, "overlayColor", fromColor, toColor, FadeDuration);

                await Task.Delay(TimeSpan.FromSeconds(FadeDuration));

                if (generation != animationGeneration) return;

                // MARK: - PROMOTE NEXT -> CURRENT
                // Bookkeeping only - _next's zoom/fade/color are already
                // running and simply continue under their new "current" label,
                // nothing about them is touched or restarted here.
                currentIndex = nextIndex;
                nextIndex = (nextIndex + 1) % Items.Count;

                ImageSlot promoted = _current;
                _current = _next;
                _next = promoted;

                // Recycle the demoted slot to hold the image after next.
                StopAnimation(_next.OpacityKey);
                StopAnimation(_next.ScaleXKey);
                StopAnimation(_next.ScaleYKey);
                ResetBlur(_next);

                _next.Image.Source = Items[nextIndex].Image;
                _next.Image.Opacity = 0;
                _next.Scale.ScaleX = StartScale;
                _next.Scale.ScaleY = StartScale;
            }
        }

        private void StartZoom(ImageSlot slot)
        {
            AnimateDouble(slot.Scale, slot.ScaleXKey, nameof(ScaleTransform.ScaleX), StartScale, EndScale, DisplayDuration);
            AnimateDouble(slot.Scale, slot.ScaleYKey, nameof(ScaleTransform.ScaleY), StartScale, EndScale, DisplayDuration);
        }

        private void FadeOpacity(ImageSlot slot, double from, double to, double seconds)
        {
            AnimateDouble(slot.Image, slot.OpacityKey, nameof(Opacity), from, to, seconds);
        }

        private void StopAllSlideshowAnimations()
        {
            StopAnimation(_slotA.ScaleXKey);
            StopAnimation(_slotA.ScaleYKey);
            StopAnimation(_slotA.OpacityKey);

            StopAnimation(_slotB.ScaleXKey);
            StopAnimation(_slotB.ScaleYKey);
            StopAnimation(_slotB.OpacityKey);

            StopAnimation("overlayColor");
            StopAnimation("overlayOpacity");
        }

        private void StopAnimation(string key)
        {
            if (_activeStoryboards.TryGetValue(key, out var storyboard))
                storyboard.Stop();
        }

        // MARK: - Small animation helpers
        // Storyboard + DoubleAnimation/ColorAnimation stands in for SwiftUI's
        // withAnimation - part of the WinRT/XAML animation system since the
        // original Windows 8 baseline, so both are safe on 10.0.10240.0.
        private void AnimateDouble(DependencyObject target, string key, string propertyPath, double from, double to, double seconds)
        {
            StopAnimation(key);

            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = new Duration(TimeSpan.FromSeconds(seconds)),
                EnableDependentAnimation = true,
            };

            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, propertyPath);

            var storyboard = new Storyboard();
            storyboard.Children.Add(animation);

            _activeStoryboards[key] = storyboard;

            storyboard.Begin();
        }

        private void AnimateColor(Brush target, string key, Color from, Color to, double seconds)
        {
            StopAnimation(key);

            var animation = new ColorAnimation
            {
                From = from,
                To = to,
                Duration = new Duration(TimeSpan.FromSeconds(seconds)),
            };

            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, nameof(SolidColorBrush.Color));

            var storyboard = new Storyboard();
            storyboard.Children.Add(animation);

            _activeStoryboards[key] = storyboard;

            storyboard.Begin();
        }

        // MARK: - Blur (Composition layer)
        //
        // There's no XAML dependency property for blur (that's WPF, not
        // UWP/WinUI), so this rides alongside the Storyboard-driven Opacity/Scale
        // animations above instead of replacing anything: a SpriteVisual carrying
        // a Gaussian-blurred backdrop brush is overlaid on the slot's Image, and
        // its BlurAmount is animated on the Composition layer in lockstep with
        // the existing opacity fade.

        // Lazily creates and attaches the blur brush the first time a slot needs
        // it - can't be done any earlier than this because it needs the Image's
        // element visual/compositor, which are only reliably available once the
        // control is in the tree.
        private void EnsureBlurBrush(ImageSlot slot)
        {
            if (slot.BlurBrush != null)
                return;

            var elementVisual = ElementCompositionPreview.GetElementVisual(slot.Image);
            var compositor = elementVisual.Compositor;

            var blurEffect = new GaussianBlurEffect
            {
                Name = "Blur",
                BlurAmount = 0f,
                BorderMode = EffectBorderMode.Soft,
                Source = new CompositionEffectSourceParameter("Backdrop"),
            };

            var factory = compositor.CreateEffectFactory(blurEffect, new[] { "Blur.BlurAmount" });

            var brush = factory.CreateBrush();
            brush.SetSourceParameter("Backdrop", compositor.CreateBackdropBrush());

            var sprite = compositor.CreateSpriteVisual();
            sprite.Brush = brush;
            sprite.RelativeSizeAdjustment = Vector2.One;

            // Overlays the blurred backdrop directly on top of this slot's own
            // Image - since the Image renders first, "backdrop" here IS the
            // Image's own pixels, so this reads as the image itself blurring.
            ElementCompositionPreview.SetElementChildVisual(slot.Image, sprite);

            slot.BlurBrush = brush;
        }

        private void AnimateBlur(ImageSlot slot, double from, double to, double seconds, double delay = 0)
        {
            EnsureBlurBrush(slot);

            var compositor = slot.BlurBrush.Compositor;

            var animation = compositor.CreateScalarKeyFrameAnimation();
            animation.InsertKeyFrame(0f, (float)from);
            animation.InsertKeyFrame(1f, (float)to);
            animation.Duration = TimeSpan.FromSeconds(seconds);

            if (delay > 0)
            {
                animation.DelayTime = TimeSpan.FromSeconds(delay);
                animation.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
            }

            slot.BlurBrush.StartAnimation("Blur.BlurAmount", animation);
        }

        // Snaps a slot's blur back to 0 with no transition - used when recycling
        // a slot (and on Restart) so it starts perfectly sharp next time it's
        // promoted to "current", the same way its Opacity/Scale get snapped back.
        private void ResetBlur(ImageSlot slot)
        {
            if (slot.BlurBrush == null)
                return;

            slot.BlurBrush.StopAnimation("Blur.BlurAmount");
            slot.BlurBrush.Properties.InsertScalar("Blur.BlurAmount", 0f);
        }
    }
}