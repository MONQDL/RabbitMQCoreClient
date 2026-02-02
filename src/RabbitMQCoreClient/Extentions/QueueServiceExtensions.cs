using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace RabbitMQCoreClient;

/// <summary>
/// Extended <see cref="IQueueService"/> publish methods.
/// </summary>
public static class QueueServiceExtensions
{
    /// <summary>
    /// Send the message to the queue. <paramref name="obj" /> will be serialized with default serializer.
    /// </summary>
    /// <typeparam name="T">The class type of the message.</typeparam>
    /// <param name="service">The <see cref="IQueueService"/> object.</param>
    /// <param name="obj">An instance of the <typeparamref name="T" /> class that will be serialized with default serializer and sent to the queue.</param>
    /// <param name="routingKey">The routing key with which the message will be sent.</param>
    /// <param name="exchange">The name of the exchange point to which the message is to be sent.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">obj</exception>
    [RequiresUnreferencedCode("Serialization might require types that cannot be statically analyzed.")]
    public static ValueTask SendAsync<T>(
        this IQueueService service,
        T obj,
        string routingKey,
        string? exchange = default,
        CancellationToken cancellationToken = default
        )
        where T : class
    {
        if (obj is null)
            throw new ArgumentNullException(nameof(obj));

        var serializedObj = service.Serializer.Serialize(obj);
        return service.SendAsync(
                    serializedObj,
                    props: QueueService.CreateDefaultProperties(),
                    exchange: exchange,
                    routingKey: routingKey,
                    cancellationToken: cancellationToken
                   );
    }

    /// <summary>
    /// Send a raw message to the queue with the default properties.
    /// </summary>
    /// <param name="service">The <see cref="IQueueService"/> object.</param>
    /// <param name="obj">An array of bytes to be sent to the queue as the body of the message.</param>
    /// <param name="routingKey">The routing key with which the message will be sent.</param>
    /// <param name="exchange">The name of the exchange point to which the message is to be sent.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">obj - obj is null</exception>
    public static ValueTask SendAsync(
        this IQueueService service,
        ReadOnlyMemory<byte> obj,
        string routingKey,
        string? exchange = default,
        CancellationToken cancellationToken = default) =>
        service.SendAsync(obj,
            props: QueueService.CreateDefaultProperties(),
            routingKey: routingKey,
            exchange: exchange,
            decreaseTtl: false,
            cancellationToken: cancellationToken
            );

    /// <summary>
    /// Send a bytes array message to the queue with the default properties.
    /// </summary>
    /// <param name="service">The <see cref="IQueueService"/> object.</param>
    /// <param name="obj">An array of bytes to be sent to the queue as the body of the message.</param>
    /// <param name="routingKey">The routing key with which the message will be sent.</param>
    /// <param name="exchange">The name of the exchange point to which the message is to be sent.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">obj - obj is null</exception>
    public static ValueTask SendAsync(
        this IQueueService service,
        byte[] obj,
        string routingKey,
        string? exchange = default,
        CancellationToken cancellationToken = default) =>
        service.SendAsync(new ReadOnlyMemory<byte>(obj),
            props: QueueService.CreateDefaultProperties(),
            routingKey: routingKey,
            exchange: exchange,
            decreaseTtl: false,
            cancellationToken: cancellationToken
            );

    /// <summary>
    /// Send a string message to the queue with the default properties.
    /// </summary>
    /// <param name="service">The <see cref="IQueueService"/> object.</param>
    /// <param name="obj">A string to be sent to the queue as the body of the message.</param>
    /// <param name="routingKey">The routing key with which the message will be sent.</param>
    /// <param name="exchange">The name of the exchange point to which the message is to be sent.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">obj - obj is null</exception>
    public static ValueTask SendAsync(
        this IQueueService service,
        string obj,
        string routingKey,
        string? exchange = default,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(obj))
            throw new ArgumentException($"{nameof(obj)} is null or empty.", nameof(obj));

        var body = Encoding.UTF8.GetBytes(obj).AsMemory();

        return service.SendAsync(body,
            props: QueueService.CreateDefaultProperties(),
            routingKey: routingKey,
            exchange: exchange,
            decreaseTtl: false,
            cancellationToken: cancellationToken
            );
    }

    /// <summary>
    /// Send messages pack to the queue. <paramref name="objs" /> will be serialized with default serializer.
    /// </summary>
    /// <typeparam name="T">The class type of the message.</typeparam>
    /// <param name="service">The <see cref="IQueueService"/> object.</param>
    /// <param name="objs">A list of objects that are instances of the class <typeparamref name="T" /> 
    /// that will be serialized with default serializer and sent to the queue.</param>
    /// <param name="routingKey">The routing key with which the message will be sent.</param>
    /// <param name="exchange">The name of the exchange point to which the message is to be sent.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">obj</exception>
    [RequiresUnreferencedCode("Serialization might require types that cannot be statically analyzed.")]
    public static ValueTask SendBatchAsync<T>(
        this IQueueService service,
        IEnumerable<T> objs,
        string routingKey,
        string? exchange = default,
        CancellationToken cancellationToken = default
        ) where T : class =>
            service.SendBatchAsync(
                objs: objs.Select(x => service.Serializer.Serialize(x)),
                QueueService.CreateDefaultProperties(),
                routingKey: routingKey,
                exchange: exchange,
                cancellationToken: cancellationToken
            );

    /// <summary>
    /// Send a batch of bytes array messages to the queue with default properties.
    /// </summary>
    /// <param name="service">The <see cref="IQueueService"/> object.</param>
    /// <param name="objs">List of objects to be sent to the queue as the body of the message.</param>
    /// <param name="routingKey">The routing key with which the message will be sent.</param>
    /// <param name="exchange">The name of the exchange point to which the message is to be sent.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    public static ValueTask SendBatchAsync(
        this IQueueService service,
        IEnumerable<byte[]> objs,
        string routingKey,
        string? exchange = default,
        CancellationToken cancellationToken = default) =>
            service.SendBatchAsync(
                objs: objs.Select(x => new ReadOnlyMemory<byte>(x)),
                QueueService.CreateDefaultProperties(),
                routingKey: routingKey,
                exchange: exchange,
                cancellationToken: cancellationToken
            );

    /// <summary>
    /// Send a batch of string messages to the queue with default properties.
    /// </summary>
    /// <param name="service">The <see cref="IQueueService"/> object.</param>
    /// <param name="objs">List of objects to be sent to the queue as the body of the message.</param>
    /// <param name="routingKey">The routing key with which the message will be sent.</param>
    /// <param name="exchange">The name of the exchange point to which the message is to be sent.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    public static ValueTask SendBatchAsync(
        this IQueueService service,
        IEnumerable<string> objs,
        string routingKey,
        string? exchange = default,
        CancellationToken cancellationToken = default) => 
            service.SendBatchAsync(
                objs: objs.Select(x => new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(x))),
                QueueService.CreateDefaultProperties(),
                routingKey: routingKey,
                exchange: exchange,
                cancellationToken: cancellationToken
            );

    /// <summary>
    /// Send a batch of string messages to the queue with default properties.
    /// </summary>
    /// <param name="service">The <see cref="IQueueService"/> object.</param>
    /// <param name="objs">List of objects to be sent to the queue as the body of the message.</param>
    /// <param name="routingKey">The routing key with which the message will be sent.</param>
    /// <param name="exchange">The name of the exchange point to which the message is to be sent.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    public static ValueTask SendBatchAsync(
        this IQueueService service,
        IEnumerable<ReadOnlyMemory<byte>> objs,
        string routingKey,
        string? exchange = default,
        CancellationToken cancellationToken = default) => 
            service.SendBatchAsync(
                objs: objs,
                QueueService.CreateDefaultProperties(),
                routingKey: routingKey,
                exchange: exchange,
                cancellationToken: cancellationToken
            );
}
