using RabbitMQ.Client.Events;
using System;

namespace RabbitMQCoreClient.Configuration.DependencyInjection.Options
{
    public class RabbitMQCoreClientOptions
    {
        /// <summary>
        /// RabbitMQ server address.
        /// </summary>
        public string HostName { get; set; } = "127.0.0.1";

        /// <summary>
        /// Password of the user who has rights to connect to the server <see cref="HostName"/>.
        /// </summary>
        public string Password { get; set; } = "guest";

        /// <summary>
        /// Service access port.
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
        /// User who has rights to connect to the server <see cref="HostName"/>.
        /// </summary>
        public string UserName { get; set; } = "guest";

        /// <summary>
        /// Gets or sets the virtual host.
        /// </summary>
        public string VirtualHost { get; set; } = "/";

        /// <summary>
        /// The number of times the message was attempted to be processed during which an exception was thrown.
        /// </summary>
        public int DefaultTtl { get; set; } = 5;

        /// <summary>
        /// Number of messages to be pre-loaded into the handler.
        /// </summary>
        public ushort PrefetchCount { get; set; } = 1;

        /// <summary>
        /// Internal library call exception handler event.
        /// </summary>
        public EventHandler<CallbackExceptionEventArgs>? ConnectionCallbackExceptionHandler { get; set; }
    }
}
