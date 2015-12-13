using System;

namespace NCaches.Core {

    public sealed class Clock : IClock {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
