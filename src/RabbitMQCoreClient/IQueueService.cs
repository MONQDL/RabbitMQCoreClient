using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQCoreClient.Configuration.DependencyInjection.Options;
using RabbitMQCoreClient.Events;

namespace RabbitMQCoreClient;

/// <summary>
/// The interface describes the basic set of methods required to implement a RabbitMQ message queue handler.
/// </summary>
public interface IQueueService : IAsyncDisposable
{
    /// <summary>
    /// RabbitMQ connection interface.
    /// </summary>
    IConnection Connection { get; }

    /// <summary>
    /// A channel for sending RabbitMQ data.
    /// </summary>
    IChannel SendChannel { get; }

    /// <summary>
    /// MQ service settings.
    /// </summary>
    RabbitMQCoreClientOptions Options { get; }

    /// <summary>
    /// Occurs when connection restored after reconnect.
    /// </summary>
    event AsyncEventHandler<ReconnectEventArgs> ReconnectedAsync;

    /// <summary>
    /// Occurs when connection is interrupted for some reason.
    /// </summary>
    event AsyncEventHandler<ShutdownEventArgs> ConnectionShutdownAsync;

    /// <summary>
    /// Send a message to the queue (thread safe method).
    /// </summary>
    /// <param name="json">The json.</param>
    /// <param name="routingKey">The routing key with which the message will be sent.</param>
    /// <param name="exchange">The name of the exchange point to which the message is to be sent.</param>
    /// <param name="decreaseTtl">If <c>true</c> then decrease TTL.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">jsonString - jsonString
    /// or
    /// exchange - exchange</exception>
    ValueTask SendJsonAsync(
        string json,
        string routingKey,
        string? exchange = default,
        bool decreaseTtl = true);

    /// <summary>
    /// Send a message to the queue (thread safe method).
    /// </summary>
    /// <param name="jsonBytes">The json converted to UTF-8 bytes array.</param>
    /// <param name="routingKey">The routing key with which the message will be sent.</param>
    /// <param name="exchange">The name of the exchange point to which the message is to be sent.</param>
    /// <param name="decreaseTtl">If <c>true</c> then decrease TTL.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">jsonString - jsonString
    /// or
    /// exchange - exchange</exception>
    ValueTask SendJsonAsync(
        ReadOnlyMemory<byte> jsonBytes,
        string routingKey,
        string? exchange = default,
        bool decreaseTtl = true);

    /// <summary>
    /// Send the message to the queue (thread safe method). <paramref name="obj" /> will be serialized to Json.
    /// </summary>
    /// <typeparam name="T">The class type of the message.</typeparam>
    /// <param name="obj">An instance of the <typeparamref name="T" /> class that will be serialized to JSON and sent to the queue.</param>
    /// <param name="routingKey">The routing key with which the message will be sent.</param>
    /// <param name="exchange">The name of the exchange point to which the message is to be sent.</param>
    /// <param name="decreaseTtl">If set to <c>true</c> [decrease TTL].</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">jsonString - jsonString
    /// or
    /// exchange - exchange</exception>
    /// <exception cref="ArgumentNullException">obj</exception>
    ValueTask SendAsync<T>(
        T obj,
        string routingKey,
        string? exchange = default,
        bool decreaseTtl = true
        );

    /// <summary>
    /// Send a raw message to the queue with the specified properties <paramref name="props" /> (thread safe).
    /// </summary>
    /// <param name="obj">An array of bytes to be sent to the queue as the body of the message.</param>
    /// <param name="props">Message properties such as add. headers. Can be created via `Channel.CreateBasicProperties()`.</param>
    /// <param name="routingKey">The routing key with which the message will be sent.</param>
    /// <param name="exchange">The name of the exchange point to which the message is to be sent.</param>
    /// <param name="decreaseTtl">If <c>true</c> then decrease TTL.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">jsonString - jsonString
    /// or
    /// exchange - exchange</exception>
    ValueTask SendAsync(
        byte[] obj,
        BasicProperties props,
        string routingKey,
        string? exchange = default,
        bool decreaseTtl = true);

    /// <summary>
    /// Send a raw message to the queue with the specified properties <paramref name="props" /> (thread safe).
    /// </summary>
    /// <param name="obj">An array of bytes to be sent to the queue as the body of the message.</param>
    /// <param name="props">Message properties such as add. headers. Can be created via `Channel.CreateBasicProperties()`.</param>
    /// <param name="routingKey">The routing key with which the message will be sent.</param>
    /// <param name="exchange">The name of the exchange point to which the message is to be sent.</param>
    /// <param name="decreaseTtl">If <c>true</c> then decrease TTL.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">jsonString - jsonString
    /// or
    /// exchange - exchange</exception>
    ValueTask SendAsync(
        ReadOnlyMemory<byte> obj,
        BasicProperties props,
        string routingKey,
        string? exchange = default,
        bool decreaseTtl = true);

    /// <summary>
    /// Send messages pack to the queue (thread safe method). <paramref name="objs" /> will be serialized to Json.
    /// </summary>
    /// <typeparam name="T">The class type of the message.</typeparam>
    /// <param name="objs">A list of objects that are instances of the class <typeparamref name="T" /> 
    /// that will be serialized to JSON and sent to the queue.</param>
    /// <param name="routingKey">The routing key with which the message will be sent.</param>
    /// <param name="exchange">The name of the exchange point to which the message is to be sent.</param>
    /// <param name="decreaseTtl">if set to <c>true</c> [decrease TTL].</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">jsonString - jsonString
    /// or
    /// exchange - exchange</exception>
    /// <exception cref="ArgumentNullException">obj</exception>
    ValueTask SendBatchAsync<T>(
        IEnumerable<T> objs,
        string routingKey,
        string? exchange = default,
        bool decreaseTtl = true);

    /// <summary>
    /// Send a batch raw message to the queue with the specified properties (thread safe).
    /// </summary>
    /// <param name="objs">List of objects and settings that will be sent to the queue.</param>
    /// <param name="routingKey">The routing key with which the message will be sent.</param>
    /// <param name="exchange">The name of the exchange point to which the message is to be sent.</param>
    /// <param name="decreaseTtl">If <c>true</c> then decrease TTL.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">jsonString - jsonString
    /// or
    /// exchange - exchange</exception>
    ValueTask SendBatchAsync(
        IEnumerable<(ReadOnlyMemory<byte> Body, BasicProperties Props)> objs,
        string routingKey,
        string? exchange = default,
        bool decreaseTtl = true);

    /// <summary>
    /// Send batch messages to the queue (thread safe method).
    /// </summary>
    /// <param name="serializedJsonList">A list of serialized json to be sent to the queue in batch.</param>
    /// <param name="routingKey">The routing key with which the message will be sent.</param>
    /// <param name="exchange">The name of the exchange point to which the message is to be sent.</param>
    /// <param name="decreaseTtl">If <c>true</c> then decrease TTL.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">jsonString - jsonString
    /// or
    /// exchange - exchange</exception>
    ValueTask SendJsonBatchAsync(
        IEnumerable<string> serializedJsonList,
        string routingKey,
        string? exchange = default,
        bool decreaseTtl = true);

    /// <summary>
    /// Send batch messages to the queue (thread safe method).
    /// </summary>
    /// <param name="serializedJsonList">A list of serialized json to be sent to the queue in batch.</param>
    /// <param name="routingKey">The routing key with which the message will be sent.</param>
    /// <param name="exchange">The name of the exchange point to which the message is to be sent.</param>
    /// <param name="decreaseTtl">If <c>true</c> then decrease TTL.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">jsonString - jsonString
    /// or
    /// exchange - exchange</exception>
    ValueTask SendJsonBatchAsync(
        IEnumerable<ReadOnlyMemory<byte>> serializedJsonList,
        string routingKey,
        string? exchange = default,
        bool decreaseTtl = true);
}
