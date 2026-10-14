using System;
using System.Collections.Generic;
using System.Text;
using Windows.ApplicationModel.Resources;

namespace SinaiPlayer.Lib.Helpers
{
    public static class LocalizationHelper
    {
        private static readonly ResourceLoader _resourceLoader = ResourceLoader.GetForCurrentView();

        public static string GetString(string key)
        {
            return _resourceLoader.GetString(key);
        }

        public static string Format(string key, params object[] args)
        {
            return string.Format(GetString(key), args);
        }
    }
}
