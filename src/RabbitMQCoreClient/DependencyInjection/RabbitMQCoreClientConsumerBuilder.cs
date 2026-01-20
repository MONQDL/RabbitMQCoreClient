using Microsoft.Extensions.DependencyInjection;
using RabbitMQCoreClient.DependencyInjection;
using RabbitMQCoreClient.Models;

namespace RabbitMQCoreClient.Configuration.DependencyInjection;

/// <summary>
/// RabbitMQCoreClient consumer builder.
/// </summary>
public sealed class RabbitMQCoreClientConsumerBuilder : IRabbitMQCoreClientConsumerBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMQCoreClientConsumerBuilder" /> class.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <exception cref="ArgumentNullException">services</exception>
    public RabbitMQCoreClientConsumerBuilder(IRabbitMQCoreClientBuilder builder)
    {
        Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        Services = builder.Services ?? throw new ArgumentException($"{nameof(builder.Services)} is null");
    }

    /// <inheritdoc />
    public IRabbitMQCoreClientBuilder Builder { get; }

    /// <inheritdoc />
    public IServiceCollection Services { get; }

    /// <inheritdoc />
    public IList<QueueBase> Queues { get; } = [];

    /// <inheritdoc />
    public Dictionary<string, (Type Type, ConsumerHandlerOptions Options)> RoutingHandlerTypes { get; } =
        [];
}
