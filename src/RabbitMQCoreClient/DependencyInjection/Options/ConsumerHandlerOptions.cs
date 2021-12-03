using Newtonsoft.Json;
using RabbitMQCoreClient.Serializers;

namespace RabbitMQCoreClient.Configuration.DependencyInjection.Options
{
    /// <summary>
    /// Consumer Handler Options.
    /// </summary>
    public class ConsumerHandlerOptions
    {
        /// <summary>
        /// Gets or sets the json serializer that overides default serializer for the following massage handler.
        /// </summary>
        public IMessageSerializer? CustomSerializer { get; set; } = default;

        /// <summary>
        /// The routing key that will mark the message on the exception handling stage.
        /// If configured, the message will return to the exchange with this key instead of <see cref="Key"/>.
        /// </summary>
        public string? RetryKey { get; set; } = default;
    }
}
