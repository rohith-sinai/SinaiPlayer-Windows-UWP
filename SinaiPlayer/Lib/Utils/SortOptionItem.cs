using SinaiPlayer.Lib.Enums; 

namespace SinaiPlayer.Lib.Utils
{
    public class SortOptionItem
    {
        public SortOption Value { get; }

        public string Display { get; }

        public SortOptionItem(SortOption value, string display)
        {
            Value = value;
            Display = display;
        }

        public override string ToString() => Display;
    }
}
