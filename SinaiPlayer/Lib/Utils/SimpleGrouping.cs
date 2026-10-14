using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SinaiPlayer.Lib.Utils
{
    public class SimpleGrouping<TKey, TElement> : IGrouping<TKey, TElement>
    {
        private readonly List<TElement> _items;
        public TKey Key { get; }

        public SimpleGrouping(TKey key, IEnumerable<TElement> items)
        {
            Key = key;
            _items = items.ToList();
        }

        public IEnumerator<TElement> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
