using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Collections.Generic;
using System.Collections.Concurrent;
using NCaches.Core;
using System.Threading.Tasks;

namespace NCaches.InMemory {
    public abstract class InMemoryCacheBase<K, V, E> : ICache<K, V> where E : InMemoryCacheEntry<V> {
        protected ConcurrentDictionary<K, E> _cache;
        protected IClock _clock;

        protected ISubject<KeyValuePair<K, V>> _addedEntries;
        protected ISubject<KeyValuePair<K, V>> _removedEntries;
        protected ISubject<KeyValuePair<K, V>> _updatedEntries;

        protected InMemoryCacheBase(ConcurrentDictionary<K, E> cache, IClock clock) {
            _clock = clock;
            _cache = cache;

            _addedEntries = new Subject<KeyValuePair<K, V>>();
            _removedEntries = new Subject<KeyValuePair<K, V>>();
            _updatedEntries = new Subject<KeyValuePair<K, V>>();
        }

        public void Set(KeyValuePair<K, V> kvp) =>
            Set(kvp.Key, kvp.Value);

        public abstract void Set(K key, V value);

        public bool Remove(K key) {
            E entry;
            if (_cache.TryRemove(key, out entry)) {
                _removedEntries.OnNext(new KeyValuePair<K, V>(key, entry.Element));
                return true;
            }
            return false;
        }

        public abstract Task InvalidateEntries(Func<CacheEntry<V>, DateTimeOffset, Task<bool>> invalidate);

        public abstract Task<V> Get(K key);

        public IObservable<KeyValuePair<K, V>> AddedEntries => _addedEntries.AsObservable();
        public IObservable<KeyValuePair<K, V>> RemovedEntries => _removedEntries.AsObservable();
        public IObservable<KeyValuePair<K, V>> UpdatedEntries => _updatedEntries.AsObservable();
    }
}
