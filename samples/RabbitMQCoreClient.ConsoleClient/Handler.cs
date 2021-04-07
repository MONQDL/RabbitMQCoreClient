using Newtonsoft.Json;
using RabbitMQCoreClient.Configuration.DependencyInjection.Options;
using RabbitMQCoreClient.Models;
using System;
using System.Threading.Tasks;

namespace RabbitMQCoreClient.ConsoleClient
{
    public class Handler : MessageHandlerJson<SimpleObj>
    {
        protected override Task HandleMessage(SimpleObj message, RabbitMessageEventArgs args)
        {
            Console.WriteLine($"message from {args.RoutingKey}");
            ProcessMessage(message);

            return Task.CompletedTask;
        }

        protected override ValueTask OnParseError(string json, JsonException e, RabbitMessageEventArgs args) => base.OnParseError(json, e, args);

        void ProcessMessage(SimpleObj obj)
        {
            //if (obj.Name != "my test name")
            //{
            //    ErrorMessageRouter.MoveToDeadLetter();
            //    throw new ArgumentException("parser failed");
            //}
            Console.WriteLine(this.Options.JsonSerializerSettings.ContractResolver?.ToString() ?? "NULL");
            Console.WriteLine("RAW: " + this.RawJson);
        }
    }

    public class RawHandler : IMessageHandler
    {
        public ErrorMessageRouting ErrorMessageRouter => new ErrorMessageRouting();
        public ConsumerHandlerOptions Options { get; set; }

        public Task HandleMessage(string message, RabbitMessageEventArgs args)
        {
            Console.WriteLine(message);
            return Task.CompletedTask;
        }
    }

    public class RawErrorHandler : IMessageHandler
    {
        public ErrorMessageRouting ErrorMessageRouter => new ErrorMessageRouting();
        public ConsumerHandlerOptions Options { get; set; }

        public Task HandleMessage(string message, RabbitMessageEventArgs args)
        {
            Console.WriteLine(message);
            throw new Exception();
        }
    }
}
