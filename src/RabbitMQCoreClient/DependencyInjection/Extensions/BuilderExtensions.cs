using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RabbitMQCoreClient;
using RabbitMQCoreClient.Configuration.DependencyInjection;
using RabbitMQCoreClient.Configuration.DependencyInjection.Options;
using RabbitMQCoreClient.DependencyInjection;
using RabbitMQCoreClient.DependencyInjection.ConfigModels;
using RabbitMQCoreClient.Exceptions;

namespace Microsoft.Extensions.DependencyInjection;

public static class BuilderExtensions
{
    /// <summary>
    /// Adds the required platform services.
    /// </summary>
    /// <param name="builder">The builder.</param>
    public static IRabbitMQCoreClientBuilder AddRequiredPlatformServices(this IRabbitMQCoreClientBuilder builder)
    {
        builder.Services.TryAddSingleton<IQueueService, QueueService>();

        builder.Services.AddHostedService<RabbitMQHostedService>();

        builder.Services.AddOptions();
        builder.Services.AddLogging();

        builder.Services.TryAddSingleton(
            resolver => resolver.GetRequiredService<IOptions<RabbitMQCoreClientOptions>>().Value);

        return builder;
    }

    /// <summary>
    /// Use default serializer as NewtonsoftJson.
    /// </summary>
    public static IRabbitMQCoreClientBuilder AddDefaultSerializer(this IRabbitMQCoreClientBuilder builder) =>
        builder.AddSystemTextJson();

    /// <summary>
    /// Add exchange connection to RabbitMQ.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="exchangeName">Name of the exchange.</param>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentException">The exchange with same name was added earlier.</exception>
    public static IRabbitMQCoreClientBuilder AddExchange(
        this IRabbitMQCoreClientBuilder builder,
        string exchangeName,
        ExchangeOptions? options = default)
    {
        if (builder.Exchanges.Any(x => x.Name == exchangeName))
            throw new ArgumentException("The exchange with same name was added earlier.");

        var exchange = new Exchange(options ?? new ExchangeOptions { Name = exchangeName });
        builder.Exchanges.Add(exchange);

        return builder;
    }

    /// <summary>
    /// Adds the consumer builder.
    /// </summary>
    /// <param name="builder">The rabbit MQ builder.</param>
    public static IRabbitMQCoreClientConsumerBuilder AddConsumer(
        this IRabbitMQCoreClientBuilder builder)
    {
        var consumerBuilder = new RabbitMQCoreClientConsumerBuilder(builder);

        builder.Services.TryAddSingleton<IRabbitMQCoreClientConsumerBuilder>(consumerBuilder);
        builder.Services.TryAddSingleton<IQueueConsumer, RabbitMQCoreClientConsumer>();

        return consumerBuilder;
    }

    /// <summary>
    /// Add default queue to RabbitMQ consumer.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="queueName">Name of the queue.</param>
    /// <param name="exchangeName">Name of the exchange. If null - will try to bind to default exchange.</param>
    /// <exception cref="ArgumentException">The exchange with same name was added earlier.</exception>
    public static IRabbitMQCoreClientConsumerBuilder AddQueue(
        this IRabbitMQCoreClientConsumerBuilder builder,
        string queueName, string? exchangeName = default)
    {
        var queue = new Queue(queueName);
        if (!string.IsNullOrEmpty(exchangeName))
            queue.Exchanges.Add(exchangeName);

        builder.AddQueue(queue);

        return builder;
    }

    /// <summary>
    /// Add default queue to RabbitMQ consumer.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="queueConfig">The queue configuration.</param>
    /// <exception cref="ArgumentNullException">queue</exception> 
    /// <exception cref="ArgumentException">The exchange with same name was added earlier.</exception>
    public static IRabbitMQCoreClientConsumerBuilder AddQueue(
        this IRabbitMQCoreClientConsumerBuilder builder,
        IConfiguration queueConfig)
    {
        if (queueConfig is null)
            throw new ArgumentNullException(nameof(queueConfig), $"{nameof(queueConfig)} is null.");

        var q = new QueueConfig();
        queueConfig.Bind(q);

        var queue = Queue.Create(q);

        builder.AddQueue(queue);

        return builder;
    }

    /// <summary>
    /// Add default queue to RabbitMQ consumer.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="queue">The queue.</param>
    /// <exception cref="ArgumentNullException">queue</exception>
    /// <exception cref="ArgumentException">The exchange with same name was added earlier.</exception>
    public static IRabbitMQCoreClientConsumerBuilder AddQueue(
        this IRabbitMQCoreClientConsumerBuilder builder,
        Queue queue)
    {
        if (queue is null)
            throw new ArgumentNullException(nameof(queue), $"{nameof(queue)} is null.");

        if (builder.Queues.Any(x => (x is Queue) && x.Name == queue.Name))
            throw new ArgumentException("The queue with same name was added earlier.");

        builder.Queues.Add(queue);

        return builder;
    }

    /// <summary>
    /// Add subscription queue to RabbitMQ consumer.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="subscriptionConfig">The queue configuration.</param>
    /// <exception cref="ArgumentNullException">queue</exception>
    /// <exception cref="ArgumentException">The exchange with same name was added earlier.</exception>
    public static IRabbitMQCoreClientConsumerBuilder AddSubscription(
        this IRabbitMQCoreClientConsumerBuilder builder,
        IConfiguration subscriptionConfig)
    {
        if (subscriptionConfig is null)
            throw new ArgumentNullException(nameof(subscriptionConfig), $"{nameof(subscriptionConfig)} is null.");

        var s = new SubscriptionConfig();
        subscriptionConfig.Bind(s);

        var subscription = Subscription.Create(s);

        builder.AddSubscription(subscription);

        return builder;
    }

    /// <summary>
    /// Add subscription queue to RabbitMQ consumer.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="exchangeName">Name of the exchange. If null - will try to bind to default exchange.</param>
    /// <exception cref="ArgumentException">The exchange with same name was added earlier.</exception>
    public static IRabbitMQCoreClientConsumerBuilder AddSubscription(
        this IRabbitMQCoreClientConsumerBuilder builder,
        string? exchangeName = default)
    {
        var subscription = new Subscription();
        if (!string.IsNullOrEmpty(exchangeName))
            subscription.Exchanges.Add(exchangeName);

        builder.AddSubscription(subscription);

        return builder;
    }

    /// <summary>
    /// Add subscription queue to RabbitMQ consumer.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="subscription">The subscription queue.</param>
    /// <exception cref="ArgumentException">The exchange with same name was added earlier.</exception>
    public static IRabbitMQCoreClientConsumerBuilder AddSubscription(
        this IRabbitMQCoreClientConsumerBuilder builder,
        Subscription subscription)
    {
        if (subscription is null)
            throw new ArgumentNullException(nameof(subscription), $"{nameof(subscription)} is null.");

        builder.Queues.Add(subscription);

        return builder;
    }

    /// <summary>
    /// Add the message handler that will be trigger on messages with desired routing keys.
    /// </summary>
    /// <typeparam name="TMessageHandler">RabbitMQ Handler type.</typeparam>
    /// <param name="builder">IRabbitMQCoreClientConsumerBuilder instance.</param>
    /// <param name="routingKeys">Routing keys bound to the queue.</param>
    /// <exception cref="ClientConfigurationException">The handler needs to set at least one routing key.</exception>
    public static IRabbitMQCoreClientConsumerBuilder AddHandler<TMessageHandler>(this IRabbitMQCoreClientConsumerBuilder builder,
        params string[] routingKeys)
        where TMessageHandler : class, IMessageHandler
    {
        CheckRoutingKeysParam(routingKeys);

        AddHandlerToBuilder(builder, typeof(TMessageHandler), default!, routingKeys);
        builder.Services.TryAddTransient<TMessageHandler>();
        return builder;
    }

    /// <summary>
    /// Add the message handler that will be trigger on messages with desired routing keys.
    /// </summary>
    /// <typeparam name="TMessageHandler">RabbitMQ Handler type.</typeparam>
    /// <param name="builder">IRabbitMQCoreClientConsumerBuilder instance.</param>
    /// <param name="options">The options that can change consumer handler default behavior.</param>
    /// <param name="routingKeys">Routing keys bound to the queue.</param>
    public static IRabbitMQCoreClientConsumerBuilder AddHandler<TMessageHandler>(this IRabbitMQCoreClientConsumerBuilder builder,
            IList<string> routingKeys,
            ConsumerHandlerOptions? options = default)
        where TMessageHandler : class, IMessageHandler
    {
        CheckRoutingKeysParam(routingKeys);

        AddHandlerToBuilder(builder, typeof(TMessageHandler), options ?? new ConsumerHandlerOptions(), routingKeys);
        builder.Services.TryAddTransient<TMessageHandler>();
        return builder;
    }

    static void AddHandlerToBuilder(IRabbitMQCoreClientConsumerBuilder builder,
        Type handlerType,
        ConsumerHandlerOptions options,
        IList<string> routingKeys)
    {
        foreach (var routingKey in routingKeys)
        {
            if (builder.RoutingHandlerTypes.TryGetValue(routingKey, out var result))
                throw new ClientConfigurationException("The routing key is already being processed by a handler like " +
                    $"{result.Type.FullName}.");

            builder.RoutingHandlerTypes.Add(routingKey, (handlerType, options));
        }
    }

    static void CheckRoutingKeysParam(IEnumerable<string> routingKeys)
    {
        if (routingKeys is null || !routingKeys.Any())
            throw new ClientConfigurationException("The handler needs to set at least one routing key.");
    }
}
