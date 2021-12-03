using RabbitMQ.Client.Events;
using System;

namespace RabbitMQCoreClient.Configuration.DependencyInjection.Options
{
    public class RabbitMQCoreClientOptions
    {
        /// <summary>
        /// Адрес сервера RabbitMQ.
        /// </summary>
        public string HostName { get; set; } = "127.0.0.1";

        /// <summary>
        /// Пароль пользователя, у которого есть права на подключение к серверу <see cref="HostName"/>.
        /// </summary>
        public string Password { get; set; } = "guest";

        /// <summary>
        /// Порт доступа к сервису.
        /// </summary>
        public int Port { get; set; } = 5672;

        /// <summary>
        /// Timeout setting for connection attempts (in milliseconds).
        /// Default: 30000.
        /// </summary>
        public int RequestedConnectionTimeout { get; set; } = 30000;

        public ushort RequestedHeartbeat { get; set; } = 60;

        /// <summary>
        /// Timeout setting between reconnection attempts (in milliseconds).
        /// Default: 3000.
        /// </summary>
        public int ReconnectionTimeout { get; set; } = 3000;

        /// <summary>
        /// Reconnection attempts count.
        /// Default: 20. Means after 60 sec it will rise ConnectionException.
        /// </summary>
        public int ReconnectionAttemptsCount { get; set; } = 20;

        /// <summary>
        /// Пользователь, у которого есть права на подключение к серверу <see cref="HostName"/>.
        /// </summary>
        public string UserName { get; set; } = "guest";

        /// <summary>
        /// Gets or sets the virtual host.
        /// </summary>
        public string VirtualHost { get; set; } = "/";

        /// <summary>
        /// Количество попыток обработки сообщения, при обработке которого было вызвано исключение.
        /// </summary>
        public int DefaultTtl { get; set; } = 5;

        /// <summary>
        /// К-во сообщений, которые будут предварительно загружены в обработчик.
        /// </summary>
        public ushort PrefetchCount { get; set; } = 1;

        /// <summary>
        /// Событие обработчика исключения внутренних вызовов библиотеки.
        /// </summary>
        public EventHandler<CallbackExceptionEventArgs>? ConnectionCallbackExceptionHandler { get; set; }
    }
}
