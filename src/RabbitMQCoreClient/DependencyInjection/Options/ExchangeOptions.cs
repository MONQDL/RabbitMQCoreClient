using System.Collections.Generic;

namespace RabbitMQCoreClient.Configuration.DependencyInjection.Options
{
    /// <summary>
    /// Exchange point settings.
    /// </summary>
    public class ExchangeOptions
    {
        /// <summary>
        /// Exchange point name.
        /// </summary>
        public string Name { get; set; } = default!;

        /// <summary>
        /// Exchange point type.
        /// Possible values: "direct", "topic", "fanout", "headers"
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
        /// A set of additional settings for the exchange point.
        /// </summary>
        public IDictionary<string, object> Arguments { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Sets the Default Interchange Point value.
        /// Default: false.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the exchange point is the default point; otherwise, <c>false</c>.
        /// </value>
        public bool IsDefault { get; set; } = false;
    }
}
