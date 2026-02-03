using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQCoreClient.Configuration;
using RabbitMQCoreClient.Events;
using RabbitMQCoreClient.Exceptions;
using RabbitMQCoreClient.Models;
using System.Net;

namespace RabbitMQCoreClient;

/// <summary>
/// Default RabbitMQ consumer.
/// </summary>
public sealed class RabbitMQCoreClientConsumer : IQueueConsumer
{
    readonly IQueueService _queueService;
    readonly IServiceScopeFactory _scopeFactory;
    readonly IRabbitMQCoreClientConsumerBuilder _builder;
    readonly ILogger _log;
    CancellationToken _serviceLifetimeToken = default;

    /// <summary>
    /// The RabbitMQ consume messages channel.
    /// </summary>
    public IChannel? ConsumeChannel { get; private set; }

    AsyncEventingBasicConsumer? _consumer;

    /// <summary>
    /// The Async consumer, with default consume method configurated.
    /// </summary>
    public AsyncEventingBasicConsumer? Consumer => _consumer;

    bool _wasSubscribed = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMQCoreClientConsumer"/> class.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="log">The log.</param>
    /// <param name="queueService">The queue service.</param>
    /// <param name="scopeFactory">The scope factory.</param>
    /// <exception cref="ArgumentNullException">
    /// scopeFactory
    /// or
    /// queueService
    /// or
    /// log
    /// or
    /// builder
    /// </exception>
    public RabbitMQCoreClientConsumer(
        IRabbitMQCoreClientConsumerBuilder builder,
        ILogger<RabbitMQCoreClientConsumer> log,
        IQueueService queueService,
        IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory), $"{nameof(scopeFactory)} is null.");
        _queueService = queueService ?? throw new ArgumentNullException(nameof(queueService), $"{nameof(queueService)} is null.");
        _log = log ?? throw new ArgumentNullException(nameof(log), $"{nameof(log)} is null.");
        _builder = builder ?? throw new ArgumentNullException(nameof(builder), $"{nameof(builder)} is null.");
    }

    /// inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken != default)
            _serviceLifetimeToken = cancellationToken;

        if (_consumer != null && _consumer.IsRunning)
            return;

        if (_queueService.Connection is null || !_queueService.Connection.IsOpen)
            await _queueService.ConnectAsync(cancellationToken);

        if (_queueService.Connection is null || !_queueService.Connection.IsOpen)
            throw new NotConnectedException("Connection is not opened.");

        if (_queueService.PublishChannel is null || _queueService.PublishChannel.IsClosed)
            throw new NotConnectedException("Send channel is not opened.");

        if (!_wasSubscribed)
        {
            _queueService.ReconnectedAsync += QueueService_OnReconnected;
            _queueService.ConnectionShutdownAsync += QueueService_OnConnectionShutdown;
            _wasSubscribed = true;
        }

        ConsumeChannel = await _queueService.Connection.CreateChannelAsync(cancellationToken: cancellationToken);
        ConsumeChannel.CallbackExceptionAsync += Channel_CallbackException;
        await ConsumeChannel.BasicQosAsync(0, _queueService.Options.PrefetchCount, false, cancellationToken); // Per consumer limit

        await ConnectToAllQueues();
    }

    /// inheritdoc />
    public async Task ShutdownAsync() => await StopAndClearConsumer();

    async Task ConnectToAllQueues()
    {
        if (ConsumeChannel is null)
            throw new NotConnectedException("The consumer Channel is null.");

        _consumer = new AsyncEventingBasicConsumer(ConsumeChannel);

        _consumer.ReceivedAsync += Consumer_Received;

        // DeadLetterExchange configuration.
        if (_builder.Queues.Any(x => !string.IsNullOrEmpty(x.DeadLetterExchange)))
            await ConfigureDeadLetterExchange();

        foreach (var queue in _builder.Queues)
        {
            // Set queue parameters from main configuration.
            if (_queueService.Options.UseQuorumQueues)
                queue.UseQuorum = true;
            await queue.StartQueueAsync(ConsumeChannel, _consumer, _serviceLifetimeToken);
        }
        _log.LogInformation("Consumer connected to '{QueuesCount}' queues.", _builder.Queues.Count);
    }

    async Task Consumer_Received(object? sender, BasicDeliverEventArgs @event)
    {
        if (ConsumeChannel is null)
            throw new NotConnectedException("ConsumeChannel is null");

        var rabbitArgs = new RabbitMessageEventArgs(@event.RoutingKey, @event.ConsumerTag);

        if (_log.IsEnabled(LogLevel.Debug))
            _log.LogDebug("New message received with deliveryTag='{DeliveryTag}'.", @event.DeliveryTag);

        // Send a message to the death queue if ttl is over.
        if (@event.BasicProperties.Headers?.TryGetValue(AppConstants.RabbitMQHeaders.TtlHeader, out var ttl) == true
            && ttl is int ttlInt
            && ttlInt <= 0)
        {
            await ConsumeChannel.BasicNackAsync(@event.DeliveryTag, false, false, _serviceLifetimeToken);
            if (_log.IsEnabled(LogLevel.Debug))
                _log.LogDebug("Message was rejected due to low ttl.");
            return;
        }

        if (!_builder.RoutingHandlerTypes.TryGetValue(@event.RoutingKey, out var result))
        {
            await RejectDueToNoHandler(@event);
            return;
        }
        var handlerType = result.Type;
        var handlerOptions = result.Options;

        // Get the message handler service.
        using var scope = _scopeFactory.CreateScope();
        var handler = (IMessageHandler)scope.ServiceProvider.GetRequiredService(handlerType);
        if (handler is null)
        {
            await RejectDueToNoHandler(@event);
            return;
        }

        var handlerContext = new MessageHandlerContext(new(), handlerOptions);

        if (_log.IsEnabled(LogLevel.Debug))
            _log.LogDebug("Created scope for handler type '{TypeName}'. Start processing message.",
                handler.GetType().Name);
        try
        {
            await handler.HandleMessage(@event.Body, rabbitArgs, handlerContext);
            await ConsumeChannel.BasicAckAsync(@event.DeliveryTag, false, _serviceLifetimeToken);
            if (_log.IsEnabled(LogLevel.Debug))
                _log.LogDebug("Message successfully processed by handler type '{TypeName}' " +
                          "with deliveryTag='{DeliveryTag}'.", handler?.GetType().Name, @event.DeliveryTag);
        }
        catch (Exception e)
        {
            // Process the message depending on the given route.
            switch (handlerContext.ErrorMessageRouter.Route)
            {
                case Routes.DeadLetter:
                    await ConsumeChannel.BasicNackAsync(@event.DeliveryTag, false, false, _serviceLifetimeToken);
                    _log.LogError(e, "Error message with deliveryTag='{DeliveryTag}'. " +
                        "Sent to dead letter exchange.", @event.DeliveryTag);
                    break;
                case Routes.SourceQueue:
                    var decreaseTtl = handlerContext.ErrorMessageRouter.TtlAction == TtlActions.Decrease;
                    await ConsumeChannel.BasicAckAsync(@event.DeliveryTag, false, _serviceLifetimeToken);

                    // Forward the message back to the queue, while the TTL of the message is reduced by 1,
                    // depending on the settings of handler.ErrorMessageRouter.TtlAction.
                    // The message is sent back to the queue using the `handlerOptions?.RetryKey` key,
                    // if specified, otherwise it is sent to the queue with the original key.
                    await _queueService.SendAsync(
                        @event.Body,
                        new BasicProperties(@event.BasicProperties),
                        exchange: @event.Exchange,
                        routingKey: !string.IsNullOrEmpty(handlerOptions?.RetryKey) ? handlerOptions.RetryKey : @event.RoutingKey,
                        decreaseTtl: decreaseTtl);
                    _log.LogError(e, "Error message with deliveryTag='{DeliveryTag}'. Requeue.", @event.DeliveryTag);
                    break;
            }
        }
    }

    async Task QueueService_OnReconnected(object? sender, ReconnectEventArgs args)
    {
        await StopAndClearConsumer();
        await StartAsync();
    }

    Task QueueService_OnConnectionShutdown(object? sender, ShutdownEventArgs args) =>
        StopAndClearConsumer();

    async Task ConfigureDeadLetterExchange()
    {
        if (ConsumeChannel is null)
            throw new NotConnectedException("ConsumeChannel is null");

        // Declaring DeadLetterExchange.
        var deadLetterExchanges = _builder.Queues
            .Where(x => !string.IsNullOrWhiteSpace(x.DeadLetterExchange))
            .Select(x => x.DeadLetterExchange!)
            .Distinct();

        // TODO: Rewrite the configuration of the dead message queue in the future. So far, hardcode.
        const string deadLetterQueueName = "dead_letter";

        // We register the queue where the "rejected" messages will be stored.
        await ConsumeChannel.QueueDeclareAsync(queue: deadLetterQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: _serviceLifetimeToken);
        var allRoutingKeys = _builder
            .Queues
            .SelectMany(x => x.RoutingKeys)
            .Distinct()
            .ToArray();

        foreach (var deadLetterEx in deadLetterExchanges)
        {
            await ConsumeChannel.ExchangeDeclareAsync(
                exchange: deadLetterEx,
                type: "direct",
                durable: true,
                autoDelete: false,
                arguments: null,
                cancellationToken: _serviceLifetimeToken
                );

            foreach (var route in allRoutingKeys)
                await ConsumeChannel.QueueBindAsync(
                    queue: deadLetterQueueName,
                    exchange: deadLetterEx,
                    routingKey: route,
                    arguments: null,
                    cancellationToken: _serviceLifetimeToken
                );
        }
    }

    async Task RejectDueToNoHandler(BasicDeliverEventArgs ea)
    {
        if (_log.IsEnabled(LogLevel.Debug))
            _log.LogDebug("Message was rejected due to no handler configured for the routing key '{RoutingKey}'.",
                ea.RoutingKey);

        if (ConsumeChannel != null)
            await ConsumeChannel.BasicNackAsync(ea.DeliveryTag, false, false, _serviceLifetimeToken);
    }

    async Task StopAndClearConsumer()
    {
        _log.LogInformation("Closing and cleaning up consumer connection and channels.");
        try
        {
            if (_queueService != null)
            {
                _queueService.ReconnectedAsync -= QueueService_OnReconnected;
                _queueService.ConnectionShutdownAsync -= QueueService_OnConnectionShutdown;
            }

            if (_consumer != null)
            {
                _consumer.ReceivedAsync -= Consumer_Received;
                _consumer = null;
            }

            // Closing consuming channel.
            if (ConsumeChannel != null)
                ConsumeChannel.CallbackExceptionAsync -= Channel_CallbackException;

            if (ConsumeChannel?.IsOpen == true)
                await ConsumeChannel.CloseAsync((int)HttpStatusCode.OK, "Goodbye");
        }
        catch (Exception e)
        {
            _log.LogError(e, "Error closing consumer channel.");
            // Close() may throw an IOException if connection
            // dies - but that's ok (handled by reconnect)
        }
    }

    Task Channel_CallbackException(object? sender, CallbackExceptionEventArgs? e)
    {
        if (e != null)
        {
            var message = string.Join(Environment.NewLine, e.Detail.Select(x => $"{x.Key} - {x.Value}"));

            _log.LogError(e.Exception, message);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await StopAndClearConsumer();
}
