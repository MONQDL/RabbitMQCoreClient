using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQCoreClient.Configuration.DependencyInjection;
using RabbitMQCoreClient.Configuration.DependencyInjection.Options;
using RabbitMQCoreClient.Events;
using RabbitMQCoreClient.Exceptions;
using RabbitMQCoreClient.Serializers;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using static RabbitMQCoreClient.Configuration.AppConstants.RabbitMQHeaders;

namespace RabbitMQCoreClient;

/// <summary>
/// Implementation of the <see cref="IQueueService" />.
/// </summary>
/// <seealso cref="RabbitMQCoreClient.IQueueService" />
public sealed class QueueService : IQueueService
{
    readonly ILogger<QueueService> _log;
    readonly IList<Exchange> _exchanges;
    readonly IMessageSerializer _serializer;
    bool _connectionBlocked = false;
    IConnection? _connection;
    IChannel? _publishChannel;

    CancellationToken _serviceLifetimeToken = default;

    /// <inheritdoc />
    public RabbitMQCoreClientOptions Options { get; }

    /// <inheritdoc />
    public IConnection? Connection => _connection;

    /// <inheritdoc />
    public IChannel? PublishChannel => _publishChannel;

    /// <inheritdoc />
    public event AsyncEventHandler<ReconnectEventArgs> ReconnectedAsync = default!;

    /// <inheritdoc />
    public event AsyncEventHandler<ShutdownEventArgs> ConnectionShutdownAsync = default!;

    string? GetDefaultExchange() => _exchanges.FirstOrDefault(x => x.Options.IsDefault)?.Name;

    long _reconnectAttemptsCount;

    /// <summary>
    /// Initializes a new instance of the class <see cref="QueueService" />.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="builder">The builder.</param>
    /// <exception cref="ArgumentNullException">options</exception>
    public QueueService(RabbitMQCoreClientOptions options, ILoggerFactory loggerFactory, IRabbitMQCoreClientBuilder builder)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options), $"{nameof(options)} is null.");
        _log = loggerFactory.CreateLogger<QueueService>();
        _exchanges = builder.Exchanges;
        _serializer = builder.Serializer ?? new SystemTextJsonMessageSerializer();
    }

    /// <summary>
    /// Connects this instance to RabbitMQ.
    /// </summary>
    async Task ConnectInternal()
    {
        // Protection against repeated calls to the `Connect()` method.
        if (_connection?.IsOpen == true)
        {
            _log.LogWarning("Connection already open.");
            return;
        }

        _log.LogInformation("Connecting to RabbitMQ endpoint {HostName}.", Options.HostName);

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
            VirtualHost = Options.VirtualHost
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
        _connection = await factory.CreateConnectionAsync(_serviceLifetimeToken);

        _connection.ConnectionShutdownAsync += Connection_ConnectionShutdown;
        _connection.ConnectionBlockedAsync += Connection_ConnectionBlocked;
        _connection.CallbackExceptionAsync += Connection_CallbackException;
        if (Options.ConnectionCallbackExceptionHandler != null)
            _connection.CallbackExceptionAsync += Options.ConnectionCallbackExceptionHandler;
        _log.LogDebug("Connection opened.");

        _publishChannel = await _connection.CreateChannelAsync(cancellationToken: _serviceLifetimeToken);
        _publishChannel.CallbackExceptionAsync += Channel_CallbackException;
        await _publishChannel.BasicQosAsync(0, Options.PrefetchCount, false, _serviceLifetimeToken); // Per consumer limit

        foreach (var exchange in _exchanges)
        {
            await exchange.StartExchangeAsync(_publishChannel, _serviceLifetimeToken);
        }
        _connectionBlocked = false;

        CheckSendChannelOpened();
        _log.LogInformation("Connected to RabbitMQ endpoint {HostName}", Options.HostName);
    }

    /// <summary>
    /// Close all connections and Cleans up.
    /// </summary>
    public async Task ShutdownAsync()
    {
        _log.LogInformation("Closing and cleaning up publisher connection and channels.");
        try
        {
            if (_connection != null)
            {
                _connection.ConnectionShutdownAsync -= Connection_ConnectionShutdown;
                _connection.CallbackExceptionAsync -= Connection_CallbackException;
                _connection.ConnectionBlockedAsync -= Connection_ConnectionBlocked;
                if (Options.ConnectionCallbackExceptionHandler != null)
                    _connection.CallbackExceptionAsync -= Options.ConnectionCallbackExceptionHandler;
            }
            // Closing send channel.
            if (_publishChannel != null)
                _publishChannel.CallbackExceptionAsync -= Channel_CallbackException;

            // Closing connection.
            if (_connection?.IsOpen == true)
                await _connection.CloseAsync(TimeSpan.FromSeconds(1));

            if (ConnectionShutdownAsync != null)
                await ConnectionShutdownAsync.Invoke(this, new ShutdownEventArgs(ShutdownInitiator.Application,
                    0, "Closed gracefully", 0, 0, null, default));
        }
        catch (Exception e)
        {
            _log.LogError(e, "Error closing connection.");
            // Close() may throw an IOException if connection
            // dies - but that's ok (handled by reconnect)
        }
    }

    /// <inheritdoc />
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken != default)
            _serviceLifetimeToken = cancellationToken;

        if (_connection is null)
            _log.LogInformation("Start RabbitMQ connection");
        else
        {
            _log.LogInformation("RabbitMQ reconnect requested");
            await ShutdownAsync();
        }

        while (true) // loop until state is true, checking every Options.ReconnectionTimeout
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (Options.ReconnectionAttemptsCount is not null 
                    && _reconnectAttemptsCount > Options.ReconnectionAttemptsCount)
                throw new ReconnectAttemptsExceededException($"Max reconnect attempts {Options.ReconnectionAttemptsCount} reached.");

            try
            {
                _log.LogInformation("Trying to connect with reconnect attempt {ReconnectAttempt}", _reconnectAttemptsCount);
                await ConnectInternal();
                _reconnectAttemptsCount = 0;

                if (ReconnectedAsync != null)
                    await ReconnectedAsync.Invoke(this, new ReconnectEventArgs());

                break; // state set to true - breaks out of loop
            }
            catch (Exception e)
            {
                _reconnectAttemptsCount++;
                await Task.Delay(Options.ReconnectionTimeout, cancellationToken);
                string? innerExceptionMessage = null;
                if (e.InnerException != null)
                    innerExceptionMessage = e.InnerException.Message;
                _log.LogCritical(e, "Connection failed. Details: {ErrorMessage} Reconnect attempts: {ReconnectAttempt}",
                    e.Message + " " + innerExceptionMessage, _reconnectAttemptsCount);
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask AsyncDispose() => await ShutdownAsync();

    /// <inheritdoc />
    public ValueTask SendJsonAsync(
        string json,
        string routingKey,
        string? exchange = default,
        bool decreaseTtl = true)
    {
        if (string.IsNullOrEmpty(json))
            throw new ArgumentException($"{nameof(json)} is null or empty.", nameof(json));

        var body = Encoding.UTF8.GetBytes(json);
        var properties = CreateBasicJsonProperties();

        _log.LogDebug("Sending json message {Message} to exchange {Exchange} " +
            "with routing key {RoutingKey}.", json, exchange, routingKey);

        return SendAsync(body,
            props: properties,
            exchange: exchange,
            routingKey: routingKey,
            decreaseTtl: decreaseTtl
            );
    }

    /// <inheritdoc />
    public ValueTask SendJsonAsync(
        ReadOnlyMemory<byte> jsonBytes,
        string routingKey,
        string? exchange = default,
        bool decreaseTtl = true)
    {
        if (jsonBytes.Length == 0)
            throw new ArgumentException($"{nameof(jsonBytes)} is null or empty.", nameof(jsonBytes));

        var properties = CreateBasicJsonProperties();

        _log.LogDebug("Sending json message to exchange {Exchange} " +
            "with routing key {RoutingKey}.", exchange, routingKey);

        return SendAsync(jsonBytes,
            props: properties,
            exchange: exchange,
            routingKey: routingKey,
            decreaseTtl: decreaseTtl
            );
    }

    /// <inheritdoc />
    public ValueTask SendAsync<T>(
        T obj,
        string routingKey,
        string? exchange = default,
        bool decreaseTtl = true
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
                    decreaseTtl: decreaseTtl
                   );
    }

    /// <inheritdoc />
    public ValueTask SendAsync(
        byte[] obj,
        BasicProperties props,
        string routingKey,
        string? exchange = default,
        bool decreaseTtl = true) => SendAsync(new ReadOnlyMemory<byte>(obj),
            props: props,
            exchange: exchange,
            routingKey: routingKey,
            decreaseTtl: decreaseTtl
            );

    /// <inheritdoc />
    public ValueTask SendBatchAsync<T>(
        IEnumerable<T> objs,
        string routingKey,
        string? exchange = default,
        bool decreaseTtl = true)
    {
        var messages = new List<ReadOnlyMemory<byte>>();
        foreach (var obj in objs)
            messages.Add(_serializer.Serialize(obj));

        return SendJsonBatchAsync(
            serializedJsonList: messages,
            exchange: exchange,
            routingKey: routingKey,
            decreaseTtl: decreaseTtl
        );
    }

    /// <inheritdoc />
    public ValueTask SendJsonBatchAsync(
        IEnumerable<string> serializedJsonList,
        string routingKey,
        string? exchange = default,
        bool decreaseTtl = true)
    {
        var messages = new List<(ReadOnlyMemory<byte> Body, BasicProperties Props)>();
        foreach (var json in serializedJsonList)
        {
            var props = CreateBasicJsonProperties();
            AddTtl(props, decreaseTtl);

            var body = Encoding.UTF8.GetBytes(json);
            messages.Add((body, props));
        }

        _log.LogDebug("Sending json messages batch to exchange {Exchange} " +
            "with routing key {RoutingKey}.", exchange, routingKey);

        return SendBatchAsync(
            objs: messages,
            exchange: exchange,
            routingKey: routingKey,
            decreaseTtl: decreaseTtl
        );
    }

    /// <inheritdoc />
    public ValueTask SendJsonBatchAsync(
        IEnumerable<ReadOnlyMemory<byte>> serializedJsonList,
        string routingKey,
        string? exchange = default,
        bool decreaseTtl = true)
    {
        var messages = new List<(ReadOnlyMemory<byte> Body, BasicProperties Props)>();
        foreach (var json in serializedJsonList)
        {
            var props = CreateBasicJsonProperties();
            AddTtl(props, decreaseTtl);

            messages.Add((json, props));
        }

        _log.LogDebug("Sending json messages batch to exchange {Exchange} " +
            "with routing key {RoutingKey}.", exchange, routingKey);

        return SendBatchAsync(
            objs: messages,
            exchange: exchange,
            routingKey: routingKey,
            decreaseTtl: decreaseTtl
        );
    }

    #region Base Publish methods

    /// <inheritdoc />
    public async ValueTask SendAsync(
        ReadOnlyMemory<byte> obj,
        BasicProperties props,
        string routingKey,
        string? exchange = default,
        bool decreaseTtl = true)
    {
        if (string.IsNullOrEmpty(exchange))
            exchange = GetDefaultExchange();

        if (string.IsNullOrEmpty(exchange))
            throw new ArgumentException($"{nameof(exchange)} is null or empty.", nameof(exchange));

        if (obj.Length > Options.MaxBodySize)
        {
            var decodedString = DecodeMessageAsString(obj);
            throw new BadMessageException($"The message size \"{obj.Length}\" exceeds max body limit of \"{Options.MaxBodySize}\" " +
                $"on routing key \"{routingKey}\" (exchange: \"{exchange}\"). Decoded message part: {decodedString}");
        }

        CheckSendChannelOpened();

        AddTtl(props, decreaseTtl);

        await _publishChannel.BasicPublishAsync(exchange: exchange,
                             routingKey: routingKey,
                             mandatory: false, // Just not reacting when no queue is subscribed for key.
                             basicProperties: props,
                             body: obj);
        _log.LogDebug("Sent raw message to exchange {Exchange} with routing key {RoutingKey}.",
            exchange, routingKey);
    }

    /// <inheritdoc />
    public async ValueTask SendBatchAsync(
        IEnumerable<(ReadOnlyMemory<byte> Body, BasicProperties Props)> objs,
        string routingKey,
        string? exchange = default,
        bool decreaseTtl = true)
    {
        if (string.IsNullOrEmpty(exchange))
            exchange = GetDefaultExchange();

        if (string.IsNullOrEmpty(exchange))
            throw new ArgumentException($"{nameof(exchange)} is null or empty.", nameof(exchange));

        CheckSendChannelOpened();

        const ushort MAX_OUTSTANDING_CONFIRMS = 256;

        var batchSize = Math.Max(1, MAX_OUTSTANDING_CONFIRMS / 2);

        var publishTasks = new List<ValueTask>();

        foreach (var (body, props) in objs)
        {
            if (body.Length > Options.MaxBodySize)
            {
                var decodedString = DecodeMessageAsString(body);

                _log.LogError("Skipped message due to message size '{MessageSize}' exceeds max body limit of '{MaxBodySize}' " +
                    "on routing key '{RoutingKey}' (exchange: '{Exchange}'. Decoded message part: {DecodedString})",
                    body.Length,
                    Options.MaxBodySize,
                    routingKey,
                    exchange,
                    decodedString);
                continue;
            }

            AddTtl(props, decreaseTtl);

            var publishTask = _publishChannel.BasicPublishAsync(
                exchange: exchange,
                routingKey: routingKey,
                body: body,
                mandatory: false, // Just not reacting when no queue is subscribed for key.
                basicProperties: props);
            publishTasks.Add(publishTask);

            await MaybeAwaitPublishes(publishTasks, batchSize);
        }

        // Await any remaining tasks in case message count was not
        // evenly divisible by batch size.
        await MaybeAwaitPublishes(publishTasks, 0);

        _log.LogDebug("Sent raw messages batch to exchange {Exchange} " +
            "with routing key {RoutingKey}.", exchange, routingKey);
    }

    async Task MaybeAwaitPublishes(List<ValueTask> publishTasks, int batchSize)
    {
        if (publishTasks.Count >= batchSize)
        {
            foreach (var pt in publishTasks)
            {
                try
                {
                    await pt;
                }
                catch (Exception e)
                {
                    _log.LogError(e, "[ERROR] saw nack or return, ex: '{ErrorMessage}'", e.Message);
                }
            }
            publishTasks.Clear();
        }
    }
    #endregion

    #region Actions implementation
    Task Connection_CallbackException(object? sender, CallbackExceptionEventArgs e)
    {
        if (e != null)
            _log.LogError(e.Exception, e.Exception.Message);

        return Task.CompletedTask;
    }

    Task Channel_CallbackException(object? sender, CallbackExceptionEventArgs e)
    {
        if (e != null)
        {
            var message = string.Join(Environment.NewLine, e.Detail.Select(x => $"{x.Key} - {x.Value}"));
            _log.LogError(e.Exception, message);
        }

        return Task.CompletedTask;
    }

    async Task Connection_ConnectionShutdown(object? sender, ShutdownEventArgs args)
    {
        if (args != null)
            _log.LogError("Connection broke! Reason: {BrokeReason}", args.ReplyText);

        if (ConnectionShutdownAsync != null)
            await ConnectionShutdownAsync.Invoke(sender ?? this, args!);

        await ConnectAsync();
    }

    async Task Connection_ConnectionBlocked(object? sender, ConnectionBlockedEventArgs e)
    {
        if (e != null)
            _log.LogError("Connection blocked! Reason: {Reason}", e.Reason);
        _connectionBlocked = true;
        await ConnectAsync();
    }
    #endregion

    static BasicProperties CreateBasicJsonProperties()
    {
        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json"
        };
        return properties;
    }

    void AddTtl(BasicProperties props, bool decreaseTtl)
    {
        // Не делаем ничего, если признак “уменьшать TTL” выключен
        if (!decreaseTtl) return;
        // Убедимся, что словарь заголовков существует
        props.Headers ??= new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        // Попробуем получить текущий TTL
        var currentTtl = Options.DefaultTtl;   // <‑- значение по умолчанию
        if (props.Headers.TryGetValue(TtlHeader, out var ttlObj)
            && !TryParseInt(ttlObj, out currentTtl)) // Попытка безопасно преобразовать к int
        {
            // В случае неправильного типа – оставляем default.
            _log.LogWarning("Header '{TtlHeader}' contains invalid value: {TtlObj}",
                TtlHeader, ttlObj);
            currentTtl = Options.DefaultTtl;
        }
        // Снижаем TTL, но не допускаем отрицательных значений
        currentTtl = Math.Max(currentTtl - 1, 0);
        // Обновляем заголовок
        props.Headers[TtlHeader] = currentTtl;
    }

    static bool TryParseInt(object? value, out int result)
    {
        result = 0;
        switch (value)
        {
            case int i:
                result = i;
                return true;
            case long l when l is >= int.MinValue and <= int.MaxValue:
                result = (int)l;
                return true;
            case short s:
                result = s;
                return true;
            case byte b:
                result = b;
                return true;
            case string str when int.TryParse(str, out var parsed):
                result = parsed;
                return true;
            case null:
            default:
                return false;
        }
    }

    [MemberNotNull(nameof(_publishChannel))]
    void CheckSendChannelOpened()
    {
        if (_publishChannel is null || _publishChannel.IsClosed)
            throw new NotConnectedException("Channel not opened.");

        if (_connectionBlocked)
            throw new NotConnectedException("Connection is blocked.");
    }

    /// <summary>
    /// Try decode bytes array to string 1024 length or write as hex.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    static string DecodeMessageAsString(ReadOnlyMemory<byte> obj)
    {
        int bufferSize = 1024; // We need ~1 KB of text to log.
        var decodedStringLength = Math.Min(obj.Length, bufferSize);
        var slice = obj.Span.Slice(0, decodedStringLength);

        // Find the index of the last complete character
        var lastValidIndex = slice.Length - 1;
        while (lastValidIndex >= 0 && (slice[lastValidIndex] & 0b11000000) == 0b10000000)
        {
            // If a byte is a "continuation" of a UTF-8 character (starts with 10xxxxxx),
            // it means that it is part of the previous character and needs to be discarded.
            lastValidIndex--;
        }

        // Truncating to the last valid character
        slice = slice.Slice(0, lastValidIndex + 1);

        // Checking the string is UTF8
        var decoder = Encoding.UTF8.GetDecoder();
        var buffer = new char[decodedStringLength];
        decoder.Convert(slice, buffer, flush: true, out _, out var charsUsed, out var completed);
        if (completed)
            return new string(buffer, 0, charsUsed);
        else
        {
            // Generating bytes as string.
            return BitConverter.ToString(obj.Span.ToArray(), 0, decodedStringLength);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_publishChannel != null)
            await _publishChannel.DisposeAsync();

        if (_connection != null)
            await _connection.DisposeAsync();
    }
}
