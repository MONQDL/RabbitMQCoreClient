namespace RabbitMQCoreClient.Models;

/// <summary>
/// Actions performed with ttl.
/// </summary>
public enum TtlActions
{
    /// <summary>
    /// Reduce ttl messages.
    /// </summary>
    Decrease,
    /// <summary>
    /// Don't change ttl messages.
    /// </summary>
    DoNotChange
}
