using System.Collections;
using System.Runtime.InteropServices;

namespace Kast.Provider
{
    #region MultiKey<TK1, TK2>
    public readonly struct MultiKey<TK1, TK2>
        where TK1 : notnull
        where TK2 : notnull
    {
        public readonly TK1 Key1;
        public readonly TK2 Key2;

        public MultiKey(TK1 key1, TK2 key2)
        {
            Key1 = key1;
            Key2 = key2;
        }
    }
    #endregion

    public class MultiConcurrentDictionary<TK1, TK2, TValue> : IDictionary<MultiKey<TK1, TK2>, TValue>
        where TK1 : notnull
        where TK2 : notnull
    {
        private readonly Dictionary<TK2, TK1> _keyDictionary;
        private readonly Dictionary<TK1, KeyValuePair<TK2, TValue>> _valDictionary;
        private readonly object _syncRoot = new();

        #region Constructors
        public MultiConcurrentDictionary(IEqualityComparer<TK1>? key1Comparer = null, IEqualityComparer<TK2>? key2Comparer = null)
        {
            _valDictionary = new(key1Comparer);
            _keyDictionary = new(key2Comparer);
        }

        public MultiConcurrentDictionary(
            IEnumerable<KeyValuePair<MultiKey<TK1, TK2>, TValue>> content,
            IEqualityComparer<TK1>? key1Comparer = null,
            IEqualityComparer<TK2>? key2Comparer = null)
        {
            _valDictionary = new Dictionary<TK1, KeyValuePair<TK2, TValue>>(content.Select(e => new KeyValuePair<TK1, KeyValuePair<TK2, TValue>>(e.Key.Key1, new KeyValuePair<TK2, TValue>(e.Key.Key2, e.Value))), key1Comparer);
            _keyDictionary = new Dictionary<TK2, TK1>(content.Select(e => new KeyValuePair<TK2, TK1>(e.Key.Key2, e.Key.Key1)), key2Comparer);
        }
        #endregion

        public TValue this[TK1 key]
        {
            get => _valDictionary[key].Value;
            set => throw new NotSupportedException();
        }

        public TValue this[TK2 key]
        {
            get => this[_keyDictionary[key]];
            set => throw new NotSupportedException();
        }

        public TValue this[MultiKey<TK1, TK2> multiKey]
        {
            get => this[multiKey.Key1, multiKey.Key2];
            set => _ = AddOrUpdate(multiKey, value);
        }

        public TValue this[TK1 key1, TK2 key2]
        {
            get => _valDictionary[key1].Value;
            set => _ = AddOrUpdate(key1, key2, value);
        }

        ICollection<MultiKey<TK1, TK2>> IDictionary<MultiKey<TK1, TK2>, TValue>.Keys => (ICollection<MultiKey<TK1, TK2>>)Keys;
        public IEnumerable<MultiKey<TK1, TK2>> Keys => CopyK1K2();
        public IEnumerable<TK1> Keys1 => CopyK1();
        public IEnumerable<TK2> Keys2 => CopyK2();

        ICollection<TValue> IDictionary<MultiKey<TK1, TK2>, TValue>.Values => (ICollection<TValue>)Values;
        public IEnumerable<TValue> Values => CopyV();

        public int Count => _valDictionary.Count;

        public bool ContainsKey(MultiKey<TK1, TK2> multiKey) => ContainsKey(multiKey.Key1);
        public bool ContainsKey(TK1 key) => _valDictionary.ContainsKey(key);
        public bool ContainsKey(TK2 key) => _keyDictionary.ContainsKey(key);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<KeyValuePair<MultiKey<TK1, TK2>, TValue>> GetEnumerator()
        {
            foreach (var entry in CopyK1K2V())
                yield return entry;
        }

        public void Clear()
        {
            lock (_syncRoot)
            {
                _valDictionary.Clear();
                _keyDictionary.Clear();
            }
        }

        public bool TryGetValue(MultiKey<TK1, TK2> multiKey, out TValue value)
            => TryGetValue(multiKey.Key1, out value!);

        public bool TryGetValue(TK1 key, out TValue? value)
        {
            if (_valDictionary.TryGetValue(key, out KeyValuePair<TK2, TValue> entry))
            {
                value = entry.Value;
                ThrowOnInconsistency();
                return true;
            }

            value = default;
            ThrowOnInconsistency();
            return false;
        }

        public bool TryGetValue(TK2 key, out TValue value)
        {
            if (_keyDictionary.TryGetValue(key, out TK1? key1) && _valDictionary.TryGetValue(key1, out var entry))
            {
                value = entry.Value;
                ThrowOnInconsistency();
                return true;
            }

            value = default!;
            ThrowOnInconsistency();
            return false;
        }

        public TValue GetOrAdd(MultiKey<TK1, TK2> multiKey, TValue value)
            => GetOrAdd(multiKey.Key1, multiKey.Key2, value, out _);
        public TValue GetOrAdd(MultiKey<TK1, TK2> multiKey, TValue value, out bool added)
            => GetOrAdd(multiKey.Key1, multiKey.Key2, value, out added);
        public TValue GetOrAdd(KeyValuePair<MultiKey<TK1, TK2>, TValue> entry)
            => GetOrAdd(entry.Key.Key1, entry.Key.Key2, entry.Value, out _);
        public TValue GetOrAdd(KeyValuePair<MultiKey<TK1, TK2>, TValue> entry, out bool added)
            => GetOrAdd(entry.Key.Key1, entry.Key.Key2, entry.Value, out added);
        public TValue GetOrAdd(TK1 key1, TK2 key2, TValue value)
            => GetOrAdd(key1, key2, value, out bool _);

        public TValue GetOrAdd(TK1 key1, TK2 key2, TValue value, out bool added)
        {
            lock (_syncRoot)
            {
                ref var keyRefOrNew = ref CollectionsMarshal.GetValueRefOrAddDefault(_keyDictionary, key2, out bool existingKey);
                ref var valOrNew = ref CollectionsMarshal.GetValueRefOrAddDefault(_valDictionary, key1, out bool existingEntry);
                if (existingKey && existingEntry)
                {
                    added = false;
                    ThrowOnInconsistency();
                    return valOrNew.Value;
                }

                added = true;
                if (!existingEntry && !existingKey)
                {
                    keyRefOrNew = key1;
                    valOrNew = new KeyValuePair<TK2, TValue>(key2, value);
                }
                else if (!existingKey)
                {
                    _keyDictionary.Remove(valOrNew.Key);
                    keyRefOrNew = key1;
                }
                else
                {
                    _valDictionary.Remove(keyRefOrNew!);
                    valOrNew = new KeyValuePair<TK2, TValue>(key2, value);
                }
                ThrowOnInconsistency();
                return value;
            }
        }

        private void ThrowOnInconsistency()
        {
#if DEBUG
            if (_valDictionary.Count != _keyDictionary.Count)
                throw new InvalidOperationException($"{nameof(MultiConcurrentDictionary<TK1, TK2, TValue>)} count mismatch: {nameof(_valDictionary)} ({_valDictionary.Count}) vs {nameof(_keyDictionary)} ({_keyDictionary.Count}) ");
#endif        
        }

        public TValue AddOrUpdate(MultiKey<TK1, TK2> multiKey, TValue value, out bool added)
            => AddOrUpdate(multiKey.Key1, multiKey.Key2, value, out added);
        public TValue AddOrUpdate(MultiKey<TK1, TK2> multiKey, TValue value)
            => AddOrUpdate(multiKey.Key1, multiKey.Key2, value, out bool _);
        public TValue AddOrUpdate(KeyValuePair<MultiKey<TK1, TK2>, TValue> entry, out bool added)
            => AddOrUpdate(entry.Key.Key1, entry.Key.Key2, entry.Value, out added);
        public TValue AddOrUpdate(KeyValuePair<MultiKey<TK1, TK2>, TValue> entry)
            => AddOrUpdate(entry.Key.Key1, entry.Key.Key2, entry.Value, out bool _);
        public TValue AddOrUpdate(TK1 key1, TK2 key2, TValue value)
            => AddOrUpdate(key1, key2, value, out bool _);

        public TValue AddOrUpdate(TK1 key1, TK2 key2, TValue value, out bool added)
        {
            lock (_syncRoot)
            {
                ref var keyRefOrNew = ref CollectionsMarshal.GetValueRefOrAddDefault(_keyDictionary, key2, out bool existingKey);
                ref var valOrNew = ref CollectionsMarshal.GetValueRefOrAddDefault(_valDictionary, key1, out bool existingEntry);
                if (!existingKey && !existingEntry)
                {
                    keyRefOrNew = key1;
                    valOrNew = new KeyValuePair<TK2, TValue>(key2, value);
                    added = true;
                    ThrowOnInconsistency();
                    return value;
                }

                added = false;
                if (existingKey && existingEntry)
                {
                    keyRefOrNew = key1;
                    valOrNew = new KeyValuePair<TK2, TValue>(key2, value);
                }
                else if (existingKey)
                {
                    _valDictionary.Remove(keyRefOrNew!);
                    keyRefOrNew = key1;
                    valOrNew = new KeyValuePair<TK2, TValue>(key2, value);
                }
                else
                {
                    _keyDictionary.Remove(valOrNew.Key);
                    valOrNew = new KeyValuePair<TK2, TValue>(key2, value);
                    keyRefOrNew = key1;
                }

                ThrowOnInconsistency();
                return value;
            }
        }

        public bool TryRemove(MultiKey<TK1, TK2> multiKey) => TryRemove(multiKey.Key1, multiKey.Key2, out _);
        public bool TryRemove(MultiKey<TK1, TK2> multiKey, out TValue? value) => TryRemove(multiKey.Key1, multiKey.Key2, out value);
        public bool TryRemove(TK1 key1, TK2 key2) => TryRemove(key1, key2, out _);

        public bool TryRemove(TK1 key1, TK2 key2, out TValue? value)
        {
            lock (_syncRoot)
            {
                if (_keyDictionary.Remove(key2) && _valDictionary.Remove(key1, out var entry))
                {
                    value = entry.Value;
                    ThrowOnInconsistency();
                    return true;
                }

                value = default;
                ThrowOnInconsistency();
                return false;
            }
        }

        public bool TryRemove(TK2 key) => TryRemove(key, out _);

        public bool TryRemove(TK2 key, out TValue? value)
        {
            lock (_syncRoot)
            {
                if (_keyDictionary.Remove(key, out TK1? key1) && _valDictionary.Remove(key1, out var entry))
                {
                    value = entry.Value;
                    ThrowOnInconsistency();
                    return true;
                }

                value = default;
                ThrowOnInconsistency();
                return false;
            }
        }

        public bool TryRemove(TK1 key) => TryRemove(key, out _);

        public bool TryRemove(TK1 key, out TValue? value)
        {
            lock (_syncRoot)
            {
                if (_valDictionary.Remove(key, out var entry) && _keyDictionary.Remove(entry.Key))
                {
                    value = entry.Value;
                    ThrowOnInconsistency();
                    return true;
                }

                value = default;
                ThrowOnInconsistency();
                return false;
            }
        }

        private IReadOnlyList<TValue> CopyV()
        {
            lock (_syncRoot)
            {
                return _valDictionary.Values.Select(e => e.Value).ToList();
            }
        }

        private IReadOnlyList<TK1> CopyK1()
        {
            lock (_syncRoot)
            {
                return _valDictionary.Keys.ToList();
            }
        }

        private IReadOnlyList<TK2> CopyK2()
        {
            lock (_syncRoot)
            {
                return _keyDictionary.Keys.ToList();
            }
        }

        private IReadOnlyList<MultiKey<TK1, TK2>> CopyK1K2()
        {
            lock (_syncRoot)
            {
                return _valDictionary.Select(e => new MultiKey<TK1, TK2>(e.Key, e.Value.Key)).ToList();
            }
        }

        private IReadOnlyList<KeyValuePair<MultiKey<TK1, TK2>, TValue>> CopyK1K2V()
        {
            lock (_syncRoot)
            {
                return _valDictionary
                    .Select(e => new KeyValuePair<MultiKey<TK1, TK2>, TValue>(new MultiKey<TK1, TK2>(e.Key, e.Value.Key), e.Value.Value))
                    .ToList();
            }
        }

        bool ICollection<KeyValuePair<MultiKey<TK1, TK2>, TValue>>.IsReadOnly
            => ((IDictionary<TK1, KeyValuePair<TK2, TValue>>)_valDictionary).IsReadOnly;

        void IDictionary<MultiKey<TK1, TK2>, TValue>.Add(MultiKey<TK1, TK2> multiKey, TValue value)
            => AddOrUpdate(multiKey, value);

        bool IDictionary<MultiKey<TK1, TK2>, TValue>.Remove(MultiKey<TK1, TK2> multiKey)
            => TryRemove(multiKey);
        bool ICollection<KeyValuePair<MultiKey<TK1, TK2>, TValue>>.Remove(KeyValuePair<MultiKey<TK1, TK2>, TValue> item)
            => TryRemove(item.Key);

        void ICollection<KeyValuePair<MultiKey<TK1, TK2>, TValue>>.Add(KeyValuePair<MultiKey<TK1, TK2>, TValue> entry)
            => AddOrUpdate(entry.Key, entry.Value);

        bool ICollection<KeyValuePair<MultiKey<TK1, TK2>, TValue>>.Contains(KeyValuePair<MultiKey<TK1, TK2>, TValue> item)
            => ContainsKey(item.Key);

        void ICollection<KeyValuePair<MultiKey<TK1, TK2>, TValue>>.CopyTo(KeyValuePair<MultiKey<TK1, TK2>, TValue>[] array, int arrayIndex)
            => this.ToArray().CopyTo(array, arrayIndex);
    }
}
