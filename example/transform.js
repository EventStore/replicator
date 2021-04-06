function transform(stream, eventType, data, meta) {
    const evt = JSON.parse(data);
    return {
        stream: 'blah' + stream,
        eventType: 'new' + eventType,
        data: JSON.stringify({...evt, prop: 'test'}),
        metadata: meta
    }
}
