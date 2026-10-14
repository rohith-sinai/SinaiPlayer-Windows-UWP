using SinaiPlayer.Lib.Enums;
using System;
using Windows.Storage;

namespace SinaiPlayer.Lib.Utils
{
    class PreferenceUtils
    {
        private static ApplicationDataContainer Settings =>
            ApplicationData.Current.LocalSettings;


        private const string keyTheme = "PREF_THEME";
        private const string keyLastMusicTab = "PREF_LAST_MUSIC_TAB";
        private const string keyLastNavPage = "PREF_LAST_NAV_PAGE";

        private const string keyVolume = "PREF_VOLUME";

        private const string keyFetchMetadata = "PREF_FETCH_METADATA";

        private const string keyShowNowPlayingSlideshow = "PREF_SHOW_NOWPLAYING_SLIDESHOW";

        private const string keySongsSortOrder = "PREF_SONGS_SORT_ORDER";
        private const string keyArtistsSortOrder = "PREF_ARTISTS_SORT_ORDER";
        private const string keyAlbumsSortOrder = "PREF_ALBUMS_SORT_ORDER";
        private const string keyVideosSortOrder = "PREF_VIDEOS_SORT_ORDER";
        private const string keyPlaylistsSortOrder = "PREF_PLAYLISTS_SORT_ORDER";

        private const string keyShownIntroPage = "PREF_SHOWN_INTRO_PAGE";

        private const string keyShowFeedbackDialog = "PREF_SHOW_FEEDBACK_DIALOG";
        private const string keyFeedbackDialogNextShowDate = "PREF_FEEDBACK_DIALOG_NEXT_SHOW_DATE";


        // Show Intro Page
        public static void SetShownIntroPage(bool value)
        {
            Settings.Values[keyShownIntroPage] = value;
        }

        public static bool GetShownIntroPage()
        {
            bool defaultValue = false;

            if (Settings.Values.TryGetValue(keyShownIntroPage, out object value))
            {
                if (value is bool isEnabled)
                {
                    return isEnabled;
                }
            }

            return defaultValue;
        }

        // Show Feedback Dialog
        public static void SetShowFeedbackDialog(bool value)
        {
            Settings.Values[keyShowFeedbackDialog] = value;
        }

        public static bool GetShowFeedbackDialog()
        {
            bool defaultValue = true;

            if (Settings.Values.TryGetValue(keyShowFeedbackDialog, out object value))
            {
                if (value is bool isEnabled)
                {
                    return isEnabled;
                }
            }

            return defaultValue;
        }

        // Feedback Dialog - Next Show Date
        // Stores the point in time at which the feedback dialog is next allowed to be shown.
        // Null means "not scheduled yet" (e.g. the app has never reached the check before).
        public static void SetFeedbackDialogNextShowDate(DateTimeOffset value)
        {
            Settings.Values[keyFeedbackDialogNextShowDate] = value;
        }

        public static DateTimeOffset? GetFeedbackDialogNextShowDate()
        {
            if (Settings.Values.TryGetValue(keyFeedbackDialogNextShowDate, out object value))
            {
                if (value is DateTimeOffset date)
                {
                    return date;
                }
            }

            return null;
        }

        // Theme
        public static void SetTheme(string theme)
        {
            Settings.Values[keyTheme] = theme;
        }

        public static string GetTheme()
        {
            return Settings.Values[keyTheme] as string ?? "auto";
        }


        // Fetch Metadata
        public static void SetFetchMetadataEnabled(bool value)
        {
            Settings.Values[keyFetchMetadata] = value;
        }

        public static bool GetIsEnabledFetchMetadata()
        {
            bool defaultValue = true;

            if (Settings.Values.TryGetValue(keyFetchMetadata, out object value))
            {
                if (value is bool isEnabled)
                {
                    return isEnabled;
                }
            }

            return defaultValue;
        }

        // NowPlaying Slideshow
        public static void SetNowPlayingSlideshowEnabled(bool value)
        {
            Settings.Values[keyShowNowPlayingSlideshow] = value;
        }

        public static bool GetIsEnabledNowPlayingSlideshow()
        {
            bool defaultValue = false;

            if (Settings.Values.TryGetValue(keyShowNowPlayingSlideshow, out object value))
            {
                if (value is bool isEnabled)
                {
                    return isEnabled;
                }
            }

            return defaultValue;
        }

        // Volume
        public static void SetVolume(double value)
        {
            Settings.Values[keyVolume] = value;
        }

        public static double GetVolume()
        {
            double defaultVolume = 100.0;

            // Direct type casting is cleaner and safer
            if (Settings.Values.TryGetValue(keyVolume, out object value))
            {
                if (value is double volume)
                {
                    return volume;
                }
            }

            return defaultVolume;
        }

        // LAST NAV TAB
        public static void SetLastNavTab(string tag)
        {
            Settings.Values[keyLastNavPage] = tag;
        }

        public static string GetLastNavTab()
        {
            return Settings.Values[keyLastNavPage] as string;
        }

        // LAST MUSIC TAB
        public static void SetLastMusicTab(int tabIndex)
        {
            Settings.Values[keyLastMusicTab] = tabIndex.ToString();
        }

        public static int GetLastMusicTab()
        {
            int dv = 0;
            var s = Settings.Values[keyLastMusicTab] as string;
            if (s == null || s == "") return dv;
            int? val = int.Parse(s);
            return val ?? dv;
        }

        // SONGS SORTING
        // Set Songs Sort Order
        public static void SetSongsSortOrder(SortOption option)
        {
            Settings.Values[keySongsSortOrder] = option.ToString();
        }

        // Get Songs Sort Order
        public static SortOption GetSongsSortOrder()
        {
            return ParseSortOrder(Settings.Values[keySongsSortOrder] as string, SortOption.Title);
        }

        // ALBUMS SORTING
        // Set Albums Sort Order
        public static void SetAlbumsSortOrder(SortOption option)
        {
            Settings.Values[keyAlbumsSortOrder] = option.ToString();
        }

        // Get Albums Sort Order
        public static SortOption GetAlbumsSortOrder()
        {
            return ParseSortOrder(Settings.Values[keyAlbumsSortOrder] as string, SortOption.Year);
        }

        // ARTISTS SORTING
        // Set Artists Sort Order
        public static void SetArtistsSortOrder(SortOption option)
        {
            Settings.Values[keyArtistsSortOrder] = option.ToString();
        }

        // Get Artists Sort Order
        public static SortOption GetArtistsSortOrder()
        {
            return SortOption.Title;
        }

        // VIDEOS SORTING
        // Set Videos Sort Order
        public static void SetVideosSortOrder(SortOption option)
        {
            Settings.Values[keyVideosSortOrder] = option.ToString();
        }

        // Get Videos Sort Order
        public static SortOption GetVideosSortsOrder()
        {
            return ParseSortOrder(Settings.Values[keyVideosSortOrder] as string, SortOption.Title);
        }

        // PLAYLISTS SORTING
        // Set Playlists Sort Order
        public static void SetPlaylistsSortsOrder(SortOption option)
        {
            Settings.Values[keyPlaylistsSortOrder] = option.ToString();
        }

        // Get Playlists Sort Order
        public static SortOption GetPlaylistsSortsOrder()
        {
            return ParseSortOrder(Settings.Values[keyPlaylistsSortOrder] as string, SortOption.Title);
        }


        private static SortOption ParseSortOrder(string raw, SortOption fallback)
        {
            return Enum.TryParse(raw, out SortOption result) ? result : fallback;
        }
    }
}