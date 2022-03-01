// ReSharper disable SuggestBaseTypeForParameter

namespace EventStore.Replicator.Shared.Contracts; 

public record StreamAcl(
    string[]? ReadRoles,
    string[]? WriteRoles,
    string[]? DeleteRoles,
    string[]? MetaReadRoles,
    string[]? MetaWriteRoles
);