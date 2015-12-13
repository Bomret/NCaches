using System;

namespace NCaches.Core {
    public interface IClock {
        DateTimeOffset UtcNow { get; }
    }
}
