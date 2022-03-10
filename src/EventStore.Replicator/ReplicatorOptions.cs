namespace EventStore.Replicator; 

public record ReplicatorOptions(bool RestartOnFailure, bool RunContinuously);