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
    }
}
