using RabbitMQCoreClient.Models;
using RabbitMQCoreClient.Serializers;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// RabbitMQCoreClient builder.
/// </summary>
public interface IRabbitMQCoreClientBuilder
{
    /// <summary>
    /// List of services registered in DI.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// List of configured exchange points.
    /// </summary>
    List<Exchange> Exchanges { get; }

    /// <summary>
    /// Gets the default exchange.
    /// </summary>
    Exchange? DefaultExchange { get; }

    /// <summary>
    /// The default message serializer.
    /// </summary>
    IMessageSerializer? Serializer { get; set; }
}
