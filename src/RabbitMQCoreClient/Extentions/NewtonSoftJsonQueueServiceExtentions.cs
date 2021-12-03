using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace RabbitMQCoreClient
{
    public static class NewtonSoftJsonQueueServiceExtentions
    {
        [NotNull]
        public static JsonSerializerSettings DefaultSerializerSettings =>
            new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };

        /// <summary>
        /// Send the message to the queue (thread safe method). <paramref name="obj" /> will be serialized to Json.
        /// </summary>
        /// <typeparam name="T">Тип класса сообщения.</typeparam>
        /// <param name="queueService">The <see cref="IQueueService"/> service object.</param>
        /// <param name="obj">Экземпляр класса <typeparamref name="T" />, который будет сериализирован в JSON и отправлен в очередь.</param>
        /// <param name="routingKey">Ключ маршрутизации, с которыми будет отправлено сообщение.</param>
        /// <param name="exchange">Название точки обмена, в которую требуется послать сообщение.</param>
        /// <param name="decreaseTtl">if set to <c>true</c> [decrease TTL].</param>
        /// <param name="correlationId">Корреляционный Id, который используется для логирования сообщений.</param>
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
            JsonSerializerSettings? jsonSerializerSettings,
            string? exchange = default,
            bool decreaseTtl = true,
            string? correlationId = default
            )
        {
            // Проверка на Null без боксинга. // https://stackoverflow.com/a/864860
            if (EqualityComparer<T>.Default.Equals(obj, default))
                throw new ArgumentNullException(nameof(obj));

            var serializedObj = JsonConvert.SerializeObject(obj, jsonSerializerSettings ?? DefaultSerializerSettings);
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
        /// <typeparam name="T">Тип класса сообщения.</typeparam>
        /// <param name="queueService">The <see cref="IQueueService"/> service object.</param>
        /// <param name="objs">Список объектов, которые являются экземплярами класса <typeparamref name="T" />,
        /// которые будут сериализированы в JSON и отправлены в очередь.</param>
        /// <param name="routingKey">Ключ маршрутизации, с которыми будет отправлено сообщение.</param>
        /// <param name="exchange">Название точки обмена, в которую требуется послать сообщение.</param>
        /// <param name="decreaseTtl">if set to <c>true</c> [decrease TTL].</param>
        /// <param name="correlationId">Корреляционный Id, который используется для логирования сообщений.</param>
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
            JsonSerializerSettings? jsonSerializerSettings,
            string? exchange = default,
            bool decreaseTtl = true,
            string? correlationId = default)
        {
            var messages = new List<string>();
            var serializeSettings = jsonSerializerSettings ?? DefaultSerializerSettings;
            foreach (var obj in objs)
            {
                messages.Add(JsonConvert.SerializeObject(obj, serializeSettings));
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
