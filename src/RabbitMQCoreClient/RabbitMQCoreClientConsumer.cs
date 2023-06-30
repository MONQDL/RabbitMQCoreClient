using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQCoreClient.Configuration;
using RabbitMQCoreClient.Exceptions;
using RabbitMQCoreClient.Models;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQCoreClient
{
    public sealed class RabbitMQCoreClientConsumer : IQueueConsumer
    {
        readonly IQueueService _queueService;
        readonly IServiceScopeFactory _scopeFactory;
        readonly IRabbitMQCoreClientConsumerBuilder _builder;
        readonly ILogger _log;

        /// <summary>
        /// The RabbitMQ consume messages channel.
        /// </summary>
        public IModel? ConsumeChannel { get; private set; }

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
        public void Start()
        {
            if (_consumer != null && _consumer.IsRunning)
                return;

            if (_queueService.Connection is null || !_queueService.Connection.IsOpen)
                throw new NotConnectedException("Connection is not opened.");

            if (_queueService.SendChannel is null || _queueService.SendChannel.IsClosed)
                throw new NotConnectedException("Send channel is not opened.");

            if (!_wasSubscribed)
            {
                _queueService.OnReconnected += QueueService_OnReconnected;
                _queueService.OnConnectionShutdown += QueueService_OnConnectionShutdown;
                _wasSubscribed = true;
            }

            ConsumeChannel = _queueService.Connection.CreateModel();
            ConsumeChannel.CallbackException += Channel_CallbackException;
            ConsumeChannel.BasicQos(0, _queueService.Options.PrefetchCount, false); // Per consumer limit

            ConnectToAllQueues();
        }

        /// inheritdoc />
        public void Shutdown() => StopAndClearConsumer();

        void ConnectToAllQueues()
        {
            if (ConsumeChannel is null)
                throw new NotConnectedException("The consumer Channel is null.");

            _consumer = new AsyncEventingBasicConsumer(ConsumeChannel);

            _consumer.Received += Consumer_Received;

            // DeadLetterExchange configuration.
            if (_builder.Queues.Any(x => !string.IsNullOrEmpty(x.DeadLetterExchange)))
                ConfigureDeadLetterExchange();

            foreach (var queue in _builder.Queues)
            {
                // Set queue parameters from main configuration.
                if (_queueService.Options.UseQuorumQueues)
                    queue.UseQuorum = true;
                queue.StartQueue(ConsumeChannel, _consumer);
            }
            _log.LogInformation($"Consumer connected to {_builder.Queues.Count} queues.");
        }

        async Task Consumer_Received(object? sender, BasicDeliverEventArgs @event)
        {
            if (ConsumeChannel is null)
                throw new NotConnectedException("ConsumeChannel is null");

            var rabbitArgs = new RabbitMessageEventArgs(@event.RoutingKey)
            {
                CorrelationId = @event.BasicProperties.CorrelationId,
                ConsumerTag = @event.ConsumerTag
            };

            _log.LogDebug("New message received with deliveryTag={deliveryTag}.", @event.DeliveryTag);

            // Send a message to the death queue if ttl is over.
            if (@event.BasicProperties.Headers?.ContainsKey(AppConstants.RabbitMQHeaders.TtlHeader) == true
                && (int)@event.BasicProperties.Headers[AppConstants.RabbitMQHeaders.TtlHeader] <= 0)
            {
                ConsumeChannel.BasicNack(@event.DeliveryTag, false, false);
                _log.LogDebug("Message was rejected due to low ttl.");
                return;
            }

            if (!_builder.RoutingHandlerTypes.ContainsKey(@event.RoutingKey))
            {
                RejectDueToNoHandler(@event);
                return;
            }
            var handlerType = _builder.RoutingHandlerTypes[@event.RoutingKey].Type;
            var handlerOptions = _builder.RoutingHandlerTypes[@event.RoutingKey].Options;

            // Get the message handler service.
            using var scope = _scopeFactory.CreateScope();
            var handler = (IMessageHandler)scope.ServiceProvider.GetRequiredService(handlerType);
            if (handler is null)
            {
                RejectDueToNoHandler(@event);
                return;
            }

            handler.Options = handlerOptions ?? new();
            // If user overides the default serializer then the custom serializer will be used for the handler.
            handler.Serializer = handler.Options.CustomSerializer ?? _builder.Builder.Serializer;

            _log.LogDebug($"Created scope for handler type {handler.GetType().Name}. Start processing message.");
            try
            {
                await handler.HandleMessage(@event.Body, rabbitArgs);
                ConsumeChannel.BasicAck(@event.DeliveryTag, false);
                _log.LogDebug($"Message successfully processed by handler type {handler?.GetType().Name} " +
                              $"with deliveryTag={{deliveryTag}}.", @event.DeliveryTag);
            }
            catch (Exception e)
            {
                // Process the message depending on the given route.
                switch (handler.ErrorMessageRouter.Route)
                {
                    case Routes.DeadLetter:
                        ConsumeChannel.BasicNack(@event.DeliveryTag, false, false);
                        _log.LogError(e, "Error message with deliveryTag={deliveryTag}. " +
                            "Sent to dead letter exchange.", @event.DeliveryTag);
                        break;
                    case Routes.SourceQueue:
                        var decreaseTtl = handler.ErrorMessageRouter.TtlAction == TtlActions.Decrease;
                        ConsumeChannel.BasicAck(@event.DeliveryTag, false);

                        // Forward the message back to the queue, while the TTL of the message is reduced by 1,
                        // depending on the settings of handler.ErrorMessageRouter.TtlAction.
                        // The message is sent back to the queue using the `handlerOptions?.RetryKey` key,
                        // if specified, otherwise it is sent to the queue with the original key.
                        await _queueService.SendAsync(
                            @event.Body,
                            @event.BasicProperties,
                            exchange: @event.Exchange,
                            routingKey: !string.IsNullOrEmpty(handlerOptions?.RetryKey) ? handlerOptions.RetryKey : @event.RoutingKey,
                            decreaseTtl: decreaseTtl,
                            correlationId: @event.BasicProperties.CorrelationId);
                        _log.LogError(e, "Error message with deliveryTag={deliveryTag}. Requeue.", @event.DeliveryTag);
                        break;
                }
            }
        }

        void QueueService_OnReconnected()
        {
            StopAndClearConsumer();
            Start();
        }

        void QueueService_OnConnectionShutdown()
        {
            StopAndClearConsumer();
        }

        void ConfigureDeadLetterExchange()
        {
            if (ConsumeChannel is null)
                throw new NotConnectedException("ConsumeChannel is null");

            // Declaring DeadLetterExchange.
            var deadLetterExchanges = _builder.Queues
                .Where(x => !string.IsNullOrWhiteSpace(x.DeadLetterExchange))
                .Select(x => x.DeadLetterExchange)
                .Distinct();

            // TODO: Redo the configuration of the dead message queue in the future. So far, hardcode.
            const string deadLetterQueueName = "dead_letter";

            // We register the queue where the "rejected" messages will be stored.
            ConsumeChannel.QueueDeclare(queue: "dead_letter",
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);
            var allRoutingKeys = _builder.Queues.SelectMany(x => x.RoutingKeys).Distinct();

            foreach (var deadLetterEx in deadLetterExchanges)
            {
                ConsumeChannel.ExchangeDeclare(
                    exchange: deadLetterEx,
                    type: "direct",
                    durable: true,
                    autoDelete: false,
                    arguments: null
                    );

                if (allRoutingKeys.Any())
                    foreach (var route in allRoutingKeys)
                        ConsumeChannel.QueueBind(
                            queue: deadLetterQueueName,
                            exchange: deadLetterEx,
                            routingKey: route,
                            arguments: null
                        );
            }
        }

        void RejectDueToNoHandler(BasicDeliverEventArgs ea)
        {
            _log.LogDebug($"Message was rejected due to no handler configured for the routing key {ea.RoutingKey}.");
            ConsumeChannel?.BasicNack(ea.DeliveryTag, false, false);
        }

        void StopAndClearConsumer()
        {
            try
            {
                if (_consumer != null)
                {
                    _consumer.Received -= Consumer_Received;
                    _consumer = null;
                }

                // Closing consuming channel.
                if (ConsumeChannel != null)
                {
                    ConsumeChannel.CallbackException -= Channel_CallbackException;
                }
                if (ConsumeChannel?.IsOpen == true)
                    ConsumeChannel.Close((int)HttpStatusCode.OK, "Goodbye");
            }
            catch (Exception e)
            {
                _log.LogError(e, "Error closing consumer channel.");
                // Close() may throw an IOException if connection
                // dies - but that's ok (handled by reconnect)
            }
        }

        void Channel_CallbackException(object? sender, CallbackExceptionEventArgs? e)
        {
            if (e != null)
                _log.LogError(e.Exception, string.Join(Environment.NewLine, e.Detail.Select(x => $"{x.Key} - {x.Value}")));
        }

        public void Dispose() => StopAndClearConsumer();
    }
}
