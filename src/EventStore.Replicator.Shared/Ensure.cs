using System;

namespace EventStore.Replicator.Shared {
    public static class Ensure {
        public static string NotEmpty(string? value, string parameter)
            => string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentNullException(parameter)
                : value;
    }
}