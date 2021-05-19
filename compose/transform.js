function transform(original) {
    log.info("Transforming event {Type} from {Stream}", original.eventType, original.stream);

    if (original.stream.length > 7) return undefined;
    
    const newEvent = {
        ...original.data,
        Data1: `new${original.data.Data1}`,
        NewProp: `${original.data.Id} - ${original.data.Data2}`
    };
    return {
        stream: `transformed${original.stream}`,
        eventType: `V2.${original.eventType}`,
        data: newEvent,
        meta: original.meta
    }
}