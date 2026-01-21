using RabbitMQCoreClient.DependencyInjection;
using RabbitMQCoreClient.Models;
using System.Text;
using System.Text.Json.Serialization.Metadata;

namespace RabbitMQCoreClient.WebApp;

public class Handler : MessageHandlerJson<SimpleObj>
{
    protected override JsonTypeInfo<SimpleObj> GetSerializerContext() => SimpleObjContext.Default.SimpleObj;

    protected override Task HandleMessage(SimpleObj message, RabbitMessageEventArgs args, MessageHandlerContext context)
    {
        ProcessMessage(message);

        return Task.CompletedTask;
    }

    void ProcessMessage(SimpleObj obj) =>
        //if (obj.Name != "my test name")
        //{
        //    ErrorMessageRouter.MoveToDeadLetter();
        //    throw new ArgumentException("parser failed");
        //}

        Console.WriteLine("It's all ok.");
}

public class RawHandler : IMessageHandler
{
    public ErrorMessageRouting ErrorMessageRouter => new ErrorMessageRouting();
    public ConsumerHandlerOptions? Options { get; set; }

    public Task HandleMessage(ReadOnlyMemory<byte> message, RabbitMessageEventArgs args, MessageHandlerContext context)
    {
        Console.WriteLine(Encoding.UTF8.GetString(message.ToArray()));
        throw new Exception();
        return Task.CompletedTask;
    }
}
