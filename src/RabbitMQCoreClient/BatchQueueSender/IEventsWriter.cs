namespace RabbitMQCoreClient.BatchQueueSender;

/// <summary>
/// The service interface that represents methods to work with queue bus.
/// </summary>
public interface IEventsWriter
{
    /// <summary>
    /// Send events batch to queue.
    /// </summary>
    /// <param name="events">List of events to send to queue.</param>
    /// <param name="routingKey">The routing key.</param>
    /// <returns><see cref="Task"/> when the operation completes.</returns>
    /// <returns></returns>
    Task WriteBatch(IEnumerable<EventItem> events, string routingKey);
}
