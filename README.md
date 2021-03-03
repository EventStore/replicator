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

## Checkpoint

As the tool should keep track on the current offset of the reader, it uses the checkpoint store to persist the current reading position.

The default checkpoint store uses a file, specified in the configuration.

## Execution flow

Replicator will read all the events from the source, apply filters and transformations, then write events to the sink.

When the reader reaches the end of the source stream, it will stop.
After the sink buffer exhausts, the replication process will take a break for five seconds and will start again from the last known offset.
Therefore, the tool can run forever, effectively replicating all the events from the source to the sink.

## Filters

Currently, the default filters are:
- No filter (accepts all events)
- Scavenge filter (denies all events, which should've been scavenged due to TTL, max count or for deleted streams)

The scavenge filter is enabled by the configuration. If it is disabled, the "no filter" is used.

## Transforms

Currently, the default transforms are:
- No transform (copies the original event as-is as the propose event)
- Enriching transform. which is identical to "no transform", but adds some metadata (see Limitations)

## Limitations

### Speed

We aim for the global order of the source events to propagate to the sink. Therefore, the sink is limited to
have a single writer, which writes events one by one, as it must write to individuals streams. Naturally,
the seed of the writer is limited by the single write latency. For example, if the single write
latency is 5ms, it can write 1000/5 = 200 events per second, or 720,000 events per hour.

Therefore, the tool might not be suitable for very large databases.

In addition, if the source cluster receives new events at the rate that is exceeding the speed of writes
to the source cluster, the replication process won't ever be able to catch up and replicate the whole database.

### EventCreated

As the tool uses a regular public API for the writes, it is unable to manipulate the `EventCreated` system property as it is assigned by
the server. This property will get the current timestamp in the sink.

The enriching transform adds a metadata property `$originalCreatedDate` to the event itself to mitigate the issue.

### Event number

Each event in EventStoreDB has a number in the original stream. If the stream has been truncated, the numbering continues incrementally.
Therefore, such a stream might have the first event in it with a number `50`, if it was truncated before `50`.
The replicator is unable to change the event number, so numbering in the sink will always start from zero.

The enriching transform adds a metadata property `$originalEventNumber` to the event itself to mitigate the issue.

### Event position

As the source event position represents a physical place in the database where the event is stored, by moving it over to 
another database, this position will change.

The enriching transform adds a metadata property `$originalEventPosition` to the event itself to mitigate the issue.

## Configuration

The easiest way to configure Replicator is using environment variables.

The following configuration options are available:

| Variable | Description | Default |
| :------- | :---------- | :------ |
| `REPLICATOR_READER_PROTOCOL` | Protocol for the reader, `tcp` or `grpc` | `tcp` |
| `REPLICATOR_READER_CONNECTIONSTRING` | Connection string for the reader | Localhost TCP connection string |
| `REPLICATOR_SINK_PROTOCOL` | Protocol for the sink writer, `tcp` or `grpc` | `grpc` |
| `REPLICATOR_SINK_CONNECTIONSTRING` | Connection string for the sink writer | Localhost gRPC connection string |
| `REPLICATOR_SCAVENGE` | Enable scavenge filter | `true` |

## Deployment

### Helm

The Replicator can be easily deployed to Kubernetes using Helm.

Add the Helm repository:

```bash
helm repo add es-replicator https://eventstore.github.io/replicator
helm repo update
```

Set connection strings in the values file:
```yaml
replicator:
    reader:
        connectionString: "GossipSeeds=onprem-1.myorg.com:2113,onprem-2.myorg.com:2113,onprem-3.myorg.com:2113; HeartBeatTimeout=500; DefaultUserCredentials=admin:changeit;"
    sink:
        connectionString: "esdb://admin:changeit@clusterid.mesdb.eventstore.cloud:2113"
```

Deploy to Kubernetes:

```bash
helm install es-replicator es-replicator/es-replicator -f values.yml -n replicator
```

Wait a bit for the pod to start, then use port forwarding to the replicator service:

```bash
kubectl -n replicator port-forward svc/es-replicator 5000
```

Then, you can point your browser to `http://localhost:5000` and see the replicator UI.