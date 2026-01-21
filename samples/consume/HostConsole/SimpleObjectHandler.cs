using Microsoft.Extensions.Logging;
using RabbitMQCoreClient;
using RabbitMQCoreClient.Models;
using System.Text.Json.Serialization.Metadata;

namespace HostConsole;

internal sealed class SimpleObjectHandler : MessageHandlerJson<SimpleObj>
{
    readonly ILogger<SimpleObjectHandler> _logger;

    public SimpleObjectHandler(ILogger<SimpleObjectHandler> logger)
    {
        _logger = logger;
    }

    protected override JsonTypeInfo<SimpleObj> GetSerializerContext() => SimpleObjContext.Default.SimpleObj;

    protected override Task HandleMessage(SimpleObj message, RabbitMessageEventArgs args, MessageHandlerContext context)
    {
        _logger.LogInformation("Incoming simple object name: {Name}", message.Name);

        return Task.CompletedTask;
    }
}

