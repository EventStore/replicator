using System.Text.Json;

namespace EventStore.Replicator.Esdb.Grpc.Internals; 

public static class MetaSerialization {
    internal static readonly JsonSerializerOptions StreamMetadataJsonSerializerOptions = new() {
        Converters = {
            StreamMetadataJsonConverter.Instance
        },
    };
}