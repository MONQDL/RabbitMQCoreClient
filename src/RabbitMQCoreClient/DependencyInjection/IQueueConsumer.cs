using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// The Consumer interface uses for starting and stopping manipulations.
/// </summary>
public interface IQueueConsumer : IAsyncDisposable
{
    /// <summary>
    /// Connect to all queues and start receiving messages.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops listening Queues.
    /// </summary>
    Task ShutdownAsync();

    /// <summary>
    /// The channel that consume messages from the RabbitMQ Instance. 
    /// Can be Null, if <see cref="StartAsync"/> method was not called.
    /// </summary>
    IChannel? ConsumeChannel { get; }

    /// <summary>
    /// The Async consumer, with default consume method configurated.
    /// </summary>
    AsyncEventingBasicConsumer? Consumer { get; }
}
