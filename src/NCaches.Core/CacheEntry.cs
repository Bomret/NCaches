using System;

namespace NCaches.Core {
    public abstract class CacheEntry<TElem> {
        protected CacheEntry(TElem element, DateTimeOffset addedAt) {
            Element = element;
            AddedAt = addedAt;
        }

        protected CacheEntry(TElem element, DateTimeOffset addedAt, DateTimeOffset updatedAt) 
            : this(element, addedAt) {
            LastUpdatedAt = updatedAt;
        }

        public DateTimeOffset AddedAt { get; }
        public TElem Element { get; }
        public DateTimeOffset? LastUpdatedAt { get; }
    }
}
