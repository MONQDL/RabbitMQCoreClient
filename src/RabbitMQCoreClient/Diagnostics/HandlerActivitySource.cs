using System.Diagnostics;

namespace RabbitMQCoreClient.Diagnostics;

internal static class HandlerActivitySource
{
    internal static readonly ActivitySource Source = new("RabbitMQCoreClient", "7.1");
}
