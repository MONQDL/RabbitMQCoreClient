using RabbitMQCoreClient.Configuration.DependencyInjection;
using RabbitMQCoreClient.Serializers;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection;

public interface IRabbitMQCoreClientBuilder
{
    /// <summary>
    /// List of services registered in DI.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// List of configured exchange points.
    /// </summary>
    IList<Exchange> Exchanges { get; }

    /// <summary>
    /// Gets the default exchange.
    /// </summary>
    Exchange? DefaultExchange { get; }

    /// <summary>
    /// The default JSON serializer.
    /// </summary>
    IMessageSerializer Serializer { get; set; }
}
