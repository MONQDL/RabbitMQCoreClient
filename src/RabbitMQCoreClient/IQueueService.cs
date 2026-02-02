using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQCoreClient.DependencyInjection;
using RabbitMQCoreClient.Events;
using RabbitMQCoreClient.Serializers;

namespace RabbitMQCoreClient;

/// <summary>
/// The interface describes the basic set of methods required to implement a RabbitMQ message queue handler.
/// </summary>
public interface IQueueService : IAsyncDisposable
{
    /// <summary>
    /// RabbitMQ connection interface. Null until first connected.
    /// </summary>
    IConnection? Connection { get; }

    /// <summary>
    /// A channel for sending RabbitMQ data. Null until first connected.
    /// </summary>
    IChannel? PublishChannel { get; }

    /// <summary>
    /// MQ service settings.
    /// </summary>
    RabbitMQCoreClientOptions Options { get; }

    /// <summary>
    /// Message serializer to be used to serialize objects to sent to queue.
    /// </summary>
    IMessageSerializer Serializer { get; }

    /// <summary>
    /// Occurs when connection restored after reconnect.
    /// </summary>
    event AsyncEventHandler<ReconnectEventArgs> ReconnectedAsync;

    /// <summary>
    /// Occurs when connection is interrupted for some reason.
    /// </summary>
    event AsyncEventHandler<ShutdownEventArgs> ConnectionShutdownAsync;

    /// <summary>
    /// Send a raw message to the queue with the specified properties <paramref name="props" />.
    /// </summary>
    /// <param name="obj">An array of bytes to be sent to the queue as the body of the message.</param>
    /// <param name="props">Message properties such as add. headers. Can be created via `Channel.CreateBasicProperties()`.</param>
    /// <param name="routingKey">The routing key with which the message will be sent.</param>
    /// <param name="exchange">The name of the exchange point to which the message is to be sent.</param>
    /// <param name="decreaseTtl">If <c>true</c> then decrease TTL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">jsonString - jsonString
    /// or
    /// exchange - exchange</exception>
    ValueTask SendAsync(
        ReadOnlyMemory<byte> obj,
        BasicProperties props,
        string routingKey,
        string? exchange = default,
        bool decreaseTtl = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a batch raw message to the queue with the specified properties.
    /// </summary>
    /// <param name="objs">List of objects and settings that will be sent to the queue.</param>
    /// <param name="routingKey">The routing key with which the message will be sent.</param>
    /// <param name="exchange">The name of the exchange point to which the message is to be sent.</param>
    /// <param name="decreaseTtl">If <c>true</c> then decrease TTL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">jsonString - jsonString
    /// or
    /// exchange - exchange</exception>
    ValueTask SendBatchAsync(
        IEnumerable<(ReadOnlyMemory<byte> Body, BasicProperties Props)> objs,
        string routingKey,
        string? exchange = default,
        bool decreaseTtl = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a batch raw message to the queue with single properties for all messages.
    /// </summary>
    /// <param name="objs">List of objects and settings that will be sent to the queue.</param>
    /// <param name="props">Message properties such as add. headers. Can be created via `Channel.CreateBasicProperties()`.</param>
    /// <param name="routingKey">The routing key with which the message will be sent.</param>
    /// <param name="exchange">The name of the exchange point to which the message is to be sent.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">jsonString - jsonString
    /// or
    /// exchange - exchange</exception>
    ValueTask SendBatchAsync(
        IEnumerable<ReadOnlyMemory<byte>> objs,
        BasicProperties props,
        string routingKey,
        string? exchange = default,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Connect to RabbitMQ server and run publisher channel.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Shutdown RabbitMQ connection.
    /// </summary>
    /// <returns></returns>
    Task ShutdownAsync();
}
