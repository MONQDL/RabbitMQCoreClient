using System.Collections.Generic;

namespace RabbitMQCoreClient.Configuration.DependencyInjection.Options
{
    /// <summary>
    /// Настройки точки обмена
    /// </summary>
    public class ExchangeOptions
    {
        /// <summary>
        /// Название точки обмена.
        /// </summary>
        public string Name { get; set; } = default!;

        /// <summary>
        /// Тип точки обмена.
        /// Возможные значения: "direct", "topic", "fanout", "headers"
        /// </summary>
        public string Type { get; set; } = "direct";

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ExchangeOptions"/> is durable.
        /// </summary>
        /// <value>
        ///   <c>true</c> if durable; otherwise, <c>false</c>.
        /// </value>
        public bool Durable { get; set; } = true;

        /// <summary>
        /// If yes, the exchange will delete itself after at least one queue or exchange
        /// has been bound to this one, and then all queues or exchanges have been unbound.
        /// </summary>
        public bool AutoDelete { get; set; } = false;

        /// <summary>
        /// Набор дополнительных настроек точки обмена.
        /// </summary>
        public IDictionary<string, object> Arguments { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Устанавливает значение "Точка обмена по умолчанию".
        /// По умолчанию: false.
        /// </summary>
        /// <value>
        ///   <c>true</c>, если точка обмена является точкой по умолчанию; иначе, <c>false</c>.
        /// </value>
        public bool IsDefault { get; set; } = false;
    }
}
