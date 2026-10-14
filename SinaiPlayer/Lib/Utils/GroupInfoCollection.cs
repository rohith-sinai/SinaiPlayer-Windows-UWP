using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SinaiPlayer.Lib.Utils
{
    public class GroupInfoCollection<T> : List<T>
    {
        public object Key { get; set; }

        public GroupInfoCollection() { }

        public GroupInfoCollection(object key, IEnumerable<T> items) : base(items)
        {
            Key = key;
        }
    }
}
