using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NCaches.Core {
    public interface ICache<K, V> {
        IObservable<KeyValuePair<K, V>> AddedEntries { get; }
        IObservable<KeyValuePair<K, V>> UpdatedEntries { get; }
        IObservable<KeyValuePair<K, V>> RemovedEntries { get; }

        Task<V> Get(K key);

        void Set(K key, V value);
        void Set(KeyValuePair<K, V> kvp);
        
        Task InvalidateEntries(Func<CacheEntry<V>, DateTimeOffset, Task<bool>> invalidate);
    }

    public interface ISelfInvalidatingCache<K, V> : ICache<K, V> {
        void Set(K key, V value, Func<CacheEntry<V>, DateTimeOffset, Task<bool>> invalidation);
        void Set(KeyValuePair<K, V> kvp, Func<CacheEntry<V>, DateTimeOffset, Task<bool>> invalidation);
    }

    public interface ISelfUpdatingCache<K, V> : ICache<K, V> {
        void Set(K key, V value, Func<CacheEntry<V>, DateTimeOffset, Task<bool>> invalidation, Func<V, Task<V>> update);
        void Set(KeyValuePair<K, V> kvp, Func<CacheEntry<V>, DateTimeOffset, Task<bool>> invalidation, Func<V, Task<V>> update);
    }
}
