using RabbitMQCoreClient.Configuration.DependencyInjection.Options;
using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    public interface IRabbitMQCoreClientConsumerBuilder
    {
        /// <summary>
        /// RabbitMQCoreClientBuilder
        /// </summary>
        IRabbitMQCoreClientBuilder Builder { get; }

        /// <summary>
        /// Список сервисов, зарегистрированных в DI.
        /// </summary>
        IServiceCollection Services { get; }

        /// <summary>
        /// Список сконфигурированных очередей.
        /// </summary>
        IList<QueueBase> Queues { get; }

        /// <summary>
        /// Список зарегистрированных обработчиков событий по ключу маршрутизации.
        /// </summary>
        Dictionary<string, (Type Type, ConsumerHandlerOptions Options)> RoutingHandlerTypes { get; }
    }
}
