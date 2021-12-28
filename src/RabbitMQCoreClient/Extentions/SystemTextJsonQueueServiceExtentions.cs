using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Tasks;

namespace RabbitMQCoreClient
{
    public static class SystemTextJsonQueueServiceExtentions
    {
        [NotNull]
        public static JsonSerializerOptions DefaultSerializerSettings =>
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
            };

        /// <summary>
        /// Send the message to the queue (thread safe method). <paramref name="obj" /> will be serialized to Json.
        /// </summary>
        /// <typeparam name="T">The class type of the message.</typeparam>
        /// <param name="queueService">The <see cref="IQueueService"/> service object.</param>
        /// <param name="obj">An instance of the <typeparamref name="T" /> class that will be serialized to JSON and sent to the queue.</param>
        /// <param name="routingKey">The routing key with which the message will be sent.</param>
        /// <param name="exchange">The name of the exchange point to which the message is to be sent.</param>
        /// <param name="decreaseTtl">if set to <c>true</c> [decrease TTL].</param>
        /// <param name="correlationId">Correlation Id, which is used to log messages.</param>
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
            bool decreaseTtl = true,
            string? correlationId = default
            )
        {
            // Checking for Null without boxing. // https://stackoverflow.com/a/864860
            if (EqualityComparer<T>.Default.Equals(obj, default))
                throw new ArgumentNullException(nameof(obj));

            var serializedObj = JsonSerializer.Serialize(obj, jsonSerializerSettings ?? DefaultSerializerSettings);
            return queueService.SendJsonAsync(
                        serializedObj,
                        exchange: exchange,
                        routingKey: routingKey,
                        decreaseTtl: decreaseTtl,
                        correlationId: correlationId
                       );
        }

        /// <summary>
        /// Send messages pack to the queue (thred safe method). <paramref name="objs" /> will be serialized to Json.
        /// </summary>
        /// <typeparam name="T">The class type of the message.</typeparam>
        /// <param name="queueService">The <see cref="IQueueService"/> service object.</param>
        /// <param name="objs">A list of objects that are instances of the class <typeparamref name="T" /> 
        /// that will be serialized to JSON and sent to the queue.</param>
        /// <param name="routingKey">The routing key with which the message will be sent.</param>
        /// <param name="exchange">The name of the exchange point to which the message is to be sent.</param>
        /// <param name="decreaseTtl">If set to <c>true</c> [decrease TTL].</param>
        /// <param name="correlationId">Correlation Id, which is used to log messages.</param>
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
            bool decreaseTtl = true,
            string? correlationId = default)
        {
            var messages = new List<string>();
            var serializeSettings = jsonSerializerSettings ?? DefaultSerializerSettings;
            foreach (var obj in objs)
            {
                messages.Add(JsonSerializer.Serialize(obj, serializeSettings));
            }

            return queueService.SendJsonBatchAsync(
                serializedJsonList: messages,
                exchange: exchange,
                routingKey: routingKey,
                decreaseTtl: decreaseTtl,
                correlationId: correlationId
            );
        }
    }
}
