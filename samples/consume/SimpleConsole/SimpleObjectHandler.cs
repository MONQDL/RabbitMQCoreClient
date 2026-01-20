using Microsoft.Extensions.Logging;
using RabbitMQCoreClient;
using RabbitMQCoreClient.Models;
using System.Text.Json.Serialization.Metadata;

namespace SimpleConsole;

internal sealed class SimpleObjectHandler : MessageHandlerJson<SimpleObj>
{
    readonly ILogger<SimpleObjectHandler> _logger;

    public SimpleObjectHandler(ILogger<SimpleObjectHandler> logger)
    {
        _logger = logger;
    }

    protected override JsonTypeInfo<SimpleObj> GetSerializerContext() => SimpleObjContext.Default.SimpleObj;

    protected override Task HandleMessage(SimpleObj message, RabbitMessageEventArgs args)
    {
        _logger.LogInformation("Incoming simple object name: {Name}", message.Name);

        return Task.CompletedTask;
    }

    protected override ValueTask OnParseError(string json, Exception e, RabbitMessageEventArgs args)
    {
        _logger.LogError(e, "Incoming message can't be deserialized. Error: {ErrorMessage}", e.Message);
        return base.OnParseError(json, e, args);
    }
}

