function transform(stream, eventType, data, meta) {
    return null;
    
    const event = JSON.parse(data);
    const newEvent = {
        ...event,
        Data1: `new${event.Data1}`,
        NewProp: `${event.Id} - ${event.Data2}`
    };
    return {
        stream: `transformed${stream}`,
        eventType: `V2.${eventType}`,
        data: JSON.stringify(newEvent),
        meta
    }
}