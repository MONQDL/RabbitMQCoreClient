using Microsoft.Extensions.DependencyInjection;
using RabbitMQCoreClient.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RabbitMQCoreClient.Configuration.DependencyInjection;

public sealed class RabbitMQCoreClientBuilder : IRabbitMQCoreClientBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMQCoreClientConsumerBuilder"/> class.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <exception cref="ArgumentNullException">services</exception>
    public RabbitMQCoreClientBuilder(IServiceCollection services) =>
        Services = services ?? throw new ArgumentNullException(nameof(services));

    /// <inheritdoc />
    public IServiceCollection Services { get; }

    /// <inheritdoc />
    public IList<Exchange> Exchanges { get; } = new List<Exchange>();

    /// <inheritdoc />
    public Exchange? DefaultExchange => Exchanges.FirstOrDefault(x => x.Options.IsDefault);

    /// <inheritdoc />
    public IMessageSerializer Serializer { get; set; }
}
