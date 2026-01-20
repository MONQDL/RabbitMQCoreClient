using RabbitMQCoreClient.BatchQueueSender;
using RabbitMQCoreClient.Serializers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace RabbitMQCoreClient;

/// <summary>
/// Extended System.Text.Json publish methods.
/// </summary>
public static class SystemTextJsonQueueServiceExtensions
{
    /// <summary>
    /// Send the message to the queue with serialization to Json.
    /// </summary>
    /// <typeparam name="T">The class type of the message.</typeparam>
    /// <param name="service">The <see cref="IQueueService"/> service object.</param>
    /// <param name="obj">An instance of the <typeparamref name="T" /> class that will be serialized to JSON and sent to the queue.</param>
    /// <param name="routingKey">The routing key with which the message will be sent.</param>
    /// <param name="exchange">The name of the exchange point to which the message is to be sent.</param>
    /// <param name="jsonSerializerSettings">The json serializer settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">obj</exception>
    [RequiresUnreferencedCode("Method uses System.Text.Json.JsonSerializer.SerializeToUtf8Bytes witch is incompatible with trimming.")]
    public static ValueTask SendAsync<T>(
        this IQueueService service,
        T obj,
        string routingKey,
        JsonSerializerOptions? jsonSerializerSettings,
        string? exchange = default,
        CancellationToken cancellationToken = default
        ) where T : class
    {
        if (obj is null)
            throw new ArgumentNullException(nameof(obj));

        var serializedObj = JsonSerializer.SerializeToUtf8Bytes(obj, jsonSerializerSettings ?? SystemTextJsonMessageSerializer.DefaultOptions);
        return QueueServiceExtensions.SendAsync(service,
                    serializedObj,
                    routingKey: routingKey,
                    exchange: exchange,
                    cancellationToken: cancellationToken
                   );
    }

    /// <summary>
    /// Send the message to the queue with serialization to Json by source generator.
    /// </summary>
    /// <typeparam name="T">The class type of the message.</typeparam>
    /// <param name="service">The <see cref="IQueueService"/> service object.</param>
    /// <param name="obj">An instance of the <typeparamref name="T" /> class that will be serialized to JSON and sent to the queue.</param>
    /// <param name="routingKey">The routing key with which the message will be sent.</param>
    /// <param name="exchange">The name of the exchange point to which the message is to be sent.</param>
    /// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">obj</exception>
    public static ValueTask SendAsync<T>(
        this IQueueService service,
        T obj,
        string routingKey,
        JsonTypeInfo<T> jsonTypeInfo,
        string? exchange = default,
        CancellationToken cancellationToken = default
        ) where T : class
    {
        if (obj is null)
            throw new ArgumentNullException(nameof(obj));

        var serializedObj = JsonSerializer.SerializeToUtf8Bytes(obj, jsonTypeInfo);
        return QueueServiceExtensions.SendAsync(service,
                    serializedObj,
                    routingKey: routingKey,
                    exchange: exchange,
                    cancellationToken: cancellationToken
                   );
    }

    /// <summary>
    /// Send pack of messages to the queue with serialization to Json.
    /// </summary>
    /// <typeparam name="T">The class type of the message.</typeparam>
    /// <param name="service">The <see cref="IQueueService"/> service object.</param>
    /// <param name="objs">A list of objects that are instances of the class <typeparamref name="T" /> 
    /// that will be serialized to JSON and sent to the queue.</param>
    /// <param name="routingKey">The routing key with which the message will be sent.</param>
    /// <param name="exchange">The name of the exchange point to which the message is to be sent.</param>
    /// <param name="jsonSerializerSettings">The json serializer settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">obj</exception>
    [RequiresUnreferencedCode("Method uses System.Text.Json.JsonSerializer.SerializeToUtf8Bytes witch is incompatible with trimming.")]
    public static ValueTask SendBatchAsync<T>(
        this IQueueService service,
        IEnumerable<T> objs,
        string routingKey,
        JsonSerializerOptions? jsonSerializerSettings,
        string? exchange = default,
        CancellationToken cancellationToken = default
        ) where T : class
    {
        var serializeSettings = jsonSerializerSettings ?? SystemTextJsonMessageSerializer.DefaultOptions;
        var messages = objs.Select(x => new ReadOnlyMemory<byte>(
                JsonSerializer.SerializeToUtf8Bytes(x, serializeSettings)));

        return service.SendBatchAsync(
            objs: messages,
            routingKey: routingKey,
            exchange: exchange,
            cancellationToken: cancellationToken
        );
    }

    /// <summary>
    /// Send pack of messages to the queue with serialization to Json by source generator.
    /// </summary>
    /// <typeparam name="T">The class type of the message.</typeparam>
    /// <param name="service">The <see cref="IQueueService"/> service object.</param>
    /// <param name="objs">A list of objects that are instances of the class <typeparamref name="T" /> 
    /// that will be serialized to JSON and sent to the queue.</param>
    /// <param name="routingKey">The routing key with which the message will be sent.</param>
    /// <param name="exchange">The name of the exchange point to which the message is to be sent.</param>
    /// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">obj</exception>
    public static ValueTask SendBatchAsync<T>(
        this IQueueService service,
        IEnumerable<T> objs,
        string routingKey,
        JsonTypeInfo<T> jsonTypeInfo,
        string? exchange = default,
        CancellationToken cancellationToken = default
        ) where T : class
    {
        var messages = objs.Select(x => new ReadOnlyMemory<byte>(
                 JsonSerializer.SerializeToUtf8Bytes(x, jsonTypeInfo)));

        return service.SendBatchAsync(
            objs: messages,
            exchange: exchange,
            routingKey: routingKey,
            cancellationToken: cancellationToken
        );
    }

    /// <summary>
    /// Add an object to be send as event to the data bus.
    /// </summary>
    /// <param name="service">The <see cref="IQueueBufferService"/> object.</param>
    /// <param name="obj">The object to send to the data bus.</param>
    /// <param name="routingKey">The name of the route key with which you want to send events to the data bus.</param>
    /// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
    public static void AddEvent<T>(this IQueueBufferService service, 
        [NotNull] T obj, 
        string routingKey,
        JsonTypeInfo<T> jsonTypeInfo)
        where T : class =>
        service.Add(new EventItem(JsonSerializer.SerializeToUtf8Bytes(obj, jsonTypeInfo), routingKey));

    /// <summary>
    /// Add objects collection to send as events to the data bus.
    /// </summary>
    /// <typeparam name="T">The type of list item of the <paramref name="objs"/> property.</typeparam>
    /// <param name="service">The <see cref="IQueueBufferService"/> object.</param>
    /// <param name="objs">The list of objects to send to the data bus.</param>
    /// <param name="routingKey">The name of the route key with which you want to send events to the data bus.</param>
    /// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
    /// <returns></returns>
    public static void AddEvents<T>(this IQueueBufferService service, 
        IEnumerable<T> objs, 
        string routingKey,
        JsonTypeInfo<T> jsonTypeInfo)
        where T : class
    {
        foreach (var obj in objs)
            service.Add(new EventItem(JsonSerializer.SerializeToUtf8Bytes(obj, jsonTypeInfo), routingKey));
    }
}
