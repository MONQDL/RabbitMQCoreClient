namespace RabbitMQCoreClient.BatchQueueSender;

/// <summary>
/// Interface of the additional message processing service.
/// </summary>
public interface IEventsHandler
{
    /// <summary>
    /// Executes after events batch was sent to the <see cref="IEventsWriter"/>.
    /// </summary>
    /// <param name="events">List of sent events.</param>
    /// <returns></returns>
    Task OnAfterWriteEvents(IEnumerable<EventItem> events);

    /// <summary>
    /// Executes on batch write process throws error.
    /// </summary>
    /// <param name="events">List of errored events.</param>
    /// <returns></returns>
    Task OnWriteErrors(IEnumerable<EventItem> events);
}
