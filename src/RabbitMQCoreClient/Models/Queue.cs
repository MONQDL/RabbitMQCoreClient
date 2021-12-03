using RabbitMQCoreClient.DependencyInjection.ConfigModels;
using System;
using System.Collections.Generic;

namespace RabbitMQCoreClient.Configuration.DependencyInjection.Options
{
    /// <summary>
    /// Simple custom message queue.
    /// </summary>
    public sealed class Queue : QueueBase
    {
        public Queue(string name, bool durable = true, bool exclusive = false, bool autoDelete = false)
            : base(name, durable, exclusive, autoDelete)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException($"{nameof(name)} is null or empty.", nameof(name));
        }

        public static Queue Create(QueueConfig queueConfig)
        {
            return new Queue(name: queueConfig.Name,
                       durable: queueConfig.Durable,
                       exclusive: queueConfig.Exclusive,
                       autoDelete: queueConfig.AutoDelete)
            {
                Arguments = queueConfig.Arguments ?? new Dictionary<string, object>(),
                DeadLetterExchange = queueConfig.DeadLetterExchange,
                Exchanges = queueConfig.Exchanges ?? new HashSet<string>(),
                RoutingKeys = queueConfig.RoutingKeys ?? new HashSet<string>()
            };
        }
    }
}
