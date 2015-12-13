using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using NCaches.Core;

namespace NCaches.InMemory {
    public sealed class SimpleInMemoryCache<K, V> : InMemoryCacheBase<K, V, SimpleInMemoryCacheEntry<V>> {
        public SimpleInMemoryCache(IClock clock) : base(new ConcurrentDictionary<K, SimpleInMemoryCacheEntry<V>>(), clock) { }

        public override void Set(K key, V value) {
            var kvp = new KeyValuePair<K, V>(key, value);
            var entry = new SimpleInMemoryCacheEntry<V>(value, _clock.UtcNow);
            if (_cache.TryAdd(key, entry)) {
                _addedEntries.OnNext(kvp);
            }
            if (_cache.TryUpdate(key, entry, entry)) {
                _updatedEntries.OnNext(kvp);
            }
        }

        public override Task<V> Get(K key) {
            SimpleInMemoryCacheEntry<V> e;
            if (_cache.TryGetValue(key, out e)) {
                return Task.FromResult(e.Element);
            }
            else {
                return Task.FromResult(default(V));
            }
        }

        public override async Task InvalidateEntries(Func<CacheEntry<V>, DateTimeOffset, Task<bool>> invalidate) {
            foreach (var pair in _cache) {
                var isInvalid = await invalidate(pair.Value, _clock.UtcNow).ConfigureAwait(false);
                if (isInvalid)
                    Remove(pair.Key);
            }
        }
    }
}
