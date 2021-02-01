using System.Text.RegularExpressions;

namespace EventStore.Replicator.Shared.Extensions {
    static class RegexExtensions {
        public static bool IsNullOrMatch(this Regex? regex, string value) => regex == null || regex.IsMatch(value);

        public static bool IsNullOrDoesntMatch(this Regex? regex, string value) => regex == null || !regex.IsMatch(value);
    }
}
