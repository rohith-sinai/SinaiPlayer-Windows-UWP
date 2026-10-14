using System; 

namespace SinaiPlayer.Lib.Helpers
{
    public static class SortGroupingHelper
    {
        // "A", "B", "C"... for a normal title/name; non-letters bucket into "#".
        public static string GetLetterKey(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "#";

            char c = char.ToUpperInvariant(text.Trim()[0]);

            return char.IsLetter(c) ? c.ToString() : "#";
        }

        // Alphabetical, with the "#" bucket always sorted last.
        public static int CompareTextKeys(string a, string b)
        {
            if (a == "#" && b == "#") return 0;
            if (a == "#") return 1;
            if (b == "#") return -1;

            return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
        }

        // Numeric, with "Unknown" always sorted last.
        public static int CompareYearKeys(string a, string b)
        {
            bool aUnknown = a == "Unknown";
            bool bUnknown = b == "Unknown";

            if (aUnknown && bUnknown) return 0;
            if (aUnknown) return 1;
            if (bUnknown) return -1;

            int ai = int.TryParse(a, out var av) ? av : 0;
            int bi = int.TryParse(b, out var bv) ? bv : 0;

            return bi.CompareTo(ai);
        }
    }
}
