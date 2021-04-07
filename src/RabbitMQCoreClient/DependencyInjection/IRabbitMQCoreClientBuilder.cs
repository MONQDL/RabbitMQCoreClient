using RabbitMQCoreClient.Configuration.DependencyInjection;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    public interface IRabbitMQCoreClientBuilder
    {
        /// <summary>
        /// Список сервисов, зарегистрированных в DI.
        /// </summary>
        IServiceCollection Services { get; }

        /// <summary>
        /// Список сконфигурированных точек обмена.
        /// </summary>
        IList<Exchange> Exchanges { get; }

        /// <summary>
        /// Gets the default exchange.
        /// </summary>
        Exchange? DefaultExchange { get; }
    }
}
