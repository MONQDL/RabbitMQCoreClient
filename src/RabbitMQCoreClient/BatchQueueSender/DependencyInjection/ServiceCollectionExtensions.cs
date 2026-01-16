using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQCoreClient.BatchQueueSender;

namespace RabbitMQCoreClient.DependencyInjection;

/// <summary>
/// Class containing extension methods for registering the BatchQueueSender services at DI.
/// </summary>
public static partial class ServiceCollectionExtensions
{
    static IServiceCollection AddBatchQueueSenderCore(this IServiceCollection services)
    {
        services.TryAddTransient<IEventsWriter, EventsWriter>();

        services.AddSingleton<IQueueEventsBufferEngine, QueueEventsBufferEngine>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<QueueBatchSenderOptions>>();

            return new QueueEventsBufferEngine(sp.GetRequiredService<IEventsWriter>(),
                options?.Value?.EventsFlushCount ?? 10000,
                TimeSpan.FromSeconds(options?.Value?.EventsFlushPeriodSec ?? 2),
                sp.GetService<IEventsHandler>(),
                sp.GetRequiredService<IRabbitMQCoreClientBuilder>(),
                sp.GetService<ILogger<QueueEventsBufferEngine>>());
        });
        services.TryAddSingleton<IQueueEventsBufferEngine, QueueEventsBufferEngine>();
        return services;
    }

    /// <summary>
    /// Registers an instance of the <see cref="IQueueEventsBufferEngine"/> interface in the DI.
    /// Injected service <see cref="IQueueEventsBufferEngine"/> can be used to send messages
    /// to the inmemory queue and process them at the separate thread.
    /// </summary>
    /// <param name="builder">The RabbitMQ Client builder.</param>
    /// <param name="configuration">Configuration section containing fields for configuring the message processing service.</param>
    /// <param name="setupAction">Use this method if you need to override the configuration.</param>
    public static IRabbitMQCoreClientBuilder AddBatchQueueSender(
        this IRabbitMQCoreClientBuilder builder,
        IConfiguration configuration,
        Action<QueueBatchSenderOptions>? setupAction = null)
    {
        RegisterOptions(builder.Services, configuration, setupAction);

        builder.Services.AddBatchQueueSenderCore();
        return builder;
    }

    /// <summary>
    /// Registers an instance of the <see cref="IQueueEventsBufferEngine"/> interface in the DI.
    /// Injected service <see cref="IQueueEventsBufferEngine"/> can be used to send messages
    /// to the inmemory queue and process them at the separate thread.
    /// </summary>
    /// <param name="builder">The RabbitMQ Client builder.</param>
    /// <param name="setupAction">Use this method if you need to override the configuration.</param>
    /// <returns></returns>
    public static IRabbitMQCoreClientBuilder AddBatchQueueSender(this IRabbitMQCoreClientBuilder builder,
        Action<QueueBatchSenderOptions>? setupAction)
    {
        if (setupAction != null)
            builder.Services.Configure(setupAction);
        builder.Services.AddBatchQueueSenderCore();

        return builder;
    }

    /// <summary>
    /// Registers an instance of the <see cref="IQueueEventsBufferEngine"/> interface in the DI.
    /// Injected service <see cref="IQueueEventsBufferEngine"/> can be used to send messages
    /// to the inmemory queue and process them at the separate thread.
    /// </summary>
    /// <param name="builder">List of services registered in DI.</param>
    /// <returns></returns>
    public static IRabbitMQCoreClientBuilder AddBatchQueueSender(this IRabbitMQCoreClientBuilder builder)
    {
        builder.Services.TryAddSingleton((x) => Options.Create(new QueueBatchSenderOptions()));
        builder.Services.AddBatchQueueSenderCore();

        return builder;
    }

    static void RegisterOptions(IServiceCollection services,
        IConfiguration configuration,
        Action<QueueBatchSenderOptions>? setupAction)
    {
        var instance = configuration.Get<QueueBatchSenderOptions>() ?? new QueueBatchSenderOptions();
        setupAction?.Invoke(instance);
        var options = Options.Create(instance);

        services.TryAddSingleton((x) => options);
    }
}
