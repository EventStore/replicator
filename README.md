# Event Store Replicator

The `Replicator` tool aims to live copy events from any source to any target. The tool can run continuously, but it will stop reading as soon as the reader has no more events to read. 

Additional features:
- Filter out (drop) events
- Transform events
- Propagate streams metadata
- Propagate streams deletion

Implemented readers and writers:
- EventStoreDB gRPC (v20+)
- EventStore TCP (v5+)

EventStoreDB readers implement one additional filter, which executes during the read. This filter checks metadata for all streams it gets events for and tries (best effort) to skip events, which should've been scavenged based on previous deletions, max count and max age.
