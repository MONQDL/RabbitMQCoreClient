using RabbitMQCoreClient.DependencyInjection;
using RabbitMQCoreClient.Models;
using RabbitMQCoreClient.Serializers;
using System;
using System.Text;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace RabbitMQCoreClient.ConsoleClient;

public class Handler : MessageHandlerJson<SimpleObj>
{
    protected override JsonTypeInfo<SimpleObj> GetSerializerContext() =>
        SimpleObjContext.Default.SimpleObj;

    protected override Task HandleMessage(SimpleObj message, RabbitMessageEventArgs args)
    {
        Console.WriteLine($"message from {args.RoutingKey}");
        ProcessMessage(message);

        return Task.CompletedTask;
    }

    protected override ValueTask OnParseError(string json, Exception e, RabbitMessageEventArgs args) => base.OnParseError(json, e, args);

    void ProcessMessage(SimpleObj obj)
    {
        //if (obj.Name != "my test name")
        //{
        //    ErrorMessageRouter.MoveToDeadLetter();
        //    throw new ArgumentException("parser failed");
        //}
        Console.WriteLine("obj.Name: " + obj.Name);
        Console.WriteLine("RAW: " + this.RawJson);
    }
}

public class RawHandler : IMessageHandler
{
    public ErrorMessageRouting ErrorMessageRouter => new ErrorMessageRouting();
    public ConsumerHandlerOptions Options { get; set; }

    public Task HandleMessage(ReadOnlyMemory<byte> message, RabbitMessageEventArgs args)
    {
        Console.WriteLine(Encoding.UTF8.GetString(message.ToArray()));
        return Task.CompletedTask;
    }
}

public class RawErrorHandler : IMessageHandler
{
    public ErrorMessageRouting ErrorMessageRouter => new ErrorMessageRouting();
    public ConsumerHandlerOptions Options { get; set; }
    public IMessageSerializer Serializer { get; set; }

    public Task HandleMessage(ReadOnlyMemory<byte> message, RabbitMessageEventArgs args)
    {
        Console.WriteLine(Encoding.UTF8.GetString(message.ToArray()));
        throw new Exception();
    }
}
