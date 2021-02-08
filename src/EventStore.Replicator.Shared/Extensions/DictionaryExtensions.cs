using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace EventStore.Replicator.Shared.Extensions {
    public static class DictionaryExtensions {
        public static async Task<T> GetOrAddAsync<T>(
            this ConcurrentDictionary<string, T> dict, string key, Func<Task<T>> get
        ) {
            if (dict.TryGetValue(key, out var value)) return value;

            var newValue = await get();
            dict.TryAdd(key, newValue);
            return newValue;
        }
    }
}
