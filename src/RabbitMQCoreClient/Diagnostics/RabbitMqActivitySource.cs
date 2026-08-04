using System.Diagnostics;

namespace RabbitMQCoreClient.Diagnostics;

#pragma warning disable IDE1006
internal static class RabbitMqActivitySource
{
    internal static readonly ActivitySource Source = new("RabbitMQCoreClient", "7.1");
}
