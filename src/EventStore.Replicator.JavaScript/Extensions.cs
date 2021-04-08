using System.Text;

namespace EventStore.Replicator.JavaScript {
    public static class Extensions {
        public static string AsUtf8String(this byte[] data) => Encoding.UTF8.GetString(data);

        public static byte[] AsUtf8Bytes(this string data) => Encoding.UTF8.GetBytes(data);
    }
}