namespace RabbitMQCoreClient.BatchQueueSender;

/// <summary>
/// The service interface that represents methods to work with queue bus.
/// </summary>
public interface IQueueEventsWriter
{
    /// <summary>
    /// Send events to the queue.
    /// </summary>
    /// <param name="items">The item objects to send to the queue.</param>
    /// <param name="routingKey">The routing key to send.</param>
    /// <returns><see cref="Task"/> when the operation completes.</returns>
    Task Write(object[] items, string routingKey);
}
