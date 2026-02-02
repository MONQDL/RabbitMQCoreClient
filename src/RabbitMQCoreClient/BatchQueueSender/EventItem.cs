namespace RabbitMQCoreClient.BatchQueueSender;

/// <summary>
/// Basic buffer queue message event. 
/// It contains only routingKey and message in bytes for sending to queue.
/// </summary>
public class EventItem
{
    /// <summary>
    /// Basic buffer event constructor.
    /// </summary>
    /// <param name="message">The message in bytes to be send to queue.</param>
    /// <param name="routingKey">The routing key the message to be send to queue.</param>
    public EventItem(ReadOnlyMemory<byte> message, string routingKey)
    {
        Message = message;
        RoutingKey = routingKey;
    }

    /// <summary>
    /// The message in bytes to be send to queue.
    /// </summary>
    public ReadOnlyMemory<byte> Message { get; }

    /// <summary>
    /// The routing key the message to be send to queue.
    /// </summary>
    public string RoutingKey { get; }
}

/// <summary>
/// Basic buffer queue message event with the source object.
/// Use this class for custom <see cref="IEventsWriter"/> logic.
/// </summary>
public class EventItemWithSourceObject : EventItem
{
    /// <summary>
    /// The source object of the event that you want to sent to the queue.
    /// </summary>
    public object Source { get; }

    /// <summary>
    /// Buffer event constructor.
    /// </summary>
    /// <param name="event">Source object.</param>
    /// <param name="message">The message in bytes to be send to queue.</param>
    /// <param name="routingKey">The routing key the message to be send to queue.</param>
    public EventItemWithSourceObject(object @event, ReadOnlyMemory<byte> message, string routingKey)
        : base(message, routingKey)
    {
        Source = @event;
    }
}
