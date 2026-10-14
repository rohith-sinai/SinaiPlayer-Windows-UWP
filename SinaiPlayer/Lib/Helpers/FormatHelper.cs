using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SinaiPlayer.Lib.Helpers
{
    public static class FormatHelper
    {
        public static string FormatTimeToDisplayMillis(TimeSpan time)
        {

            if (time.TotalSeconds < 0 || double.IsNaN(time.TotalSeconds) || double.IsInfinity(time.TotalSeconds))
            {
                return "00:00";
            }


            if (time.TotalHours >= 1)
            {
                return time.ToString(@"hh\:mm\:ss");
            }
            else
            {
                return time.ToString(@"mm\:ss");
            }
        }

        public static string FormatFileSize(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < units.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {units[order]}";
        }

    }
}
