using RabbitMQCoreClient.DependencyInjection.ConfigModels;
using System.Collections.Generic;

namespace RabbitMQCoreClient.Configuration.DependencyInjection.Options
{
    /// <summary>
    /// Message queue for subscribing to events. 
    /// The queue is automatically named. When the client disconnects from the server, the queue is automatically deleted.
    /// </summary>
    public sealed class Subscription : QueueBase
    {
        public Subscription()
            : base(null, false, true, true)
        { }

        public static Subscription Create(SubscriptionConfig queueConfig)
        {
            return new Subscription
            {
                Arguments = queueConfig.Arguments ?? new Dictionary<string, object>(),
                DeadLetterExchange = queueConfig.DeadLetterExchange,
                Exchanges = queueConfig.Exchanges ?? new HashSet<string>(),
                RoutingKeys = queueConfig.RoutingKeys ?? new HashSet<string>()
            };
        }
    }
}
