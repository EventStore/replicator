

// ReSharper disable UnusedAutoPropertyAccessor.Global

#nullable disable
namespace es_replicator.Settings; 

public record EsdbSettings {
    public string ConnectionString { get; init; }
    public string Protocol         { get; init; }
    public int    PageSize         { get; init; } = 1024;
}

public record Checkpoint {
    public string Path            { get; init; }
    public string Type            { get; init; } = "file";
    public int    CheckpointAfter { get; init; } = 1000;
    public string Database        { get; init; } = "replicator";
    public string InstanceId      { get; init; } = "default";
}

public record SinkSettings : EsdbSettings {
    public int    PartitionCount { get; init; } = 1;
    public string Router         { get; init; }
    public string Partitioner    { get; init; }
    public int    BufferSize     { get; init; } = 1000;
}

public record TransformSettings {
    public string Type       { get; init; } = "default";
    public string Config     { get; init; }
    public int    BufferSize { get; init; } = 1;
}

public record Filter {
    public string Type    { get; init; }
    public string Include { get; init; }
    public string Exclude { get; init; }
}

public record Replicator {
    public EsdbSettings      Reader           { get; init; }
    public SinkSettings      Sink             { get; init; }
    public bool              Scavenge         { get; init; }
    public bool              RestartOnFailure { get; init; } = true;
    public bool              RunContinuously  { get; init; } = true;
    public Checkpoint        Checkpoint       { get; init; } = new();
    public TransformSettings Transform        { get; init; } = new();
    public Filter[]          Filters          { get; init; }
}

public static class ConfigExtensions {
    public static T GetAs<T>(this IConfiguration configuration) where T : new() {
        T result = new();
        configuration.GetSection(typeof(T).Name).Bind(result);
        return result;
    }

    public static void AddOptions<T>(
        this IServiceCollection services, IConfiguration configuration
    )
        where T : class {
        services.Configure<T>(configuration.GetSection(typeof(T).Name));
    }
}
#nullable enable