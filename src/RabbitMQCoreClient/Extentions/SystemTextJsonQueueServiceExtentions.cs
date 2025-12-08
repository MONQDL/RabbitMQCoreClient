using RabbitMQCoreClient.Serializers;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace RabbitMQCoreClient;

public static class SystemTextJsonQueueServiceExtentions
{
    /// <summary>
    /// Send the message to the queue (thread safe method). <paramref name="obj" /> will be serialized to Json.
    /// </summary>
    /// <typeparam name="T">The class type of the message.</typeparam>
    /// <param name="queueService">The <see cref="IQueueService"/> service object.</param>
    /// <param name="obj">An instance of the <typeparamref name="T" /> class that will be serialized to JSON and sent to the queue.</param>
    /// <param name="routingKey">The routing key with which the message will be sent.</param>
    /// <param name="exchange">The name of the exchange point to which the message is to be sent.</param>
    /// <param name="decreaseTtl">if set to <c>true</c> [decrease TTL].</param>
    /// <param name="jsonSerializerSettings">The json serializer settings.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">jsonString - jsonString
    /// or
    /// exchange - exchange</exception>
    /// <exception cref="ArgumentNullException">obj</exception>
    public static ValueTask SendAsync<T>(
        this IQueueService queueService,
        T obj,
        string routingKey,
        JsonSerializerOptions? jsonSerializerSettings,
        string? exchange = default,
        bool decreaseTtl = true
        )
    {
        // Checking for Null without boxing. // https://stackoverflow.com/a/864860
        if (EqualityComparer<T>.Default.Equals(obj, default))
            throw new ArgumentNullException(nameof(obj));

        var serializedObj = JsonSerializer.SerializeToUtf8Bytes(obj, jsonSerializerSettings ?? SystemTextJsonMessageSerializer.DefaultOptions);
        return queueService.SendJsonAsync(
                    serializedObj,
                    exchange: exchange,
                    routingKey: routingKey,
                    decreaseTtl: decreaseTtl
                   );
    }

    /// <summary>
    /// Send the message to the queue (thread safe method). <paramref name="obj" /> will be serialized to Json with source generator.
    /// </summary>
    /// <typeparam name="T">The class type of the message.</typeparam>
    /// <param name="queueService">The <see cref="IQueueService"/> service object.</param>
    /// <param name="obj">An instance of the <typeparamref name="T" /> class that will be serialized to JSON and sent to the queue.</param>
    /// <param name="routingKey">The routing key with which the message will be sent.</param>
    /// <param name="exchange">The name of the exchange point to which the message is to be sent.</param>
    /// <param name="decreaseTtl">if set to <c>true</c> [decrease TTL].</param>
    /// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">jsonString - jsonString
    /// or
    /// exchange - exchange</exception>
    /// <exception cref="ArgumentNullException">obj</exception>
    public static ValueTask SendAsync<T>(
        this IQueueService queueService,
        T obj,
        string routingKey,
        JsonTypeInfo<T> jsonTypeInfo,
        string? exchange = default,
        bool decreaseTtl = true
        )
    {
        // Checking for Null without boxing. // https://stackoverflow.com/a/864860
        if (EqualityComparer<T>.Default.Equals(obj, default))
            throw new ArgumentNullException(nameof(obj));

        var serializedObj = JsonSerializer.SerializeToUtf8Bytes(obj, jsonTypeInfo);
        return queueService.SendJsonAsync(
                    serializedObj,
                    exchange: exchange,
                    routingKey: routingKey,
                    decreaseTtl: decreaseTtl
                   );
    }

    /// <summary>
    /// Send pack of messages to the queue (thread safe method). <paramref name="objs" /> will be serialized to Json.
    /// </summary>
    /// <typeparam name="T">The class type of the message.</typeparam>
    /// <param name="queueService">The <see cref="IQueueService"/> service object.</param>
    /// <param name="objs">A list of objects that are instances of the class <typeparamref name="T" /> 
    /// that will be serialized to JSON and sent to the queue.</param>
    /// <param name="routingKey">The routing key with which the message will be sent.</param>
    /// <param name="exchange">The name of the exchange point to which the message is to be sent.</param>
    /// <param name="decreaseTtl">If set to <c>true</c> [decrease TTL].</param>
    /// <param name="jsonSerializerSettings">The json serializer settings.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">jsonString - jsonString
    /// or
    /// exchange - exchange</exception>
    /// <exception cref="ArgumentNullException">obj</exception>
    public static ValueTask SendBatchAsync<T>(
        this IQueueService queueService,
        IEnumerable<T> objs,
        string routingKey,
        JsonSerializerOptions? jsonSerializerSettings,
        string? exchange = default,
        bool decreaseTtl = true)
    {
        var messages = new List<ReadOnlyMemory<byte>>();
        var serializeSettings = jsonSerializerSettings ?? SystemTextJsonMessageSerializer.DefaultOptions;
        foreach (var obj in objs)
        {
            messages.Add(JsonSerializer.SerializeToUtf8Bytes(obj, serializeSettings));
        }

        return queueService.SendJsonBatchAsync(
            serializedJsonList: messages,
            exchange: exchange,
            routingKey: routingKey,
            decreaseTtl: decreaseTtl
        );
    }

    /// <summary>
    /// Send pack of messages to the queue (thread safe method). <paramref name="objs" /> will be serialized to Json with source generator.
    /// </summary>
    /// <typeparam name="T">The class type of the message.</typeparam>
    /// <param name="queueService">The <see cref="IQueueService"/> service object.</param>
    /// <param name="objs">A list of objects that are instances of the class <typeparamref name="T" /> 
    /// that will be serialized to JSON and sent to the queue.</param>
    /// <param name="routingKey">The routing key with which the message will be sent.</param>
    /// <param name="exchange">The name of the exchange point to which the message is to be sent.</param>
    /// <param name="decreaseTtl">If set to <c>true</c> [decrease TTL].</param>
    /// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">jsonString - jsonString
    /// or
    /// exchange - exchange</exception>
    /// <exception cref="ArgumentNullException">obj</exception>
    public static ValueTask SendBatchAsync<T>(
        this IQueueService queueService,
        IEnumerable<T> objs,
        string routingKey,
        JsonTypeInfo<T> jsonTypeInfo,
        string? exchange = default,
        bool decreaseTtl = true)
    {
        var messages = new List<ReadOnlyMemory<byte>>();
        foreach (var obj in objs)
        {
            messages.Add(JsonSerializer.SerializeToUtf8Bytes(obj, jsonTypeInfo));
        }

        return queueService.SendJsonBatchAsync(
            serializedJsonList: messages,
            exchange: exchange,
            routingKey: routingKey,
            decreaseTtl: decreaseTtl
        );
    }
}
