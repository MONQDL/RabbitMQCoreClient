namespace RabbitMQCoreClient.BatchQueueSender;

/// <summary>
/// Queue bus buffer options.
/// </summary>
public sealed class QueueBatchSenderOptions
{
    /// <summary>
    /// The period for resetting (writing) events in RabbitMQ.
    /// Default: 2 sec.
    /// </summary>
    public int EventsFlushPeriodSec { get; set; } = 1;

    /// <summary>
    /// The number of events upon reaching which to reset (write) to the database.
    /// Default: 500.
    /// </summary>
    public int EventsFlushCount { get; set; } = 500;
}
