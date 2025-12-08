using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RabbitMQCoreClient.Configuration.DependencyInjection;
using RabbitMQCoreClient.Configuration.DependencyInjection.Options;
using RabbitMQCoreClient.DependencyInjection.ConfigFormats;
using System;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Class containing extension methods for creating a RabbitMQ message handler configuration interface.
/// <see cref="IRabbitMQCoreClientConsumerBuilder"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    static IRabbitMQCoreClientBuilder AddRabbitMQCoreClient(this IServiceCollection services)
    {
        var builder = new RabbitMQCoreClientBuilder(services);

        builder.AddRequiredPlatformServices();

        builder.AddDefaultSerializer();

        services.TryAddSingleton<IRabbitMQCoreClientBuilder>(builder);

        return builder;
    }

    /// <summary>
    /// Create an instance of the RabbitMQ message handler configuration class.
    /// </summary>
    /// <param name="services">List of services registered in DI.</param>
    /// <param name="configuration">Configuration section containing fields for configuring the message processing service.</param>
    /// <param name="setupAction">Use this method if you need to override the configuration.</param>
    public static IRabbitMQCoreClientBuilder AddRabbitMQCoreClient(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<RabbitMQCoreClientOptions>? setupAction = null)
    {
        RegisterOptions(services, configuration, setupAction);

        // We perform auto-tuning of queues from IConfiguration settings.
        var builder = services.AddRabbitMQCoreClient();

        // Support for the old queue registration format.
        builder.RegisterV1Configuration(configuration);
        builder.RegisterV2Configuration(configuration);

        return builder;
    }

    /// <summary>
    /// Create an instance of the RabbitMQ message handler configuration class.
    /// </summary>
    public static IRabbitMQCoreClientBuilder AddRabbitMQCoreClient(this IServiceCollection services,
        Action<RabbitMQCoreClientOptions>? setupAction)
    {
        services.Configure(setupAction);
        return services.AddRabbitMQCoreClient();
    }

    /// <summary>
    /// Create an instance of the RabbitMQ message handler configuration class.
    /// </summary>
    /// <param name="services">List of services registered in DI.</param>
    /// <param name="configuration">Configuration section containing fields for configuring the message processing service.</param>
    /// <param name="setupAction">Use this method if you need to override the configuration.</param>
    public static IRabbitMQCoreClientConsumerBuilder AddRabbitMQCoreClientConsumer(this IServiceCollection services,
        IConfiguration configuration, Action<RabbitMQCoreClientOptions>? setupAction = null)
    {
        // We perform auto-tuning of queues from IConfiguration settings.
        var builder = services.AddRabbitMQCoreClient(configuration, setupAction);

        var consumerBuilder = builder.AddConsumer();

        // Support for the old queue registration format.
        consumerBuilder.RegisterV1Configuration(configuration);
        consumerBuilder.RegisterV2Configuration(configuration);

        return consumerBuilder;
    }

    static void RegisterOptions(IServiceCollection services, IConfiguration configuration, Action<RabbitMQCoreClientOptions>? setupAction)
    {
        var instance = configuration.Get<RabbitMQCoreClientOptions>() ?? new RabbitMQCoreClientOptions();
        setupAction?.Invoke(instance);
        var options = Options.Options.Create(instance);

        services.AddSingleton((x) => options);
    }
}
