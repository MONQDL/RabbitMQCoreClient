using RabbitMQCoreClient.DependencyInjection.ConfigModels;

namespace RabbitMQCoreClient.Configuration.DependencyInjection.Options;

/// <summary>
/// Message queue for subscribing to events. 
/// The queue is automatically named. When the client disconnects from the server, the queue is automatically deleted.
/// </summary>
public sealed class Subscription : QueueBase
{
    /// <summary>
    /// Create new object of <see cref="Subscription"/>.
    /// </summary>
    /// <param name="useQuorum">If true - than the queue with <see cref="AppConstants.RabbitMQHeaders.QueueExpiresHeader"/> header 
    /// will be created otherwise the autodelete queue will be created.</param>
    public Subscription(bool useQuorum = false)
        : base($"sub_{Guid.NewGuid()}", false, true, true, useQuorum)
    { }

    /// <summary>
    /// Create new subscription from configuration.
    /// </summary>
    /// <param name="queueConfig"></param>
    /// <returns></returns>
    public static Subscription Create(SubscriptionConfig queueConfig) => new()
    {
        Arguments = queueConfig.Arguments ?? new Dictionary<string, object?>(),
        DeadLetterExchange = queueConfig.DeadLetterExchange,
        UseQuorum = queueConfig.UseQuorum,
        Exchanges = queueConfig.Exchanges ?? [],
        RoutingKeys = queueConfig.RoutingKeys ?? []
    };
}
