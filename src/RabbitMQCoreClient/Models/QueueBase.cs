using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQCoreClient.Exceptions;

namespace RabbitMQCoreClient.Configuration.DependencyInjection.Options;

/// <summary>
/// Options to be applied to the message queue.
/// </summary>
public abstract class QueueBase
{
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
    /// <param name="channel"></param>
    /// <param name="consumer"></param>
    public virtual async Task StartQueue(IChannel channel, AsyncEventingBasicConsumer consumer)
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
                arguments: Arguments)
            ?? throw new QueueBindException("Queue is not properly bind.");
        if (RoutingKeys.Count > 0)
            foreach (var exchangeName in Exchanges)
            {
                await BindToExchange(channel, declaredQueue, exchangeName);
            }

        await channel.BasicConsumeAsync(queue: declaredQueue.QueueName,
            autoAck: false,
            consumer: consumer,
            consumerTag: $"amq.{declaredQueue.QueueName}.{Guid.NewGuid()}"
            );
    }

    async Task BindToExchange(IChannel channel, QueueDeclareOk declaredQueue, string exchangeName)
    {
        foreach (var route in RoutingKeys)
            await channel.QueueBindAsync(
                queue: declaredQueue.QueueName,
                exchange: exchangeName,
                routingKey: route
            );
    }
}
