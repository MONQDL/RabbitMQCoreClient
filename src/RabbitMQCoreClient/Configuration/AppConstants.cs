using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace RabbitMQCoreClient.Configuration
{
#pragma warning disable CS1591
    internal static class AppConstants
    {
        public static class RabbitMQHeaders
        {
            public const string TtlHeader = "x-message-ttl";
            public const string DeadLetterExchangeHeader = "x-dead-letter-exchange";
        }

        [NotNull]
        public static JsonSerializerSettings DefaultSerializerSettings =>
            new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
    }
}
