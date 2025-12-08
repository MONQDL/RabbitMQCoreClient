using RabbitMQCoreClient.DependencyInjection.ConfigModels;
using System;
using System.Collections.Generic;

namespace RabbitMQCoreClient.Configuration.DependencyInjection.Options;

/// <summary>
/// Simple custom message queue.
/// </summary>
public sealed class Queue : QueueBase
{
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
        new Queue(name: queueConfig.Name,
                  durable: queueConfig.Durable,
                  exclusive: queueConfig.Exclusive,
                  autoDelete: queueConfig.AutoDelete,
                  useQuorum: queueConfig.UseQuorum)
        {
            Arguments = queueConfig.Arguments ?? new Dictionary<string, object?>(),
            DeadLetterExchange = queueConfig.DeadLetterExchange,
            UseQuorum = queueConfig.UseQuorum,
            Exchanges = queueConfig.Exchanges ?? new HashSet<string>(),
            RoutingKeys = queueConfig.RoutingKeys ?? new HashSet<string>()
        };
}
