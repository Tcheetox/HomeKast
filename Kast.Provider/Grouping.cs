using System.Collections;

namespace Kast.Provider
{
    public class Grouping<T, K> : IGrouping<T, K>
    {
        public T Key { get; }

        private readonly IEnumerable<K> _items;
        public Grouping(T key, IEnumerable<K> items) 
        { 
            Key = key;
            _items = items;
        }

        public Grouping(IGrouping<T, K> group)
        {
            Key = group.Key;
            _items = group;
        }

        public IEnumerator<K> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
