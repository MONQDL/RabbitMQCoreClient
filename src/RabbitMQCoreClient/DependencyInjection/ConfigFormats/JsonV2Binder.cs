using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQCoreClient.Configuration.DependencyInjection.Options;
using RabbitMQCoreClient.DependencyInjection.ConfigModels;
using RabbitMQCoreClient.Exceptions;
using System;
using System.Linq;

namespace RabbitMQCoreClient.DependencyInjection.ConfigFormats
{
    public static class JsonV2Binder
    {
        const string ExhangesSection = "Exchanges";
        const string QueuesSection = "Queues";
        const string SubscriptionsSection = "Subscriptions";

        public static IRabbitMQCoreClientBuilder RegisterV2Configuration(this IRabbitMQCoreClientBuilder builder, IConfiguration configuration)
        {
            if (configuration is null)
                return builder;

            // Регистрируем точки обмена.
            RegisterExchanges(builder, configuration);

            return builder;
        }

        static void RegisterExchanges(IRabbitMQCoreClientBuilder builder, IConfiguration configuration)
        {
            var exchanges = configuration.GetSection(ExhangesSection);
            foreach (var exchangeConfig in exchanges.GetChildren())
            {
                var options = new ExchangeOptions();
                exchangeConfig.Bind(options);
                builder.AddExchange(options.Name, options: options);
            }
        }

        public static IRabbitMQCoreClientConsumerBuilder RegisterV2Configuration(this IRabbitMQCoreClientConsumerBuilder builder,
            IConfiguration configuration)
        {
            if (configuration is null)
                return builder;

            // Регистрируем очереди и привязываем их к точкам обмена.
            RegisterQueues<QueueConfig, Queue>(builder,
                configuration.GetSection(QueuesSection),
                (qConfig) => Queue.Create(qConfig));

            // Регистрируем подписки и привязываем их к точкам обмена.
            RegisterQueues<SubscriptionConfig, Subscription>(builder,
                configuration.GetSection(SubscriptionsSection),
                (qConfig) => Subscription.Create(qConfig));

            return builder;
        }

        static void RegisterQueues<TConfig, TQueue>(IRabbitMQCoreClientConsumerBuilder builder,
            IConfigurationSection? queuesConfiguration,
            Func<TConfig, TQueue> createQueue)
            where TConfig : new()
            where TQueue : QueueBase
        {
            if (queuesConfiguration is null)
                return;

            foreach (var queueConfig in queuesConfiguration.GetChildren())
            {
                var q = BindConfig<TConfig>(queueConfig);

                var queue = createQueue(q);

                RegisterQueue(builder, queue);
            }
        }

        static void RegisterQueue<TQueue>(IRabbitMQCoreClientConsumerBuilder builder, TQueue queue)
            where TQueue : QueueBase
        {
            if (!queue.Exchanges.Any())
            {
                var defaultExchange = builder.Builder.DefaultExchange;
                if (defaultExchange is null)
                    throw new QueueBindException($"Queue {queue.Name} has no configured exchanges and the Default Exchange not found.");
                queue.Exchanges.Add(defaultExchange.Name);
            }
            else
            {
                // Checking echange points declared in queue in configured "Exchanges".
                foreach (var exchangeName in queue.Exchanges)
                {
                    var exchange = builder.Builder.Exchanges.FirstOrDefault(x => x.Name == exchangeName);
                    if (exchange is null)
                        throw new ClientConfigurationException($"The exchange {exchangeName} configured in queue {queue.Name} " +
                            $"not found in Exchanges section.");
                }
            }

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
            // Так себе решение, но зато без дубляжа
            switch (queue)
            {
                case Queue q: builder.AddQueue(q); break;
                case Subscription q: builder.AddSubscription(q); break;
            }
        }
    }
}
