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

## Build

```sh
docker build .
```

The default target architecture is amd64 (x86_64).

You can build targeting arm64 (e.g to execute on Apple Silicon) like so:

```sh
docker build --build-arg RUNTIME=linux-arm64 .
```

## Documentation

Find out the details, including deployment scenarios, in the [documentation](https://replicator.eventstore.org).

## Support

Event Store Replicator is provided as-is, without any warranty, and is not covered by Event Store support contract.

If you experience an issue when using Replicator, or you'd like to suggest a new feature, please open an issue in this GitHub project.
