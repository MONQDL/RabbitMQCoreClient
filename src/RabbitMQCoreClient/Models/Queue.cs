using RabbitMQCoreClient.Configuration;
using RabbitMQCoreClient.DependencyInjection;

namespace RabbitMQCoreClient.Models;

/// <summary>
/// Simple custom message queue.
/// </summary>
public sealed class Queue : QueueBase
{
    /// <summary>
    /// Create new object of <see cref="Queue"/>.
    /// </summary>
    /// <param name="name">The queue Name.</param>
    /// <param name="durable">If true, the queue will be saved on disc.</param>
    /// <param name="exclusive">If true, then the queue will be used by single service and will be deleted after client will disconnect.
    /// Except <paramref name="useQuorum"/> is true. Then the queue will be created with be created with header <see cref="AppConstants.RabbitMQHeaders.QueueExpiresHeader"/></param>
    /// <param name="autoDelete">If true, the queue will be automatically deleted on client disconnect.</param>
    /// <param name="useQuorum">While creating the queue use parameter "x-queue-type": "quorum".</param>
    /// <exception cref="ArgumentException">name - name</exception>
    public Queue(string name, bool durable = true, bool exclusive = false, bool autoDelete = false, bool useQuorum = false)
        : base(name, durable, exclusive, autoDelete, useQuorum)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException($"{nameof(name)} is null or empty.", nameof(name));
    }

    /// <summary>
    /// Create new queue from configuration.
    /// </summary>
    /// <param name="queueConfig">Queue model from IConfiguration.</param>
    /// <returns></returns>
    public static Queue Create(QueueConfig queueConfig) =>
        new(name: queueConfig.Name,
                  durable: queueConfig.Durable,
                  exclusive: queueConfig.Exclusive,
                  autoDelete: queueConfig.AutoDelete,
                  useQuorum: queueConfig.UseQuorum)
        {
            Arguments = queueConfig.Arguments ?? new Dictionary<string, object?>(),
            DeadLetterExchange = queueConfig.DeadLetterExchange,
            UseQuorum = queueConfig.UseQuorum,
            Exchanges = queueConfig.Exchanges ?? [],
            RoutingKeys = queueConfig.RoutingKeys ?? []
        };
}
