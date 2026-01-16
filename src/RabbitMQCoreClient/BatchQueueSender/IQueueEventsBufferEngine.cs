using RabbitMQCoreClient.Serializers;
using System.Diagnostics.CodeAnalysis;

namespace RabbitMQCoreClient.BatchQueueSender;

/// <summary>
/// Batch Queue Events buffer interface.
/// </summary>
public interface IQueueEventsBufferEngine
{
    /// <summary>
    /// Message serializer to be used to serialize objects to sent to queue.
    /// </summary>
    IMessageSerializer Serializer { get; }

    /// <summary>
    /// Add the event to the buffer to be sent to the data bus.
    /// </summary>
    /// <param name="item">The item with user-provided values array.</param>
    void Add([NotNull] EventItem item);
}
