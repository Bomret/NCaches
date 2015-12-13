using System;
using System.Threading.Tasks;
using NCaches.Core;

namespace NCaches.InMemory {
    public abstract class InMemoryCacheEntry<T> : CacheEntry<T> {
        protected InMemoryCacheEntry(T elem, DateTimeOffset added) : base(elem, added) { }
        protected InMemoryCacheEntry(T elem, DateTimeOffset added, DateTimeOffset updated) : base(elem, added, updated) { }
    }

    public sealed class SimpleInMemoryCacheEntry<T> : InMemoryCacheEntry<T> {
        public SimpleInMemoryCacheEntry(T elem, DateTimeOffset added) : base(elem, added) { }
        public SimpleInMemoryCacheEntry(T elem, DateTimeOffset added, DateTimeOffset updated) : base(elem, added, updated) { }
    }

    public sealed class InvalidateableInMemoryCacheEntry<T> : InMemoryCacheEntry<T> {
        readonly Func<CacheEntry<T>, DateTimeOffset, Task<bool>> _invalidation;

        public InvalidateableInMemoryCacheEntry(T elem, DateTimeOffset added, Func<CacheEntry<T>, DateTimeOffset, Task<bool>> invalidate) : base(elem, added) {
            _invalidation = invalidate;
        }

        public InvalidateableInMemoryCacheEntry(T elem, DateTimeOffset added, DateTimeOffset updated, Func<CacheEntry<T>, DateTimeOffset, Task<bool>> invalidate) : base(elem, added, updated) {
            _invalidation = invalidate;
        }

        public Task<bool> IsInvalid(DateTimeOffset timeToCheck) =>
            _invalidation(this, timeToCheck);
    }

    public sealed class UpdateableInMemoryCacheEntry<T> : InMemoryCacheEntry<T> {
        readonly Func<CacheEntry<T>, DateTimeOffset, Task<bool>> _invalidation;
        private Func<T, Task<T>> _update;

        public UpdateableInMemoryCacheEntry(T elem, DateTimeOffset added, Func<CacheEntry<T>, DateTimeOffset, Task<bool>> invalidate, Func<T, Task<T>> update) : base(elem, added) {
            _invalidation = invalidate;
            _update = update;
        }

        public UpdateableInMemoryCacheEntry(T elem, DateTimeOffset added, DateTimeOffset updated, Func<CacheEntry<T>, DateTimeOffset, Task<bool>> invalidate, Func<T, Task<T>> update) : base(elem, added, updated) {
            _invalidation = invalidate;
            _update = update;
        }

        public Task<bool> IsInvalid(DateTimeOffset timeToCheck) => _invalidation(this, timeToCheck);

        public async Task<UpdateableInMemoryCacheEntry<T>> Update(T value, IClock clock) {
            var newValue = await _update(value);
            return new UpdateableInMemoryCacheEntry<T>(newValue, AddedAt, clock.UtcNow, _invalidation, _update);
        }
    }
}
