using RabbitMQCoreClient.DependencyInjection;
using RabbitMQCoreClient.Models;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Consumer builder interface.
/// </summary>
public interface IRabbitMQCoreClientConsumerBuilder
{
    /// <summary>
    /// RabbitMQCoreClientBuilder
    /// </summary>
    IRabbitMQCoreClientBuilder Builder { get; }

    /// <summary>
    /// List of services registered in DI.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// List of configured queues.
    /// </summary>
    IList<QueueBase> Queues { get; }

    /// <summary>
    /// List of registered event handlers by routing key.
    /// </summary>
    Dictionary<string, (Type Type, ConsumerHandlerOptions Options)> RoutingHandlerTypes { get; }
}
