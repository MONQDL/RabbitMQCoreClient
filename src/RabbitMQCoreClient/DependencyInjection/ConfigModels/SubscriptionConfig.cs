using System.Collections.Generic;

namespace RabbitMQCoreClient.DependencyInjection.ConfigModels;

/// <summary>
/// Message queue for subscribing to events. 
/// The queue is automatically named. 
/// When the client disconnects from the server, the queue is automatically deleted.
/// </summary>
public class SubscriptionConfig
{
    /// <summary>
    /// The name of the exchange point that will receive messages for which a reject or nack was received.
    /// </summary>
    public string? DeadLetterExchange { get; set; }

    /// <summary>
    /// While creating the queue use parameter "x-queue-type": "quorum".
    /// </summary>
    public bool UseQuorum { get; set; } = false;

    /// <summary>
    /// List of additional parameters that will be used when initializing the queue.
    /// </summary>
    public IDictionary<string, object?> Arguments { get; set; } = new Dictionary<string, object?>();

    /// <summary>
    /// List of routing keys for the queue.
    /// </summary>
    public HashSet<string> RoutingKeys { get; set; } = new HashSet<string>();

    /// <summary>
    /// The list of exchange points to which the queue is bound.
    /// </summary>
    public HashSet<string> Exchanges { get; set; } = new HashSet<string>();
}
