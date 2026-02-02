namespace RabbitMQCoreClient.Models;

/// <summary>
/// Errored messages processing types.
/// </summary>
public enum Routes
{
    /// <summary>
    /// The dead letter queue.
    /// </summary>
    DeadLetter,
    /// <summary>
    /// The source queue.
    /// </summary>
    SourceQueue
}
