using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQCoreClient.Configuration.DependencyInjection.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RabbitMQCoreClient
{
    /// <summary>
    /// Интерфейс описывает базовый набор методов, требующихся для реализации обработчика очереди сообщений RabbitMQ.
    /// </summary>
    public interface IQueueService : IDisposable
    {
        /// <summary>
        /// Интерфейс соединения RabbitMQ.
        /// </summary>
        IConnection Connection { get; }

        /// <summary>
        /// Канал для отправки данных RabbitMQ.
        /// </summary>
        IModel SendChannel { get; }

        /// <summary>
        /// Настройки сервиса MQ.
        /// </summary>
        RabbitMQCoreClientOptions Options { get; }

        /// <summary>
        /// Occurs when connection restored after reconnect.
        /// </summary>
        event Action OnReconnected;

        /// <summary>
        /// Occurs when connection is shuted down on any reason.
        /// </summary>
        event Action OnConnectionShutdown;

        /// <summary>
        /// Отправить сообщение в очередь (потокобезопасный метод). <paramref name="obj" /> при это будет сериализован в Json.
        /// </summary>
        /// <typeparam name="T">Тип класса сообщения.</typeparam>
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
        ValueTask SendAsync<T>(
            T obj,
            string routingKey,
            string exchange = default,
            bool decreaseTtl = true,
            string correlationId = default,
            JsonSerializerSettings jsonSerializerSettings = default);

        /// <summary>
        /// Отправить сообщение в очередь (потокобезопасный метод).
        /// </summary>
        /// <param name="json">The json.</param>
        /// <param name="routingKey">Ключ маршрутизации, с которыми будет отправлено сообщение.</param>
        /// <param name="exchange">Название точки обмена, в которую требуется послать сообщение.</param>
        /// <param name="decreaseTtl">Если <c>true</c> то уменьшить TTL.</param>
        /// <param name="correlationId">Корреляционный Id, который используется для логирования сообщений.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">jsonString - jsonString
        /// or
        /// exchange - exchange</exception>
        ValueTask SendJsonAsync(
            string json,
            string routingKey,
            string exchange = default,
            bool decreaseTtl = true,
            string correlationId = default);

        /// <summary>
        /// Отправить сырое сообщение в очередь с указанными свойствами <paramref name="props" /> (потокобезопасный метод).
        /// </summary>
        /// <param name="obj">Массив байт, которое будет отправлено в очередь как тело сообщения.</param>
        /// <param name="props">Свойства сообщения, такие как доп. заголовки. Можно создать через Channel.CreateBasicProperties();</param>
        /// <param name="routingKey">Ключ маршрутизации, с которым будет отправлено сообщение.</param>
        /// <param name="exchange">Название точки обмена, в которую требуется послать сообщение.</param>
        /// <param name="decreaseTtl">Если <c>true</c> то уменьшить TTL.</param>
        /// <param name="correlationId">Корреляционный Id, который используется для логирования сообщений.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">jsonString - jsonString
        /// or
        /// exchange - exchange</exception>
        ValueTask SendAsync(
            byte[] obj,
            IBasicProperties props,
            string routingKey,
            string exchange,
            bool decreaseTtl = true,
            string correlationId = default);

        /// <summary>
        /// Отправить пакетно сообщения в очередь (потокобезопасный метод). <paramref name="objs" /> при этом будут сериализованы в Json.
        /// </summary>
        /// <typeparam name="T">Тип класса сообщения.</typeparam>
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
        ValueTask SendBatchAsync<T>(
            IEnumerable<T> objs,
            string routingKey,
            string exchange = default,
            bool decreaseTtl = true,
            string correlationId = default,
            JsonSerializerSettings jsonSerializerSettings = default);

        /// <summary>
        /// Отправить пакетно сообщения в очередь (потокобезопасный метод).
        /// </summary>
        /// <param name="serializedJsonList">Список сериализованных json, которые требуется отправить в очередь пакетно.</param>
        /// <param name="routingKey">Ключ маршрутизации, с которыми будет отправлено сообщение.</param>
        /// <param name="exchange">Название точки обмена, в которую требуется послать сообщение.</param>
        /// <param name="decreaseTtl">Если <c>true</c> то уменьшить TTL.</param>
        /// <param name="correlationId">Корреляционный Id, который используется для логирования сообщений.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">jsonString - jsonString
        /// or
        /// exchange - exchange</exception>
        ValueTask SendJsonBatchAsync(
            IEnumerable<string> serializedJsonList,
            string routingKey,
            string exchange = default,
            bool decreaseTtl = true,
            string correlationId = default);

        /// <summary>
        /// Отправить пакетно сырые сообщение в очередь с указанными свойствами (потокобезопасный метод).
        /// </summary>
        /// <param name="objs">Список объектов и настроек, которые будет отправлены в очередь.</param>
        /// <param name="routingKey">Ключ маршрутизации, с которым будет отправлено сообщение.</param>
        /// <param name="exchange">Название точки обмена, в которую требуется послать сообщение.</param>
        /// <param name="decreaseTtl">Если <c>true</c> то уменьшить TTL.</param>
        /// <param name="correlationId">Корреляционный Id, который используется для логирования сообщений.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">jsonString - jsonString
        /// or
        /// exchange - exchange</exception>
        ValueTask SendBatchAsync(
            IEnumerable<(byte[] Body, IBasicProperties Props)> objs,
            string routingKey,
            string exchange,
            bool decreaseTtl = true,
            string correlationId = default);
    }
}