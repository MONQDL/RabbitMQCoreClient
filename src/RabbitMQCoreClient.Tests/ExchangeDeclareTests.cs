using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQCoreClient.Configuration.DependencyInjection;
using RabbitMQCoreClient.Configuration.DependencyInjection.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace RabbitMQCoreClient.Tests;

public class ExchangeDeclareTests
{
    [Fact(DisplayName = "Checking the correct binding of options when setting up a queue v1.")]
    public void ShouldProperlyBindQueueByOptionsV1()
    {
        var services = new ServiceCollection();
        var builder = new RabbitMQCoreClientBuilder(services);
        var consumerBuilder = new RabbitMQCoreClientConsumerBuilder(builder);
        var exchange = new Exchange(new ExchangeOptions { Name = "test" });
        const string queueName = "queue1";
        const string deadLetterExchange = "testdeadletter";

        var options = new Queue(queueName, exclusive: true, durable: false)
        {
            RoutingKeys = { "r1", "r2" },
            DeadLetterExchange = deadLetterExchange,
            Exchanges = { exchange.Name }
        };

        Assert.Empty(consumerBuilder.Queues);
        consumerBuilder.AddQueue(options);

        Assert.Single(consumerBuilder.Queues);

        var firstQueue = consumerBuilder.Queues.First();

        Assert.Equal(queueName, firstQueue.Name);
        Assert.Equal(new HashSet<string> { "r1", "r2" }, firstQueue.RoutingKeys);
        Assert.Equal(deadLetterExchange, firstQueue.DeadLetterExchange);
        Assert.False(firstQueue.Durable);
        Assert.True(firstQueue.Exclusive);
    }

    [Fact(DisplayName = "Checking the correct binding of options through the configuration when setting up the queue v1.")]
    public void ShouldProperlyBindQueueByConfigurationV1()
    {
        var services = new ServiceCollection();
        var builder = new RabbitMQCoreClientBuilder(services);
        var consumerBuilder = new RabbitMQCoreClientConsumerBuilder(builder);
        const string queueName = "queue1";
        const string deadLetterExchange = "testdeadletter";

        var configurationBuilder = new ConfigurationBuilder();

        var optionsCollection = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("Queue:QueueName", queueName),
            new KeyValuePair<string, string>("Queue:RoutingKeys:1", "r1"),
            new KeyValuePair<string, string>("Queue:RoutingKeys:2", "r2"),
            new KeyValuePair<string, string>("Queue:DeadLetterExchange", deadLetterExchange),
            new KeyValuePair<string, string>("Queue:AutoDelete", "true"),
            new KeyValuePair<string, string>("Queue:Durable", "false"),
            new KeyValuePair<string, string>("Queue:Exclusive", "true")
        };

        configurationBuilder.AddInMemoryCollection(optionsCollection);

        var configuration = configurationBuilder.Build();

        Assert.Empty(consumerBuilder.Queues);
        consumerBuilder.AddQueue(configuration.GetSection("Queue"));

        Assert.Single(consumerBuilder.Queues);

        var firstQueue = consumerBuilder.Queues.First();

        Assert.Equal(queueName, firstQueue.Name);
        Assert.Equal(new HashSet<string> { "r1", "r2" }, firstQueue.RoutingKeys);
        Assert.Equal(deadLetterExchange, firstQueue.DeadLetterExchange);
        Assert.False(firstQueue.Durable);
        Assert.True(firstQueue.Exclusive);
    }

    [Fact(DisplayName = "Checking the correct binding of options through the reduced configuration when configuring the queue v1.")]
    public void ShouldProperlyBindQueueByShortConfigurationV1()
    {
        var services = new ServiceCollection();
        var builder = new RabbitMQCoreClientBuilder(services);
        var consumerBuilder = new RabbitMQCoreClientConsumerBuilder(builder);
        const string queueName = "queue1";
        const string deadLetterExchange = "testdeadletter";

        var configurationBuilder = new ConfigurationBuilder();

        var optionsCollection = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("Queue:QueueName", queueName),
            new KeyValuePair<string, string>("Queue:RoutingKeys:1", "r1"),
            new KeyValuePair<string, string>("Queue:RoutingKeys:2", "r2"),
            new KeyValuePair<string, string>("Queue:DeadLetterExchange", deadLetterExchange),
        };

        configurationBuilder.AddInMemoryCollection(optionsCollection);

        var configuration = configurationBuilder.Build();

        Assert.Empty(consumerBuilder.Queues);
        consumerBuilder.AddQueue(configuration.GetSection("Queue"));

        Assert.Single(consumerBuilder.Queues);

        var firstQueue = consumerBuilder.Queues.First();

        Assert.Equal(queueName, firstQueue.Name);
        Assert.Equal(new HashSet<string> { "r1", "r2" }, firstQueue.RoutingKeys);
        Assert.Equal(deadLetterExchange, firstQueue.DeadLetterExchange);
        Assert.True(firstQueue.Durable);
        Assert.False(firstQueue.Exclusive);
        Assert.False(firstQueue.AutoDelete);
    }

    [Fact(DisplayName = "Checking the correct binding of options when setting up a subscription v1.")]
    public void ShouldProperlyBindSubscriptionByOptionsV1()
    {
        var services = new ServiceCollection();
        var builder = new RabbitMQCoreClientBuilder(services);
        var consumerBuilder = new RabbitMQCoreClientConsumerBuilder(builder);
        var exchange = new Exchange(new ExchangeOptions { Name = "test" });
        const string deadLetterExchange = "testdeadletter";

        var options = new Subscription()
        {
            RoutingKeys = { "r1", "r2" },
            DeadLetterExchange = deadLetterExchange,
            Exchanges = { exchange.Name }
        };

        Assert.Empty(consumerBuilder.Queues);
        consumerBuilder.AddSubscription(options);

        Assert.Single(consumerBuilder.Queues);

        var firstQueue = consumerBuilder.Queues.First();

        Assert.True(Guid.TryParse(firstQueue.Name, out var _));
        Assert.Equal(new HashSet<string> { "r1", "r2" }, firstQueue.RoutingKeys);
        Assert.Equal(deadLetterExchange, firstQueue.DeadLetterExchange);
        Assert.True(firstQueue.AutoDelete);
        Assert.False(firstQueue.Durable);
        Assert.True(firstQueue.Exclusive);
    }

    [Fact(DisplayName = "Checking the correct binding of options through the configuration when setting up a subscription v1.")]
    public void ShouldProperlyBindSubscriptionByConfigurationV1()
    {
        var services = new ServiceCollection();
        var builder = new RabbitMQCoreClientBuilder(services);
        var consumerBuilder = new RabbitMQCoreClientConsumerBuilder(builder);
        const string deadLetterExchange = "testdeadletter";

        var configurationBuilder = new ConfigurationBuilder();

        var optionsCollection = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("Subscription:RoutingKeys:1", "r1"),
            new KeyValuePair<string, string>("Subscription:RoutingKeys:2", "r2"),
            new KeyValuePair<string, string>("Subscription:DeadLetterExchange", deadLetterExchange),
            // Verify that the configuration does not change the default options for the subscription.
            new KeyValuePair<string, string>("Subscription:AutoDelete", "false"),
            new KeyValuePair<string, string>("Subscription:Durable", "true"),
            new KeyValuePair<string, string>("Subscription:Exclusive", "false")
        };

        configurationBuilder.AddInMemoryCollection(optionsCollection);

        var configuration = configurationBuilder.Build();

        Assert.Empty(consumerBuilder.Queues);
        consumerBuilder.AddSubscription(configuration.GetSection("Subscription"));

        Assert.Single(consumerBuilder.Queues);

        var firstQueue = consumerBuilder.Queues.First();

        Assert.True(Guid.TryParse(firstQueue.Name, out var _));
        Assert.Equal(new HashSet<string> { "r1", "r2" }, firstQueue.RoutingKeys);
        Assert.Equal(deadLetterExchange, firstQueue.DeadLetterExchange);
        Assert.True(firstQueue.AutoDelete);
        Assert.False(firstQueue.Durable);
        Assert.True(firstQueue.Exclusive);
    }
}
