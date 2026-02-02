namespace RabbitMQCoreClient.BatchQueueSender;

/// <summary>
/// Implementation of the service for sending events to the data bus.
/// </summary>
internal sealed class EventsWriter : IEventsWriter
{
    readonly IQueueService _queueService;

    /// <summary>
    /// Create new object of <see cref="EventsWriter"/>.
    /// </summary>
    /// <param name="queueService">Queue publisher.</param>
    public EventsWriter(IQueueService queueService)
    {
        _queueService = queueService;
    }

    /// <inheritdoc />
    public async Task WriteBatch(IEnumerable<EventItem> events, string routingKey)
    {
        if (!events.Any())
            return;

        await _queueService.SendBatchAsync(events.Select(x => x.Message), routingKey);
    }
}
