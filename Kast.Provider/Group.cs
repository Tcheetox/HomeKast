using System.Collections;

namespace Kast.Provider
{
    public abstract class Group<T, K> : IGrouping<T, K>
    {
        public T Key { get; }

        private readonly IEnumerable<K> _items;
        protected Group(T key, IEnumerable<K> items) 
        { 
            Key = key;
            _items = items;
        }

        protected Group(IGrouping<T, K> group)
        {
            Key = group.Key;
            _items = group;
        }

        public IEnumerator<K> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
