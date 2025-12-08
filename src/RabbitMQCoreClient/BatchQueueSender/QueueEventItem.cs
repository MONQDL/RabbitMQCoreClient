namespace RabbitMQCoreClient.BatchQueueSender;

public readonly struct QueueEventItem
{
    /// <summary>
    /// The event to be written to the database.
    /// </summary>
    public object Event { get; }

    /// <summary>
    /// The routing key to which you want to send the event.
    /// </summary>
    public string RoutingKey { get; }

    public QueueEventItem(object @event, string tableName)
    {
        Event = @event;
        RoutingKey = tableName;
    }
}
