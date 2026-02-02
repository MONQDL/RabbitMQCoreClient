using RabbitMQCoreClient.Models;
using System;
using System.Text;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace RabbitMQCoreClient.ConsoleClient;

public class Handler : MessageHandlerJson<SimpleObj>
{
    protected override JsonTypeInfo<SimpleObj> GetSerializerContext() =>
        SimpleObjContext.Default.SimpleObj;

    protected override Task HandleMessage(SimpleObj message, RabbitMessageEventArgs args, MessageHandlerContext context)
    {
        Console.WriteLine($"message from {args.RoutingKey}");
        ProcessMessage(message);

        return Task.CompletedTask;
    }

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
    public Task HandleMessage(ReadOnlyMemory<byte> message, RabbitMessageEventArgs args, MessageHandlerContext context)
    {
        Console.WriteLine(Encoding.UTF8.GetString(message.ToArray()));
        return Task.CompletedTask;
    }
}

public class RawErrorHandler : IMessageHandler
{
    public Task HandleMessage(ReadOnlyMemory<byte> message, RabbitMessageEventArgs args, MessageHandlerContext context)
    {
        Console.WriteLine(Encoding.UTF8.GetString(message.ToArray()));
        throw new Exception();
    }
}
