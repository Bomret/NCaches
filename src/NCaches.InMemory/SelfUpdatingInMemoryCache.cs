using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using NCaches.Core;

namespace NCaches.InMemory {
    public sealed class SelfUpdatingInMemoryCache<K, V> : InMemoryCacheBase<K, V, UpdateableInMemoryCacheEntry<V>>, ISelfUpdatingCache<K, V> {
        static readonly Func<CacheEntry<V>, DateTimeOffset, Task<bool>> NeverExpire = (_, __) => Task.FromResult(false);
        static readonly Func<V, Task<V>> UpdatedWithKnownValue = value => Task.FromResult(value);

        public SelfUpdatingInMemoryCache(IClock clock) 
            : base(new ConcurrentDictionary<K, UpdateableInMemoryCacheEntry<V>>(), clock) {}

        public override void Set(K key, V value) =>
            Set(key, value, NeverExpire, UpdatedWithKnownValue);

        public void Set(KeyValuePair<K, V> kvp, Func<CacheEntry<V>, DateTimeOffset, Task<bool>> invalidation, Func<V, Task<V>> update) =>
            Set(kvp.Key, kvp.Value, invalidation, update);

        public void Set(K key, V value, Func<CacheEntry<V>, DateTimeOffset, Task<bool>> invalidation, Func<V, Task<V>> update) {
            var kvp = new KeyValuePair<K, V>(key, value);
            var entry = new UpdateableInMemoryCacheEntry<V>(value, _clock.UtcNow, invalidation, update);
            if (_cache.TryAdd(key, entry)) {
                _addedEntries.OnNext(kvp);
            }
            if (_cache.TryUpdate(key, entry, entry)) {
                _updatedEntries.OnNext(kvp);
            }
        }

        public override async Task<V> Get(K key) {
            UpdateableInMemoryCacheEntry<V> entry;
            if (_cache.TryGetValue(key, out entry)) {
                var isInvalid = await entry.IsInvalid(_clock.UtcNow).ConfigureAwait(false);
                if (!isInvalid) {
                    return entry.Element;
                }
                await RunUpdate(key, entry);
            }
            return default(V);
        }

        private async Task RunUpdate(K key, UpdateableInMemoryCacheEntry<V> entry) {
            var newEntry = await entry.Update(entry.Element, _clock).ConfigureAwait(false);
            if (_cache.TryUpdate(key, entry, entry)) {
                _updatedEntries.OnNext(new KeyValuePair<K, V>(key, newEntry.Element));
            }
        }

        public override async Task InvalidateEntries(Func<CacheEntry<V>, DateTimeOffset, Task<bool>> invalidate) {
            foreach (var pair in _cache) {
                var isInvalid = await invalidate(pair.Value, _clock.UtcNow).ConfigureAwait(false);
                if (isInvalid)
                    await RunUpdate(pair.Key, pair.Value).ConfigureAwait(false);
            }
        }
    }
}
