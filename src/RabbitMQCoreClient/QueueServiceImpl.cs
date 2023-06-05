using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQCoreClient.Configuration.DependencyInjection;
using RabbitMQCoreClient.Configuration.DependencyInjection.Options;
using RabbitMQCoreClient.Exceptions;
using RabbitMQCoreClient.Serializers;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static RabbitMQCoreClient.Configuration.AppConstants.RabbitMQHeaders;

namespace RabbitMQCoreClient
{
    /// <summary>
    /// Implementations of the <see cref="IQueueService" />.
    /// </summary>
    /// <seealso cref="RabbitMQCoreClient.IQueueService" />
    public sealed class QueueServiceImpl : IQueueService
    {
        readonly ILogger<QueueServiceImpl> _log;
        static readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        readonly IList<Exchange> _exchanges;
        readonly IMessageSerializer _serializer;
        bool _connectionBlocked = false;
        IConnection? _connection;
        IModel? _sendChannel;

        /// <summary>
        /// Client Options.
        /// </summary>
        public RabbitMQCoreClientOptions Options { get; }

        /// <summary>
        /// RabbitMQ connection interface.
        /// </summary>
        public IConnection Connection => _connection!; // So far, that's it. The property is completely initialized in the constructor.

        /// <summary>
        /// Sending channel.
        /// </summary>
        public IModel SendChannel => _sendChannel!; // So far, that's it. The property is completely initialized in the constructor.

        /// <summary>
        /// Occurs when connection restored after reconnect.
        /// </summary>
        public event Action? OnReconnected;

        /// <summary>
        /// Occurs when connection is shuted down on any reason.
        /// </summary>
        public event Action? OnConnectionShutdown;

        string? GetDefaultExchange() => _exchanges.FirstOrDefault(x => x.Options.IsDefault)?.Name;

        int _reconnectAttemptsCount = 0;

        /// <summary>
        /// Initializes a new instance of the class <see cref="QueueServiceImpl" />.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="builder">The builder.</param>
        /// <exception cref="ArgumentNullException">options</exception>
        public QueueServiceImpl(RabbitMQCoreClientOptions options, ILoggerFactory loggerFactory, IRabbitMQCoreClientBuilder builder)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options), $"{nameof(options)} is null.");
            _log = loggerFactory.CreateLogger<QueueServiceImpl>();
            _exchanges = builder.Exchanges;
            _serializer = builder.Serializer;

            Reconnect(); // Start connection cycle.
        }

        /// <summary>
        /// Connects this instance to RabbitMQ.
        /// </summary>
        public void Connect()
        {
            // Protection against repeated calls to the `Connect()` method.
            if (_connection?.IsOpen == true)
            {
                _log.LogWarning("Connection already open.");
                return;
            }

            _log.LogInformation("Connecting to RabbitMQ endpoint {RabbitMQEndpoint}.", Options.HostName);

            var factory = new ConnectionFactory
            {
                HostName = Options.HostName,
                UserName = Options.UserName,
                Password = Options.Password,
                RequestedHeartbeat = TimeSpan.FromSeconds(Options.RequestedHeartbeat),
                RequestedConnectionTimeout = TimeSpan.FromMilliseconds(Options.RequestedConnectionTimeout),
                AutomaticRecoveryEnabled = false,
                TopologyRecoveryEnabled = false,
                Port = Options.Port,
                VirtualHost = Options.VirtualHost,
                DispatchConsumersAsync = true,
            };
            if (Options.SslEnabled)
            {
                var ssl = new SslOption
                {
                    Enabled = true,
                    AcceptablePolicyErrors = Options.SslAcceptablePolicyErrors,
                    Version = Options.SslVersion,
                    ServerName = Options.SslServerName ?? string.Empty,
                    CheckCertificateRevocation = Options.SslCheckCertificateRevocation,
                    CertPassphrase = Options.SslCertPassphrase ?? string.Empty,
                    CertPath = Options.SslCertPath ?? string.Empty
                };
                factory.Ssl = ssl;
            }
            _connection = factory.CreateConnection();

            _connection.ConnectionShutdown += Connection_ConnectionShutdown;
            _connection.ConnectionBlocked += Connection_ConnectionBlocked;
            _connection.CallbackException += Connection_CallbackException;
            if (Options.ConnectionCallbackExceptionHandler != null)
                _connection.CallbackException += Options.ConnectionCallbackExceptionHandler;
            _log.LogDebug("Connection opened.");

            _sendChannel = Connection.CreateModel();
            _sendChannel.CallbackException += Channel_CallbackException;
            _sendChannel.BasicQos(0, Options.PrefetchCount, false); // Per consumer limit

            foreach (var exchange in _exchanges)
            {
                exchange.StartExchange(_sendChannel);
            }
            _connectionBlocked = false;

            CheckSendChannelOpened();
            _log.LogInformation("Connected to RabbitMQ endpoint {RabbitMQEndpoint}", Options.HostName);
        }

        /// <summary>
        /// Close all connections and Cleans up.
        /// </summary>
        public void Cleanup()
        {
            _log.LogInformation("Closing and cleaning up old connection and channels.");
            try
            {
                if (_connection != null)
                {
                    _connection.ConnectionShutdown -= Connection_ConnectionShutdown;
                    _connection.CallbackException -= Connection_CallbackException;
                    _connection.ConnectionBlocked -= Connection_ConnectionBlocked;
                    if (Options.ConnectionCallbackExceptionHandler != null)
                        _connection.CallbackException -= Options.ConnectionCallbackExceptionHandler;
                }
                // Closing send channel.
                if (_sendChannel != null)
                {
                    _sendChannel.CallbackException -= Channel_CallbackException;
                }

                OnConnectionShutdown?.Invoke();
                // Closing connection.
                if (_connection?.IsOpen == true)
                    _connection.Close(TimeSpan.FromSeconds(1));
            }
            catch (Exception e)
            {
                _log.LogError(e, "Error closing connection.");
                // Close() may throw an IOException if connection
                // dies - but that's ok (handled by reconnect)
            }
        }

        /// <summary>
        /// Reconnects this instance to RabbitMQ.
        /// </summary>
        void Reconnect()
        {
            _log.LogInformation("Reconnect requested");
            Cleanup();

            var mres = new ManualResetEventSlim(false); // state is initially false

            while (!mres.Wait(Options.ReconnectionTimeout)) // loop until state is true, checking every Options.ReconnectionTimeout
            {
                if (_reconnectAttemptsCount > Options.ReconnectionAttemptsCount)
                    throw new ReconnectAttemptsExceededException($"Max reconnect attempts {Options.ReconnectionAttemptsCount} reached.");

                try
                {
                    _log.LogInformation($"Trying to connect with reconnect attempt {_reconnectAttemptsCount}");
                    Connect();
                    _reconnectAttemptsCount = 0;
                    OnReconnected?.Invoke();
                    break;
                    //mres.Set(); // state set to true - breaks out of loop
                }
                catch (Exception e)
                {
                    _reconnectAttemptsCount++;
                    Thread.Sleep(Options.ReconnectionTimeout);
                    string? innerExceptionMessage = null;
                    if (e.InnerException != null)
                        innerExceptionMessage = e.InnerException.Message;
                    _log.LogCritical(e, $"Connection failed. Detais: {e.Message} {innerExceptionMessage}. Reconnect attempts: {_reconnectAttemptsCount}", e);
                }
            }
        }

        /// <inheritdoc />
        public void Dispose() => Cleanup();

        /// <inheritdoc />
        public ValueTask SendAsync<T>(
            T obj,
            string routingKey,
            string? exchange = default,
            bool decreaseTtl = true,
            string? correlationId = default
            )
        {
            // Проверка на Null без боксинга. // https://stackoverflow.com/a/864860
            if (EqualityComparer<T>.Default.Equals(obj, default))
                throw new ArgumentNullException(nameof(obj));

            var serializedObj = _serializer.Serialize(obj);
            return SendJsonAsync(
                        serializedObj,
                        exchange: exchange,
                        routingKey: routingKey,
                        decreaseTtl: decreaseTtl,
                        correlationId: correlationId
                       );
        }

        /// <inheritdoc />
        public ValueTask SendJsonAsync(
            string json,
            string routingKey,
            string? exchange = default,
            bool decreaseTtl = true,
            string? correlationId = default)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentException($"{nameof(json)} is null or empty.", nameof(json));

            var body = Encoding.UTF8.GetBytes(json);
            var properties = CreateBasicJsonProperties();

            _log.LogDebug("Sending json message {message} to exchange {exchange} with routing key {routingKey}.", json, exchange, routingKey);

            return SendAsync(body,
                props: properties,
                exchange: exchange,
                routingKey: routingKey,
                decreaseTtl: decreaseTtl,
                correlationId: correlationId
                );
        }

        /// <inheritdoc />
        public async ValueTask SendAsync(
            byte[] obj,
            IBasicProperties props,
            string routingKey,
            string? exchange = default,
            bool decreaseTtl = true,
            string? correlationId = default)
        {
            if (string.IsNullOrEmpty(exchange))
                exchange = GetDefaultExchange();

            if (string.IsNullOrEmpty(exchange))
                throw new ArgumentException($"{nameof(exchange)} is null or empty.", nameof(exchange));

            CheckSendChannelOpened();

            await _semaphoreSlim.WaitAsync();
            try
            {
                AddTtl(props, decreaseTtl);
                AddCorrelationId(props, correlationId);
                SendChannel.BasicPublish(exchange: exchange,
                                     routingKey: routingKey,
                                     basicProperties: props,
                                     body: obj);
                _log.LogDebug("Sent raw message to exchange {exchange} with routing key {routingKey}.", exchange, routingKey);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        /// <inheritdoc />
        public ValueTask SendBatchAsync<T>(
            IEnumerable<T> objs,
            string routingKey,
            string? exchange = default,
            bool decreaseTtl = true,
            string? correlationId = default)
        {
            var messages = new List<string>();
            foreach (var obj in objs)
            {
                messages.Add(_serializer.Serialize(obj));
            }

            return SendJsonBatchAsync(
                serializedJsonList: messages,
                exchange: exchange,
                routingKey: routingKey,
                decreaseTtl: decreaseTtl,
                correlationId: correlationId
            );
        }

        /// <inheritdoc />
        public ValueTask SendJsonBatchAsync(
            IEnumerable<string> serializedJsonList,
            string routingKey,
            string? exchange = default,
            bool decreaseTtl = true,
            string? correlationId = default)
        {
            var messages = new List<(byte[] Body, IBasicProperties Props)>();
            foreach (var json in serializedJsonList)
            {
                var props = CreateBasicJsonProperties();
                AddTtl(props, decreaseTtl);
                AddCorrelationId(props, correlationId);

                var body = Encoding.UTF8.GetBytes(json);
                messages.Add((body, props));
            }

            _log.LogDebug("Sending json messages batch to exchange {exchange} with routing key {routingKey}.", exchange, routingKey);

            return SendBatchAsync(
                objs: messages,
                exchange: exchange,
                routingKey: routingKey,
                decreaseTtl: decreaseTtl,
                correlationId: correlationId
            );
        }

        /// <inheritdoc />
        public async ValueTask SendBatchAsync(
            IEnumerable<(byte[] Body, IBasicProperties Props)> objs,
            string routingKey,
            string? exchange = default,
            bool decreaseTtl = true,
            string? correlationId = default)
        {
            if (string.IsNullOrEmpty(exchange))
                exchange = GetDefaultExchange();

            if (string.IsNullOrEmpty(exchange))
                throw new ArgumentException($"{nameof(exchange)} is null or empty.", nameof(exchange));

            CheckSendChannelOpened();

            await _semaphoreSlim.WaitAsync();
            try
            {
                var batchOperation = CreateBasicJsonBatchProperties();

                foreach (var (Body, Props) in objs)
                {
                    AddTtl(Props, decreaseTtl);
                    AddCorrelationId(Props, correlationId);

                    batchOperation?.Add(exchange, routingKey, false, Props, new ReadOnlyMemory<byte>(Body));
                }
                batchOperation?.Publish();
                _log.LogDebug("Sent raw messages batch to exchange {exchange} with routing key {routingKey}.", exchange, routingKey);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        void Connection_CallbackException(object? sender, CallbackExceptionEventArgs e)
        {
            if (e != null)
                _log.LogError(e.Exception, e.Exception.Message);
        }

        void Channel_CallbackException(object? sender, CallbackExceptionEventArgs e)
        {
            if (e != null)
                _log.LogError(e.Exception, string.Join(Environment.NewLine, e.Detail.Select(x => $"{x.Key} - {x.Value}")));
        }

        void Connection_ConnectionShutdown(object? sender, ShutdownEventArgs e)
        {
            if (e != null)
                _log.LogError($"Connection broke! Reason: {e.ReplyText}");

            Reconnect();
        }

        void Connection_ConnectionBlocked(object? sender, ConnectionBlockedEventArgs e)
        {
            if (e != null)
                _log.LogError($"Connection blocked! Reason: {e.Reason}");
            _connectionBlocked = true;
            Reconnect();
        }

        [return: NotNull]
        IBasicProperties CreateBasicJsonProperties()
        {
            CheckSendChannelOpened();

            var properties = SendChannel.CreateBasicProperties();

            properties.Persistent = true;
            properties.ContentType = "application/json";
            return properties!;
        }

        void AddTtl(IBasicProperties props, bool decreaseTtl)
        {
            if (props.Headers == null)
                props.Headers = new Dictionary<string, object>();

            if (decreaseTtl)
            {
                if (props.Headers.ContainsKey(TtlHeader))
                    props.Headers[TtlHeader] = (int)props.Headers[TtlHeader] - 1;
                else
                    props.Headers.Add(TtlHeader, Options.DefaultTtl);
            }
        }

        static void AddCorrelationId(IBasicProperties props, string? correlationId)
        {
            if (!string.IsNullOrEmpty(correlationId))
                props.CorrelationId = correlationId;
        }

        [return: NotNull]
        IBasicPublishBatch CreateBasicJsonBatchProperties() => SendChannel.CreateBasicPublishBatch();

        void CheckSendChannelOpened()
        {
            if (_sendChannel is null || _sendChannel.IsClosed)
                throw new NotConnectedException("Channel not opened.");

            if (_connectionBlocked)
                throw new NotConnectedException("Connection is blocked.");
        }
    }
}