using RabbitMQ.Client;
using RabbitMQCoreClient.Configuration.DependencyInjection.Options;
using System;

namespace RabbitMQCoreClient.Configuration.DependencyInjection
{
    /// <summary>
    /// The RabbitMQ Exchange
    /// </summary>
    public class Exchange
    {
        /// <summary>
        /// Название точки обмена.
        /// </summary>
        public string Name => Options.Name;

        /// <summary>
        /// Конфигурационные настройки точки обмена.
        /// </summary>
        public ExchangeOptions Options { get; } = new ExchangeOptions();

        /// <summary>
        /// Initializes a new instance of the <see cref="Exchange" /> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException">options</exception>
        /// <exception cref="ArgumentException">exchangeName
        /// or
        /// services</exception>
        public Exchange(ExchangeOptions options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options), $"{nameof(options)} is null.");

            if (string.IsNullOrEmpty(options.Name))
                throw new ArgumentException($"{nameof(options.Name)} is null or empty.", nameof(options.Name));
        }

        /// <summary>
        /// Starts the exchange.
        /// </summary>
        /// <param name="_channel">The channel.</param>
        public void StartExchange(IModel _channel) => _channel.ExchangeDeclare(
                exchange: Name,
                type: Options.Type,
                durable: Options.Durable,
                autoDelete: Options.AutoDelete,
                arguments: Options.Arguments
                );
    }
}
