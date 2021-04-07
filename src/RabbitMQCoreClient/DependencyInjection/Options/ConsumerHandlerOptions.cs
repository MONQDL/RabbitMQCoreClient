using Newtonsoft.Json;

namespace RabbitMQCoreClient.Configuration.DependencyInjection.Options
{
    /// <summary>
    /// Consumer Handler Options.
    /// </summary>
    public class ConsumerHandlerOptions
    {
        /// <summary>
        /// Gets or sets the json serializer settings. That will be used to consumer and sender as Default.
        /// Default is `new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() }`.
        /// </summary>
        public JsonSerializerSettings? JsonSerializerSettings { get; set; } = default;

        /// <summary>
        /// The routing key that will mark the message on the exception handling stage.
        /// If configured, the message will return to the exchange with this key instead of <see cref="Key"/>.
        /// </summary>
        public string? RetryKey { get; set; } = default;
    }
}
