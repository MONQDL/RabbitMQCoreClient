using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQCoreClient.Exceptions;
using RabbitMQCoreClient.Models;

namespace RabbitMQCoreClient.DependencyInjection.ConfigFormats;

public static class JsonV1Binder
{
    const string QueueSection = "Queue";
    const string QueueName = "Queue:QueueName";
    const string ExchangeName = "Exchange:Name";
    const string SubscriptionSection = "Subscription";

    public static IRabbitMQCoreClientBuilder RegisterV1Configuration(this IRabbitMQCoreClientBuilder builder,
        IConfiguration? configuration)
    {
        if (configuration is null)
            return builder;

        // The exchange point will be the default point.
        var oldExchangeName = configuration[ExchangeName];
        if (!string.IsNullOrEmpty(oldExchangeName))
            builder.AddExchange(oldExchangeName, options: new ExchangeOptions { Name = oldExchangeName, IsDefault = true });

        return builder;
    }

    public static IRabbitMQCoreClientConsumerBuilder RegisterV1Configuration(this IRabbitMQCoreClientConsumerBuilder builder,
        IConfiguration? configuration)
    {
        if (configuration is null)
            return builder;

        // Try to detect old configuration format.
        // The exchange point will be the default point.
        var oldExchangeName = configuration[ExchangeName];
        if (string.IsNullOrEmpty(oldExchangeName))
            return builder;

        // Old queue format detected.
        var exchange = builder.Builder.Exchanges.FirstOrDefault(x => x.Name == oldExchangeName)
            ?? throw new ClientConfigurationException($"The exchange {oldExchangeName} is " +
                "not found in \"Exchange\" section.");
        if (configuration.GetSection(QueueSection).Exists())
        {
            // Register a queue and bind it to exchange points.
            var queueName = configuration[QueueName];
            if (!string.IsNullOrEmpty(queueName))
                RegisterQueue<QueueConfig, Queue>(builder,
                    configuration.GetSection(QueueSection),
                    exchange,
                    (qConfig) => Queue.Create(qConfig));
        }

        if (configuration.GetSection(SubscriptionSection).Exists())
        {
            // Register a subscription and link it to exchange points.
            RegisterQueue<SubscriptionConfig, Subscription>(builder,
                configuration.GetSection(SubscriptionSection),
                exchange,
                (qConfig) => Subscription.Create(qConfig));
        }

        return builder;
    }

    static void RegisterQueue<TConfig, TQueue>(IRabbitMQCoreClientConsumerBuilder builder,
        IConfigurationSection? queueConfig,
        Exchange exchange,
        Func<TConfig, TQueue> createQueue)
        where TConfig : new()
        where TQueue : QueueBase
    {
        if (queueConfig is null)
            return;

        var q = BindConfig<TConfig>(queueConfig);

        var queue = createQueue(q);
        queue.Exchanges.Add(exchange.Name);

        AddQueue(builder, queue);
    }

    static TConfig BindConfig<TConfig>(IConfigurationSection queueConfig)
        where TConfig : new()
    {
        var q = new TConfig();
        queueConfig.Bind(q);
        return q;
    }

    static void AddQueue<T>(IRabbitMQCoreClientConsumerBuilder builder, T queue)
        where T : QueueBase
    {
        // So-so solution, but without dubbing.
        switch (queue)
        {
            case Queue q: builder.AddQueue(q); break;
            case Subscription q: builder.AddSubscription(q); break;
        }
    }
}
