using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace EventStore.Replicator.Shared.Extensions {
    public static class DictionaryExtensions {
        public static async Task<T> GetOrAdd<T>(
            this ConcurrentDictionary<string, T> dict, string key, Func<string, Task<T>> get
        ) {
            if (dict.TryGetValue(key, out var value)) return value;

            var newValue = await get(key);
            dict.TryAdd(key, newValue);
            return newValue;
        }
    }
}
