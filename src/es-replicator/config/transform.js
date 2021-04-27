function transform(stream, eventType, data, meta) {
    if (stream.length > 7) return undefined;
    
    const event = JSON.parse(data);
    const newEvent = {
        ...event,
        Data1: `new${event.Data1}`,
        NewProp: `${event.Id} - ${event.Data2}`
    };
    const et = stream.length <= 6 ? `V2.${eventType}` : null;
    return {
        stream: `transformed${stream}`,
        eventType: et,
        data: JSON.stringify(newEvent),
        meta
    }
}