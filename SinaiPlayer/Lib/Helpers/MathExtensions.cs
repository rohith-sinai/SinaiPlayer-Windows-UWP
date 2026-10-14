using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SinaiPlayer.Lib.Helpers
{
    public static class MathExtensions
    {
        // MathClamp 
        public static int Clamp(this int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }


        public static double Clamp(this double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }


        public static long Clamp(this long value, long min, long max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
