using RabbitMQCoreClient.DependencyInjection.ConfigModels;

namespace RabbitMQCoreClient.Configuration.DependencyInjection.Options;

/// <summary>
/// Message queue for subscribing to events. 
/// The queue is automatically named. When the client disconnects from the server, the queue is automatically deleted.
/// </summary>
public sealed class Subscription : QueueBase
{
    public Subscription(bool useQuorum = false)
        : base($"sub_{Guid.NewGuid()}", false, true, true, useQuorum)
    { }

    public static Subscription Create(SubscriptionConfig queueConfig) => new()
    {
        Arguments = queueConfig.Arguments ?? new Dictionary<string, object?>(),
        DeadLetterExchange = queueConfig.DeadLetterExchange,
        UseQuorum = queueConfig.UseQuorum,
        Exchanges = queueConfig.Exchanges ?? [],
        RoutingKeys = queueConfig.RoutingKeys ?? []
    };
}
