using LibVLCSharp.Shared;
using SinaiPlayer.Lib.Enums;
using SinaiPlayer.Lib.Helpers;
using SinaiPlayer.Lib.Models;
using SinaiPlayer.Lib.Thumbnails;
using SinaiPlayer.Lib.UI.ViewModels;
using SinaiPlayer.Lib.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Core;

namespace SinaiPlayer.Lib.Services
{
    public sealed class MediaPlayerService : INotifyPropertyChanged, IDisposable
    {

        // ───────────────────────────────────────────────────────────────────────
        //  Singleton
        // ───────────────────────────────────────────────────────────────────────

        private static MediaPlayerService _instance;
        private static readonly object _lock = new object();

        public static MediaPlayerService Instance
        {
            get
            {
                lock (_lock)
                    return _instance
                        ?? throw new InvalidOperationException(
                            "Call MediaPlayerService.Initialize(dispatcher) first (e.g. in App.xaml.cs OnLaunched, using Window.Current.Dispatcher).");
            }
        }

        public static void Initialize(CoreDispatcher uiDispatcher)
        {
            lock (_lock)
                _instance = new MediaPlayerService(uiDispatcher);
        }

        // ───────────────────────────────────────────────────────────────────────
        //  Private state
        // ───────────────────────────────────────────────────────────────────────

        // One LibVLC engine instance per process. "--no-osd" avoids libvlc drawing
        // its own on-screen text; the rest keeps logging quiet on constrained devices.
        private static LibVLC _libVLC;

        private MediaPlayer _player;          // LibVLCSharp.Shared.MediaPlayer (NOT Windows.Media.Playback.MediaPlayer)
        private readonly CoreDispatcher _ui;
        private SystemMediaTransportControls _smtc;

        private List<MediaFile> _originalQueue = new List<MediaFile>();
        private List<MediaFile> _shuffledQueue = new List<MediaFile>();
        private int _currentIndex = -1;
        private bool _isShuffled;
        private RepeatModeType _repeatMode = RepeatModeType.None;

        private MediaFile _currentMedia;
        private Media _currentLibVlcMedia;             // must be disposed when swapped
        private bool _videoSurfaceAttached;
        private bool _isPlaying;
        private bool _isLoading;
        private TimeSpan _currentPosition;
        private TimeSpan _totalDuration;
        private double _volume;
        private double _playbackRate = 1.0;
        private bool _disposed;

        private Timer _timelineTimer;

        private List<MediaFile> ActiveQueue => _isShuffled ? _shuffledQueue : _originalQueue;

        // Keep the stream backing the current SMTC thumbnail alive.
        private InMemoryRandomAccessStream _smtcThumbnailStream;

        // Prevent a slower artwork lookup for an old track from overwriting the
        // SMTC display after the user has already changed tracks.
        private int _smtcDisplayVersion;

        // ───────────────────────────────────────────────────────────────────────
        //  Events
        // ───────────────────────────────────────────────────────────────────────

        public event EventHandler<MediaFile> MediaChanged;
        public event EventHandler<bool> PlaybackStateChanged;
        public event EventHandler<TimeSpan> PositionChanged;
        public event EventHandler<TimeSpan> DurationChanged;
        public event EventHandler<double> VolumeChanged;
        public event EventHandler QueueChanged;
        public event EventHandler<string> MediaFailed;
        public event EventHandler PlaybackCompleted;

        public event PropertyChangedEventHandler PropertyChanged;

        // ───────────────────────────────────────────────────────────────────────
        //  Public properties
        // ───────────────────────────────────────────────────────────────────────

        public IReadOnlyList<MediaFile> Queue => ActiveQueue.AsReadOnly();

        public MediaFile CurrentMedia => _currentMedia;
        public int CurrentIndex => _currentIndex;
        public bool IsPlaying => _isPlaying;
        public bool IsLoading => _isLoading;
        public bool IsShuffled => _isShuffled;
        public RepeatModeType RepeatMode => _repeatMode;
        public TimeSpan TotalDuration => _totalDuration;

        /// <summary>
        /// Volume exposed as 0.0-1.0. LibVLC uses an integer 0-100 internally.
        /// Keeping this normalized prevents navigation/surface recreation from
        /// changing the user's selected volume.
        /// </summary>
        public double Volume
        {
            get => _volume;
            set
            {
                _volume = (int)Math.Round(value);
                _player.Volume = (int)Math.Round(value);
                Notify();
            }
        }

        public bool CanGoNext
            => ActiveQueue.Count > 0
            && (_currentIndex < ActiveQueue.Count - 1 || _repeatMode != RepeatModeType.None);

        public bool CanGoPrevious
            => ActiveQueue.Count > 0
            && (_currentIndex > 0 || _repeatMode != RepeatModeType.None);

        public TimeSpan CurrentPosition
        {
            get => _currentPosition;
            set => Seek(value);
        }


        public bool IsMuted
        {
            get => _player.Mute;
            set { _player.Mute = value; Notify(); }
        }

        public double PlaybackRate
        {
            get => _playbackRate;
            set
            {
                _playbackRate = MathExtensions.Clamp(value, 0.25, 4.0);
                _player.SetRate((float)_playbackRate);
                Notify();
            }
        }

        /// <summary>
        /// LibVLCSharp's MediaPlayer — NOT bindable directly to XAML like the old
        /// Windows.Media.Playback.MediaPlayer was. Use AttachRenderSurface() with a
        /// VideoView instead. Exposed here for callers that need direct access
        /// (e.g. custom overlays, extra event subscriptions).
        /// </summary>
        public MediaPlayer MediaPlayerInstance => _player;

        // ───────────────────────────────────────────────────────────────────────
        //  Constructor
        // ───────────────────────────────────────────────────────────────────────

        private MediaPlayerService(CoreDispatcher ui)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));

            // Core() only needs to run once per process; safe to call repeatedly.
            Core.Initialize();

            // Confirmed 3.3.0-safe constructor shape: LibVLC(params string[] options).
            // No bool overload is assumed here — that avoided a real risk of a
            // "no constructor takes N arguments" error on this exact line.
            _libVLC = new LibVLC("--no-osd", "--no-snapshot-preview");

            _player = new MediaPlayer(_libVLC);
            Volume = PreferenceUtils.GetVolume();

            HookPlayerEvents();
            SetupSmtc();

            _timelineTimer = new Timer(
                _ => PushSmtcTimeline(),
                null,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1));
        }

        // Volume
        public void SetVolume(double value)
        {
            Volume = value;
            PreferenceUtils.SetVolume(value);
        }

        public void ToggleMute()
        {
            if (_volume == 0)
            {
                Volume = PreferenceUtils.GetVolume();
            }
            else
            {
                Volume = 0;
            }
        }

        // Speed
        public void SetPlaybackRate(double value)
        {
            PlaybackRate = value;
            //PreferenceUtils.SetVolume(value);
        }

        /// <summary>
        /// Connects the LibVLC player to the UWP VideoView swap chain.
        ///
        /// UWP binds the swap-chain options to the LibVLC instance itself, so a new
        /// VideoView surface requires a new LibVLC/MediaPlayer pair. When navigation
        /// destroys and recreates the VideoView, we rebuild the player but preserve the
        /// current file, playback position, volume, mute state, playback rate and
        /// playing/paused state.
        /// </summary>
        public MediaPlayer AttachVideoSurface(string[] swapChainOptions)
        {
            if (swapChainOptions == null)
                throw new ArgumentNullException(nameof(swapChainOptions));

            // Do not rebuild while the current VideoView is still using this surface.
            if (_videoSurfaceAttached)
                return _player;

            _videoSurfaceAttached = true;

            // Capture playback state BEFORE disposing the old player.
            var mediaToRestore = _currentMedia;
            long savedPositionMs = 0;
            bool wasPlaying = _isPlaying;
            int savedVolume = _player != null ? _player.Volume : (int)Math.Round(_volume);
            bool savedMute = _player != null && _player.Mute;
            double savedRate = _playbackRate;

            try
            {
                if (_player != null)
                {
                    try
                    {
                        if (_player.Time > 0)
                            savedPositionMs = _player.Time;
                    }
                    catch
                    {
                        savedPositionMs = (long)Math.Max(0, _currentPosition.TotalMilliseconds);
                    }

                    if (_player.State == VLCState.Playing)
                        wasPlaying = true;
                }
            }
            catch
            {
                savedPositionMs = (long)Math.Max(0, _currentPosition.TotalMilliseconds);
            }

            savedPositionMs = Math.Max(0, savedPositionMs);

            UnhookPlayerEvents();
            DisposeCurrentMedia();

            try { _player?.Stop(); } catch { }
            try { _player?.Dispose(); } catch { }
            try { _libVLC?.Dispose(); } catch { }

            var options = new List<string>
            {
                "--no-osd",
                "--no-snapshot-preview"
            };
            options.AddRange(swapChainOptions);

            _libVLC = new LibVLC(options.ToArray());
            _player = new MediaPlayer(_libVLC);
            _player.Volume = savedVolume;
            _volume = savedVolume / 100.0;
            _player.Mute = savedMute;
            _player.SetRate((float)savedRate);

            HookPlayerEvents();

            // A newly-created LibVLC player has no media. Restore the currently playing
            // file asynchronously without changing _currentMedia or resetting the UI
            // position to zero.
            if (mediaToRestore != null)
            {
                _ = RestorePlaybackAfterSurfaceRebuildAsync(
                    mediaToRestore,
                    savedPositionMs,
                    wasPlaying);
            }

            return _player;
        }

        /// <summary>
        /// Marks the current UWP VideoView surface as detached. The next VideoView
        /// instance will therefore rebuild LibVLC with its new swap-chain options.
        /// Playback itself is intentionally NOT stopped.
        /// </summary>
        public void ReleaseVideoSurface()
        {
            _videoSurfaceAttached = false;
        }

        private async Task RestorePlaybackAfterSurfaceRebuildAsync(
            MediaFile file,
            long positionMs,
            bool wasPlaying)
        {
            if (file == null || _player == null || _libVLC == null)
                return;

            try
            {
                await StorageFile.GetFileFromPathAsync(file.Path);

                DisposeCurrentMedia();
                _currentLibVlcMedia = new Media(_libVLC, file.Path, FromType.FromPath);

                bool started = _player.Play(_currentLibVlcMedia);
                if (!started)
                    throw new Exception("LibVLC failed to restore playback after the video surface was recreated.");

                // Wait until LibVLC has opened the media before applying the position.
                // This avoids the seek being silently lost while the decoder is opening.
                for (int i = 0; i < 40; i++)
                {
                    if (_player.State == VLCState.Playing)
                        break;

                    await Task.Delay(50);
                }

                if (_player.IsSeekable && positionMs > 0)
                {
                    long durationMs = _player.Length;
                    long target = durationMs > 0
                        ? Math.Min(positionMs, durationMs - 1)
                        : positionMs;

                    if (target > 0)
                        _player.Time = target;
                }

                _currentPosition = TimeSpan.FromMilliseconds(Math.Max(0, positionMs));
                Notify(nameof(CurrentPosition));

                if (wasPlaying)
                {
                    _isPlaying = true;
                    _smtc.PlaybackStatus = MediaPlaybackStatus.Playing;
                }
                else
                {
                    _player.Pause();
                    _isPlaying = false;
                    _smtc.PlaybackStatus = MediaPlaybackStatus.Paused;
                }

                Notify(nameof(IsPlaying));
                PushSmtcTimeline();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("RestorePlaybackAfterSurfaceRebuild failed: " + ex);
                MediaFailed?.Invoke(this,
                    $"[{file.FileName}] Failed to restore video after navigating back: {ex.Message}");
            }
        }


        // ───────────────────────────────────────────────────────────────────────
        //  Queue management  (UNCHANGED — no engine-specific APIs here)
        // ───────────────────────────────────────────────────────────────────────

        public async Task SetQueueAsync(IEnumerable<MediaFile> files, int startIndex = 0, bool autoPlay = true)
        {
            if (files is null) throw new ArgumentNullException(nameof(files));

            var list = files.ToList();
            if (list.Count == 0) return;

            StopInternal(clearSource: true);

            _originalQueue = list;
            _currentIndex = MathExtensions.Clamp(startIndex, 0, list.Count - 1);

            if (_isShuffled)
                _shuffledQueue = BuildShuffledQueue(pinOriginalIndex: _currentIndex);

            OnQueueChanged();

            if (autoPlay)
                await PlayAtIndexAsync(_currentIndex);
        }

        public void AddQueueItemFirst(MediaFile file)
        {
            if (file is null) return;

            _originalQueue.Insert(0, file);
            if (_isShuffled) _shuffledQueue.Insert(0, file);

            if (_currentIndex >= 0) _currentIndex++;

            OnQueueChanged();
        }

        public void PlayNext(MediaFile file)
        {
            if (file is null) return;

            int insertAt = Math.Max(0, _currentIndex + 1);
            _originalQueue.Insert(Math.Min(insertAt, _originalQueue.Count), file);
            if (_isShuffled) _shuffledQueue.Insert(Math.Min(insertAt, _shuffledQueue.Count), file);

            OnQueueChanged();
        }

        public void AddQueueItemLast(MediaFile file)
        {
            if (file is null) return;

            _originalQueue.Add(file);
            if (_isShuffled) _shuffledQueue.Add(file);

            OnQueueChanged();
        }

        public void InsertQueueItem(int index, MediaFile file)
        {
            if (file is null) return;

            index = MathExtensions.Clamp(index, 0, ActiveQueue.Count);

            if (_isShuffled)
            {
                _shuffledQueue.Insert(index, file);
                _originalQueue.Add(file);
            }
            else
            {
                _originalQueue.Insert(index, file);
            }

            if (index <= _currentIndex) _currentIndex++;

            OnQueueChanged();
        }

        public async Task RemoveQueueItemAsync(MediaFile file)
        {
            int idx = ActiveQueue.IndexOf(file);
            if (idx >= 0) await RemoveQueueItemAtAsync(idx);
        }

        public async Task RemoveQueueItemAtAsync(int index)
        {
            if (index < 0 || index >= ActiveQueue.Count) return;

            bool wasPlaying = index == _currentIndex;
            var removed = ActiveQueue[index];

            if (_isShuffled)
            {
                _shuffledQueue.RemoveAt(index);
                _originalQueue.Remove(removed);
            }
            else
            {
                _originalQueue.RemoveAt(index);
            }

            if (index < _currentIndex)
            {
                _currentIndex--;
            }
            else if (wasPlaying)
            {
                _currentIndex = Math.Min(_currentIndex, ActiveQueue.Count - 1);

                if (ActiveQueue.Count == 0)
                    StopInternal(clearSource: true);
                else
                    await PlayAtIndexAsync(_currentIndex);
            }

            OnQueueChanged();
        }

        public async Task RemoveFilesFromQueueAsync(IEnumerable<string> paths)
        {
            if (paths == null)
                return;

            var pathSet = new HashSet<string>(
                paths.Where(p => !string.IsNullOrWhiteSpace(p)),
                StringComparer.OrdinalIgnoreCase);

            if (pathSet.Count == 0)
                return;

            bool deletingCurrent =
                _currentMedia != null &&
                pathSet.Contains(_currentMedia.Path);

            if (ActiveQueue.Count == 0)
                return;

            /*
             * If the currently playing item is being deleted, determine the next
             * surviving item BEFORE modifying the queue.
             */
            MediaFile nextMedia = null;

            if (deletingCurrent)
            {
                int nextIndex = _currentIndex + 1;

                while (nextIndex < ActiveQueue.Count)
                {
                    var candidate = ActiveQueue[nextIndex];

                    if (!pathSet.Contains(candidate.Path))
                    {
                        nextMedia = candidate;
                        break;
                    }

                    nextIndex++;
                }

                /*
                 * The current file may be the last item, so there may be no item
                 * after it. In that case we will clear the queue below.
                 */
            }

            /*
             * Remove from the original queue.
             */
            _originalQueue.RemoveAll(
                item => item != null && pathSet.Contains(item.Path));

            /*
             * Remove from shuffled queue as well.
             */
            _shuffledQueue.RemoveAll(
                item => item != null && pathSet.Contains(item.Path));

            if (deletingCurrent)
            {
                /*
                 * The current LibVLC media must be released before the physical
                 * file is deleted.
                 */
                StopInternal(clearSource: true);

                _currentMedia = null;
                _currentPosition = TimeSpan.Zero;
                _totalDuration = TimeSpan.Zero;
                _currentIndex = -1;

                Notify(nameof(CurrentMedia));
                Notify(nameof(CurrentIndex));
                Notify(nameof(CurrentPosition));
                Notify(nameof(TotalDuration));

                if (nextMedia != null)
                {
                    /*
                     * Find the surviving next item in the now-filtered queue.
                     */
                    int nextIndex = ActiveQueue.IndexOf(nextMedia);

                    if (nextIndex >= 0)
                    {
                        await PlayAtIndexAsync(nextIndex);
                    }
                    else
                    {
                        ClearQueue();
                        return;
                    }
                }
                else
                {
                    ClearQueue();
                    return;
                }
            }
            else
            {
                /*
                 * Current song wasn't deleted.
                 *
                 * Recalculate its index because items before it may have
                 * disappeared.
                 */
                if (_currentMedia != null)
                {
                    _currentIndex = ActiveQueue.IndexOf(_currentMedia);

                    if (_currentIndex < 0)
                        _currentIndex = -1;
                }
            }

            OnQueueChanged();
        }

        public void ClearQueue()
        {
            StopInternal(clearSource: true);
            _originalQueue.Clear();
            _shuffledQueue.Clear();
            _currentIndex = -1;
            _currentMedia = null;

            OnQueueChanged();
            MediaChanged?.Invoke(this, null);
            Notify(nameof(CurrentMedia));
        }

        /// <summary>
        /// Reorders the authoritative queue.
        ///
        /// When shuffle is disabled, ActiveQueue is _originalQueue, so the move
        /// directly changes the queue used by NextAsync/PreviousAsync.
        ///
        /// When shuffle is enabled, the move changes the current shuffled queue.
        /// The original queue remains unchanged so disabling shuffle can restore
        /// the user's original ordering.
        ///
        /// PlayerViewModel is synchronized through OnQueueChanged(), making the
        /// reordered queue visible to every UI that consumes the global view model.
        /// </summary>
        public void MoveQueueItem(int from, int to)
        {
            var activeQueue = ActiveQueue;

            if (activeQueue == null ||
                from < 0 ||
                from >= activeQueue.Count ||
                to < 0 ||
                to >= activeQueue.Count ||
                from == to)
            {
                return;
            }

            var item = activeQueue[from];

            activeQueue.RemoveAt(from);
            activeQueue.Insert(to, item);

            // Preserve the current playing item when its position changes.
            if (_currentMedia != null)
                _currentIndex = activeQueue.IndexOf(_currentMedia);
            else if (_currentIndex == from)
                _currentIndex = to;
            else if (from < _currentIndex && to >= _currentIndex)
                _currentIndex--;
            else if (from > _currentIndex && to <= _currentIndex)
                _currentIndex++;

            if (_currentIndex < 0 && activeQueue.Count > 0)
                _currentIndex = 0;

            PlayerViewModel.Instance.CurrentIndex = _currentIndex;

            // One authoritative synchronization point.
            OnQueueChanged();

            Notify(nameof(CurrentIndex));
        }

        /// <summary>
        /// Reorders an item using its MediaFile reference.
        /// This is the preferred API for drag/reorder UIs because it does not
        /// depend on a stale source index after the UI collection changes.
        /// </summary>
        public void MoveQueueItem(MediaFile item, int to)
        {
            if (item == null)
                return;

            var activeQueue = ActiveQueue;
            int from = activeQueue.IndexOf(item);

            if (from < 0)
                return;

            to = MathExtensions.Clamp(to, 0, activeQueue.Count - 1);

            MoveQueueItem(from, to);
        }

        // ───────────────────────────────────────────────────────────────────────
        //  Playback controls
        // ───────────────────────────────────────────────────────────────────────

        public async Task PlayAtIndexAsync(int index)
        {
            if (ActiveQueue.Count == 0 || index < 0 || index >= ActiveQueue.Count) return;
            _currentIndex = index;

            PlayerViewModel.Instance.CurrentIndex = _currentIndex;

            await LoadAndPlayAsync(ActiveQueue[index]);
        }

        public async Task PlayFileAsync(MediaFile file)
        {
            int idx = ActiveQueue.IndexOf(file);
            if (idx >= 0)
                await PlayAtIndexAsync(idx);
            else
            {
                AddQueueItemLast(file);
                await PlayAtIndexAsync(ActiveQueue.Count - 1);
            }
        }

        public void TogglePlayPause()
        {
            if (_player.State == VLCState.Playing)
                Pause();
            else
                Play();
        }

        public void Play()
        {
            if (_currentMedia is null) return;
            try
            {
                _player.Play();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Service Play failed: " + ex);
            }
        }

        public void Pause()
        {
            _player.Pause();
            _smtc.PlaybackStatus = MediaPlaybackStatus.Paused;
        }

        public void Stop() => StopInternal(clearSource: false);

        public async Task NextAsync()
        {
            if (ActiveQueue.Count == 0) return;

            if (_repeatMode == RepeatModeType.One)
            {
                await PlayAtIndexAsync(_currentIndex);
                return;
            }

            int next = _currentIndex + 1;

            if (next >= ActiveQueue.Count)
            {
                if (_repeatMode == RepeatModeType.All)
                {
                    if (_isShuffled) ReshuffleQueue();
                    next = 0;
                }
                else
                {
                    StopInternal(clearSource: false);
                    PlaybackCompleted?.Invoke(this, EventArgs.Empty);
                    return;
                }
            }

            await PlayAtIndexAsync(next);
        }

        public async Task PreviousAsync()
        {
            if (ActiveQueue.Count == 0) return;

            if (_player.Time > 3000) // ms
            {
                Seek(TimeSpan.Zero);
                return;
            }

            int prev = _currentIndex - 1;

            if (prev < 0)
                prev = _repeatMode == RepeatModeType.All ? ActiveQueue.Count - 1 : 0;

            await PlayAtIndexAsync(prev);
        }


        public void Seek(TimeSpan position)
        {
            if (!_player.IsSeekable) return;
            var clamped = TimeSpan.FromTicks(
                MathExtensions.Clamp(position.Ticks, 0, _totalDuration.Ticks));
            _player.Time = (long)clamped.TotalMilliseconds;
        }

        public void SeekRelative(double seconds)
            => Seek(TimeSpan.FromMilliseconds(_player.Time) + TimeSpan.FromSeconds(seconds));


        // ───────────────────────────────────────────────────────────────────────
        //  Shuffle & Repeat
        // ───────────────────────────────────────────────────────────────────────

        // Shuffle Mode
        public void ToggleShuffle()
        {
            _isShuffled = !_isShuffled;

            if (_isShuffled)
            {
                int originalPinIndex = _currentMedia is null
                    ? 0 : _originalQueue.IndexOf(_currentMedia);

                _shuffledQueue = BuildShuffledQueue(pinOriginalIndex: originalPinIndex);
                _currentIndex = 0;
            }
            else
            {
                _currentIndex = _currentMedia is null
                    ? 0 : _originalQueue.IndexOf(_currentMedia);

                if (_currentIndex < 0) _currentIndex = 0;
            }

            Notify(nameof(IsShuffled));
            Notify(nameof(Queue));
            QueueChanged?.Invoke(this, EventArgs.Empty);
        }

        private List<MediaFile> BuildShuffledQueue(int pinOriginalIndex = -1)
        {
            var shuffled = new List<MediaFile>(_originalQueue);
            MediaFile pinned = pinOriginalIndex >= 0 && pinOriginalIndex < _originalQueue.Count
                ? _originalQueue[pinOriginalIndex]
                : null;

            var rng = new Random();
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                MediaFile temp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = temp;
            }

            if (pinned != null)
            {
                shuffled.Remove(pinned);
                shuffled.Insert(0, pinned);
            }

            return shuffled;
        }


        public void SetShuffle(bool enabled)
        {
            if (_isShuffled != enabled) ToggleShuffle();
        }

        private void ReshuffleQueue()
        {
            _shuffledQueue = BuildShuffledQueue(pinOriginalIndex: -1);
        }

        // Repeat Mode

        public void CycleRepeatMode()
        {
            switch (_repeatMode)
            {
                case RepeatModeType.All:
                    SetRepeatMode(RepeatModeType.One);
                    break;
                case RepeatModeType.One:
                    SetRepeatMode(RepeatModeType.None);
                    break;
                case RepeatModeType.None:
                    SetRepeatMode(RepeatModeType.All);
                    break;
            }
        }

        public void SetRepeatMode(RepeatModeType mode)
        {
            _repeatMode = mode;
            Notify(nameof(RepeatMode));
            Notify(nameof(CanGoNext));
            Notify(nameof(CanGoPrevious));
        }

        // ───────────────────────────────────────────────────────────────────────
        //  LibVLCSharp — Load & Play
        // ───────────────────────────────────────────────────────────────────────

        private async Task LoadAndPlayAsync(MediaFile file)
        {
            if (file is null) return;

            SetLoading(true);
            _smtc.PlaybackStatus = MediaPlaybackStatus.Changing;

            _currentMedia = file;
            _currentPosition = TimeSpan.Zero;
            _totalDuration = file.Duration != null ? file.Duration : TimeSpan.Zero;

            MediaChanged?.Invoke(this, file);
            UpdateSmtcDisplay(file);
            Notify(nameof(CurrentMedia));
            Notify(nameof(CurrentIndex));
            Notify(nameof(CanGoNext));
            Notify(nameof(CanGoPrevious));

            try
            {
                // Same requirement as before: broadFileSystemAccess in Package.appxmanifest,
                // since we're handed a bare filesystem path.
                _ = await StorageFile.GetFileFromPathAsync(file.Path);

                DisposeCurrentMedia();

                _currentLibVlcMedia = new Media(_libVLC, file.Path, FromType.FromPath);

                bool started = _player.Play(_currentLibVlcMedia);
                if (!started)
                    throw new Exception("LibVLC failed to start playback for this file.");

                await TileService.ShowLiveTile(CurrentMedia);
            }
            catch (UnauthorizedAccessException ex)
            {
                await HandleLoadError(file, $"Access denied — add broadFileSystemAccess to Package.appxmanifest. ({ex.Message})");
            }
            catch (System.IO.FileNotFoundException)
            {
                await HandleLoadError(file, $"File not found: {file.Path}");
            }
            catch (Exception ex)
            {
                await HandleLoadError(file, ex.Message);
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async Task HandleLoadError(MediaFile file, string reason)
        {
            MediaFailed?.Invoke(this, $"[{file.FileName}] {reason}");
            await Task.Delay(300);
            await NextAsync();
        }

        private void DisposeCurrentMedia()
        {
            try { _currentLibVlcMedia?.Dispose(); } catch { /* ignore */ }
            _currentLibVlcMedia = null;
        }

        // ───────────────────────────────────────────────────────────────────────
        //  LibVLCSharp MediaPlayer event handlers
        //  NOTE: these fire on a libvlc-internal thread, never the UI thread —
        //  everything routes through RunOnUI(), same pattern as before.
        // ───────────────────────────────────────────────────────────────────────

        private void HookPlayerEvents()
        {
            _player.Playing += OnPlaying;
            _player.Paused += OnPaused;
            _player.Stopped += OnStopped;
            _player.EndReached += OnEndReached;
            _player.EncounteredError += OnEncounteredError;
            _player.TimeChanged += OnTimeChanged;
            _player.LengthChanged += OnLengthChanged;
            _player.VolumeChanged += OnVolumeChanged;
        }

        private void UnhookPlayerEvents()
        {
            _player.Playing -= OnPlaying;
            _player.Paused -= OnPaused;
            _player.Stopped -= OnStopped;
            _player.EndReached -= OnEndReached;
            _player.EncounteredError -= OnEncounteredError;
            _player.TimeChanged -= OnTimeChanged;
            _player.LengthChanged -= OnLengthChanged;
            _player.VolumeChanged -= OnVolumeChanged;
        }

        private void OnPlaying(object sender, EventArgs e)
        {
            RunOnUI(() =>
            {
                _isPlaying = true;
                _smtc.PlaybackStatus = MediaPlaybackStatus.Playing;
                SetLoading(false);
                PlaybackStateChanged?.Invoke(this, true);
                Notify(nameof(IsPlaying));
            });
        }

        private void OnPaused(object sender, EventArgs e)
        {
            RunOnUI(() =>
            {
                _isPlaying = false;
                _smtc.PlaybackStatus = MediaPlaybackStatus.Paused;
                PlaybackStateChanged?.Invoke(this, false);
                Notify(nameof(IsPlaying));
            });
        }

        private void OnStopped(object sender, EventArgs e)
        {
            RunOnUI(() =>
            {
                _isPlaying = false;
                _smtc.PlaybackStatus = MediaPlaybackStatus.Stopped;
                PlaybackStateChanged?.Invoke(this, false);
                Notify(nameof(IsPlaying));
            });
        }

        private void OnEndReached(object sender, EventArgs e)
        {
            // Firing NextAsync() directly from this callback re-enters LibVLC's own
            // thread; hop to the UI thread first, same as the old MediaEnded handler.
            RunOnUI(async () => await NextAsync());
        }

        private void OnEncounteredError(object sender, EventArgs e)
        {
            RunOnUI(async () =>
            {
                SetLoading(false);
                MediaFailed?.Invoke(this, "LibVLC playback error (see libvlc log).");
                await Task.Delay(200);
                await NextAsync();
            });
        }

        private void OnTimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            _currentPosition = TimeSpan.FromMilliseconds(e.Time);
            RunOnUI(() =>
            {
                PositionChanged?.Invoke(this, _currentPosition);
                Notify(nameof(CurrentPosition));
            });
        }

        private void OnLengthChanged(object sender, MediaPlayerLengthChangedEventArgs e)
        {
            RunOnUI(() =>
            {
                _totalDuration = TimeSpan.FromMilliseconds(e.Length);
                DurationChanged?.Invoke(this, _totalDuration);
                Notify(nameof(TotalDuration));
                PushSmtcTimeline();
            });
        }

        private void OnVolumeChanged(object sender, MediaPlayerVolumeChangedEventArgs e)
        {
            RunOnUI(() =>
            {
                // LibVLC reports volume as 0-100; expose/store it as 0.0-1.0.
                _volume = MathExtensions.Clamp(e.Volume / 100.0, 0.0, 1.0);
                VolumeChanged?.Invoke(this, _volume);
                Notify(nameof(Volume));
            });
        }

        // ───────────────────────────────────────────────────────────────────────
        //  System Media Transport Controls
        //  Obtained via GetForCurrentView() instead of player.SystemMediaTransportControls,
        //  since LibVLCSharp's MediaPlayer doesn't expose one. This API predates
        //  Windows.Media.Playback.MediaPlayer, so it is safe on very old SDKs.
        // ───────────────────────────────────────────────────────────────────────

        private void SetupSmtc()
        {
            _smtc = SystemMediaTransportControls.GetForCurrentView();

            _smtc.IsEnabled = true;
            _smtc.IsPlayEnabled = true;
            _smtc.IsPauseEnabled = true;
            _smtc.IsNextEnabled = true;
            _smtc.IsPreviousEnabled = true;
            _smtc.IsStopEnabled = true;
            _smtc.IsFastForwardEnabled = true;
            _smtc.IsRewindEnabled = true;

            _smtc.PlaybackStatus = MediaPlaybackStatus.Closed;

            _smtc.ButtonPressed += OnSmtcButton;
            _smtc.PlaybackPositionChangeRequested += OnSmtcSeek;
            _smtc.ShuffleEnabledChangeRequested += OnSmtcShuffle;
            _smtc.AutoRepeatModeChangeRequested += OnSmtcRepeat;
            // Note: there is no PlaybackRateChangeRequested hookup here — some very
            // old SMTC surfaces (esp. phone) never expose a rate control anyway.
        }

        private void OnSmtcButton(SystemMediaTransportControls _, SystemMediaTransportControlsButtonPressedEventArgs e)
        {
            RunOnUI(async () => {
                switch (e.Button)
                {
                    case SystemMediaTransportControlsButton.Play: Play(); break;
                    case SystemMediaTransportControlsButton.Pause: Pause(); break;
                    case SystemMediaTransportControlsButton.Stop: Stop(); break;
                    case SystemMediaTransportControlsButton.Next: await NextAsync(); break;
                    case SystemMediaTransportControlsButton.Previous: await PreviousAsync(); break;
                    case SystemMediaTransportControlsButton.FastForward: SeekRelative(10); break;
                    case SystemMediaTransportControlsButton.Rewind: SeekRelative(-10); break;
                }
            });

        }

        private void OnSmtcSeek(SystemMediaTransportControls _, PlaybackPositionChangeRequestedEventArgs e)
            => RunOnUI(() => Seek(e.RequestedPlaybackPosition));

        private void OnSmtcShuffle(SystemMediaTransportControls _, ShuffleEnabledChangeRequestedEventArgs e)
            => RunOnUI(() => SetShuffle(e.RequestedShuffleEnabled));

        private void OnSmtcRepeat(SystemMediaTransportControls _, AutoRepeatModeChangeRequestedEventArgs e)
        {
            RunOnUI(() =>
            {
                switch (e.RequestedAutoRepeatMode)
                {
                    case MediaPlaybackAutoRepeatMode.Track:
                        SetRepeatMode(RepeatModeType.One);
                        break;
                    case MediaPlaybackAutoRepeatMode.List:
                        SetRepeatMode(RepeatModeType.All);
                        break;
                }

            });
        }

        private void UpdateSmtcDisplay(MediaFile file)
        {
            if (file is null || _smtc is null)
                return;

            int displayVersion = Interlocked.Increment(ref _smtcDisplayVersion);

            RunOnUI(async () =>
            {
                try
                {
                    var upd = _smtc.DisplayUpdater;
                    upd.ClearAll();

                    if (file.Type == Enums.MediaType.Audio)
                    {
                        upd.Type = MediaPlaybackType.Music;
                        upd.MusicProperties.Title = file.Title;
                        upd.MusicProperties.Artist = "song • " + file.Artist;
                    }
                    else
                    {
                        upd.Type = MediaPlaybackType.Video;
                        upd.VideoProperties.Title = file.FileName;
                        upd.VideoProperties.Subtitle = "video";
                        //upd.VideoProperties.Subtitle = file.Extension.TrimStart('.').ToUpperInvariant() + " video";
                    }


                    try
                    {
                        /*
                     * The player UI already has a single artwork/cache pipeline.
                     * Use that pipeline here too:
                     *
                     *   1. MediaCacheManager -> memory/disk cache
                     *   2. If not cached, SongThumbnailRequest /
                     *      VideoThumbnailRequest populates MediaCacheManager
                     *
                     * We deliberately do NOT call StorageFile.GetThumbnailAsync()
                     * from the media service anymore.
                     */
                        MediaArtwork artwork = await LoadArtworkForSmtcAsync(file);

                        // Track changed while the artwork was loading.
                        if (displayVersion != Volatile.Read(ref _smtcDisplayVersion) ||
                            !ReferenceEquals(file, _currentMedia))
                        {
                            return;
                        }

                        if (artwork?.ImageBytes != null && artwork.ImageBytes.Length > 0)
                        {
                            var stream = new InMemoryRandomAccessStream();

                            await stream.WriteAsync(artwork.ImageBytes.AsBuffer());
                            stream.Seek(0);

                            // Keep the stream alive because SMTC may consume the
                            // RandomAccessStreamReference after Update() returns.
                            var previousStream = _smtcThumbnailStream;
                            _smtcThumbnailStream = stream;

                            upd.Thumbnail =
                                RandomAccessStreamReference.CreateFromStream(stream);

                            upd.Update();

                            try
                            {
                                previousStream?.Dispose();
                            }
                            catch
                            {
                                // Ignore old thumbnail stream disposal errors.
                            }

                            System.Diagnostics.Debug.WriteLine(
                                $"[SMTC] Artwork updated for: {file.FileName}");
                        }
                        else
                        {
                            // Metadata should still appear even when no cached artwork
                            // is available.
                            upd.Update();

                            System.Diagnostics.Debug.WriteLine(
                                $"[SMTC] No cached artwork available for: {file.FileName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("UpdateSmtcDisplay Generating failed: " + ex);
                    }

                    upd.Update();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("UpdateSmtcDisplay failed: " + ex);
                }
            });
        }


        private async Task<MediaArtwork> LoadArtworkForSmtcAsync(MediaFile file)
        {
            try
            {
                var cache = MediaCacheManager.Instance;

                if (file.Type == Enums.MediaType.Audio)
                {
                    string albumId = GetMediaId(
                        file,
                        "AlbumId",
                        "AlbumID",
                        "AlbumKey",
                        "AlbumGuid");

                    if (!string.IsNullOrWhiteSpace(albumId))
                    {
                        var cached = await cache.LoadAlbumArtworkAsync(albumId);
                        if (cached != null)
                            return cached;

                        // Cache miss: use the normal song thumbnail pipeline.
                        var request = new SongThumbnailRequest(albumId, file.Path);
                        return await request.LoadAsync();
                    }

                    // Some MediaFile versions expose only a generic Id.
                    string mediaId = GetMediaId(
                        file,
                        "Id",
                        "ID",
                        "MediaId",
                        "MediaID");

                    if (!string.IsNullOrWhiteSpace(mediaId))
                    {
                        var cached = await cache.LoadAlbumArtworkAsync(mediaId);
                        if (cached != null)
                            return cached;

                        var request = new SongThumbnailRequest(mediaId, file.Path);
                        return await request.LoadAsync();
                    }
                }
                else
                {
                    string videoId = GetMediaId(
                        file,
                        "VideoId",
                        "VideoID",
                        "Id",
                        "ID",
                        "MediaId",
                        "MediaID");

                    if (!string.IsNullOrWhiteSpace(videoId))
                    {
                        var cached = await cache.LoadVideoArtworkAsync(videoId);
                        if (cached != null)
                            return cached;

                        var request = new VideoThumbnailRequest(videoId, file.Path);
                        return await request.LoadAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SMTC] Artwork lookup failed: {ex}");
            }

            return null;
        }

        /// <summary>
        /// Reads a cache identifier from the MediaFile without assuming one
        /// particular MediaFile implementation.
        /// </summary>
        private static string GetMediaId(MediaFile file, params string[] propertyNames)
        {
            if (file is null || propertyNames is null || propertyNames.Length == 0)
                return null;

            Type type = file.GetType();

            foreach (string propertyName in propertyNames)
            {
                PropertyInfo property = type.GetProperty(
                    propertyName,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.IgnoreCase);

                if (property == null)
                    continue;

                object value = property.GetValue(file);

                if (value == null)
                    continue;

                string result = value.ToString();

                if (!string.IsNullOrWhiteSpace(result))
                    return result;
            }

            return null;
        }

        private void PushSmtcTimeline()
        {
            try
            {
                if (_smtc is null || _currentMedia is null) return;

                var tl = new SystemMediaTransportControlsTimelineProperties
                {
                    StartTime = TimeSpan.Zero,
                    MinSeekTime = TimeSpan.Zero,
                    EndTime = _totalDuration,
                    MaxSeekTime = _totalDuration,
                    Position = _currentPosition
                };
                _smtc.UpdateTimelineProperties(tl);
            }
            catch { /* ignore — SMTC not always available */ }
        }

        // ───────────────────────────────────────────────────────────────────────
        //  Private helpers
        // ───────────────────────────────────────────────────────────────────────

        private void StopInternal(bool clearSource)
        {
            _player.Pause();
            if (_player.IsSeekable)
                _player.Time = 0;

            _isPlaying = false;
            _smtc.PlaybackStatus = MediaPlaybackStatus.Stopped;
            PlaybackStateChanged?.Invoke(this, false);
            Notify(nameof(IsPlaying));

            if (clearSource)
            {
                TileService.StopLiveTile();
                _player.Stop();
                DisposeCurrentMedia();
            }
        }

        private void SetLoading(bool value)
        {
            _isLoading = value;
            RunOnUI(() => Notify(nameof(IsLoading)));
        }

        private void OnQueueChanged()
        {
            PlayerViewModel.Instance.ReplaceQueue(_isShuffled ? _shuffledQueue : _originalQueue);
            Notify(nameof(Queue));
            Notify(nameof(CanGoNext));
            Notify(nameof(CanGoPrevious));
            QueueChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RunOnUI(Action action)
        {
            if (_ui.HasThreadAccess) action();
            else _ = _ui.RunAsync(CoreDispatcherPriority.Normal, () => action());
        }

        private void Notify([CallerMemberName] string prop = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _timelineTimer?.Dispose();
            _timelineTimer = null;

            UnhookPlayerEvents();

            _smtc.ButtonPressed -= OnSmtcButton;
            _smtc.PlaybackPositionChangeRequested -= OnSmtcSeek;
            _smtc.ShuffleEnabledChangeRequested -= OnSmtcShuffle;
            _smtc.AutoRepeatModeChangeRequested -= OnSmtcRepeat;

            try
            {
                Interlocked.Increment(ref _smtcDisplayVersion);
                _smtcThumbnailStream?.Dispose();
            }
            catch
            {
                // Ignore thumbnail stream disposal errors.
            }

            _smtcThumbnailStream = null;

            DisposeCurrentMedia();
            _player.Dispose();
            // Deliberately NOT disposing the static _libVLC here — it's process-wide.
            // Dispose it once in App.xaml.cs OnSuspending/exit if you need a clean shutdown.
        }
    }
}
