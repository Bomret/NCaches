using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NCaches.Core;

namespace NCaches.InMemory {
    public sealed class SelfInvalidatingInMemoryCache<K, V> : InMemoryCacheBase<K, V, InvalidateableInMemoryCacheEntry<V>>, ISelfInvalidatingCache<K, V> {
        static readonly Func<CacheEntry<V>, DateTimeOffset, Task<bool>> NeverExpire = (_, __) => Task.FromResult(false);

        public SelfInvalidatingInMemoryCache(IClock clock) : base(new ConcurrentDictionary<K, InvalidateableInMemoryCacheEntry<V>>(), clock) {}

        public override void Set(K key, V value) =>
            Set(key, value, NeverExpire);

        public void Set(K key, V value, Func<CacheEntry<V>, DateTimeOffset, Task<bool>> invalidation) {
            var kvp = new KeyValuePair<K, V>(key, value);
            var entry = new InvalidateableInMemoryCacheEntry<V>(value, _clock.UtcNow, invalidation);
            if (_cache.TryAdd(key, entry)) {
                _addedEntries.OnNext(kvp);
            }
            if (_cache.TryUpdate(key, entry, entry)) {
                _updatedEntries.OnNext(kvp);
            }
        }

        public void Set(KeyValuePair<K, V> kvp, Func<CacheEntry<V>, DateTimeOffset, Task<bool>> invalidation) =>
            Set(kvp.Key, kvp.Value, invalidation);
        
        public override async Task<V> Get(K key) {
            InvalidateableInMemoryCacheEntry<V> entry;
            if (_cache.TryGetValue(key, out entry)) {
                var isInvalid = await entry.IsInvalid(_clock.UtcNow).ConfigureAwait(false);
                if (!isInvalid) {
                    return entry.Element;
                }
                Remove(key);
            }

            return default(V);
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
