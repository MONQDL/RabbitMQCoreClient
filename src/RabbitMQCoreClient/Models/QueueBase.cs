using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQCoreClient.Exceptions;

namespace RabbitMQCoreClient.Configuration.DependencyInjection.Options;

/// <summary>
/// Options to be applied to the message queue.
/// </summary>
public abstract class QueueBase
{
    /// <summary>
    /// Create new object of <see cref="QueueBase"/>.
    /// </summary>
    /// <param name="name">The queue Name. If null, then the name will be automatically chosen.</param>
    /// <param name="durable">If true, the queue will be saved on disc.</param>
    /// <param name="exclusive">If true, then the queue will be used by single service and will be deleted after client will disconnect.
    /// Except <see cref="UseQuorum"/> is true. Then the queue will be created with be created with header <see cref="AppConstants.RabbitMQHeaders.QueueExpiresHeader"/></param>
    /// <param name="autoDelete">If true, the queue will be automatically deleted on client disconnect.</param>
    /// <param name="useQuorum">While creating the queue use parameter "x-queue-type": "quorum".</param>
    protected QueueBase(string? name, bool durable, bool exclusive, bool autoDelete, bool useQuorum)
    {
        Name = name;
        Durable = durable;
        Exclusive = exclusive;
        AutoDelete = autoDelete;
        UseQuorum = useQuorum;
    }

    /// <summary>
    /// The queue Name. If null, then the name will be automatically chosen.
    /// </summary>
    public virtual string? Name { get; protected set; }

    /// <summary>
    /// If true, the queue will be saved on disc.
    /// </summary>
    public virtual bool Durable { get; protected set; }

    /// <summary>
    /// If true, then the queue will be used by single service and will be deleted after client will disconnect.
    /// Except <see cref="UseQuorum"/> is true. Then the queue will be created with be created with header <see cref="AppConstants.RabbitMQHeaders.QueueExpiresHeader"/>
    /// </summary>
    public virtual bool Exclusive { get; protected set; }

    /// <summary>
    /// If true, the queue will be automatically deleted on client disconnect.
    /// </summary>
    public virtual bool AutoDelete { get; protected set; }

    /// <summary>
    /// The name of the exchange point that will receive messages for which a reject or nack was received.
    /// </summary>
    public virtual string? DeadLetterExchange { get; set; }

    /// <summary>
    /// While creating the queue use parameter "x-queue-type": "quorum".
    /// </summary>
    public virtual bool UseQuorum { get; set; } = false;

    /// <summary>
    /// List of additional parameters that will be used when initializing the queue.
    /// </summary>
    public virtual IDictionary<string, object?> Arguments { get; set; } = new Dictionary<string, object?>();

    /// <summary>
    /// ist of routing keys for the queue.
    /// </summary>
    public virtual HashSet<string> RoutingKeys { get; set; } = [];

    /// <summary>
    /// The list of exchange points to which the queue is bound.
    /// </summary>
    public virtual HashSet<string> Exchanges { get; set; } = [];

    /// <summary>
    /// Declare the queue on <see cref="Exchanges"/> and start consuming messages.
    /// </summary>
    public virtual async Task StartQueueAsync(IChannel channel, 
        AsyncEventingBasicConsumer consumer, 
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(DeadLetterExchange)
            && !Arguments.ContainsKey(AppConstants.RabbitMQHeaders.DeadLetterExchangeHeader))
            Arguments.Add(AppConstants.RabbitMQHeaders.DeadLetterExchangeHeader, DeadLetterExchange);

        if (UseQuorum && !Arguments.ContainsKey(AppConstants.RabbitMQHeaders.QueueTypeHeader))
            Arguments.Add(AppConstants.RabbitMQHeaders.QueueTypeHeader, "quorum");

        if (UseQuorum && AutoDelete && !Arguments.ContainsKey(AppConstants.RabbitMQHeaders.QueueExpiresHeader))
            Arguments.Add(AppConstants.RabbitMQHeaders.QueueExpiresHeader, 10000);

        var declaredQueue = await channel.QueueDeclareAsync(queue: Name ?? string.Empty,
                durable: UseQuorum || Durable,
                exclusive: !UseQuorum && Exclusive,
                autoDelete: !UseQuorum && AutoDelete,
                arguments: Arguments, 
                cancellationToken: cancellationToken)
            ?? throw new QueueBindException("Queue is not properly bind.");
        if (RoutingKeys.Count > 0)
            foreach (var exchangeName in Exchanges)
            {
                await BindToExchangeAsync(channel, declaredQueue, exchangeName, cancellationToken);
            }

        await channel.BasicConsumeAsync(queue: declaredQueue.QueueName,
            autoAck: false,
            consumer: consumer,
            consumerTag: $"amq.{declaredQueue.QueueName}.{Guid.NewGuid()}",
            cancellationToken: cancellationToken
            );
    }

    async Task BindToExchangeAsync(IChannel channel, QueueDeclareOk declaredQueue, string exchangeName, CancellationToken cancellationToken = default)
    {
        foreach (var route in RoutingKeys)
            await channel.QueueBindAsync(
                queue: declaredQueue.QueueName,
                exchange: exchangeName,
                routingKey: route,
                cancellationToken: cancellationToken
            );
    }
}
