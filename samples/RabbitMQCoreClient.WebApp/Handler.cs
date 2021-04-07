using Newtonsoft.Json;
using RabbitMQCoreClient.Configuration.DependencyInjection.Options;
using RabbitMQCoreClient.Models;
using System;
using System.Threading.Tasks;

namespace RabbitMQCoreClient.WebApp
{
    public class Handler : MessageHandlerJson<SimpleObj>
    {
        protected override Task HandleMessage(SimpleObj message, RabbitMessageEventArgs args)
        {
            ProcessMessage(message);

            return Task.CompletedTask;
        }

        protected override ValueTask OnParseError(string json, JsonException e, RabbitMessageEventArgs args) => base.OnParseError(json, e, args);

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
        public ConsumerHandlerOptions Options
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public Task HandleMessage(string message, RabbitMessageEventArgs args)
        {
            Console.WriteLine(message);
            throw new Exception();
            return Task.CompletedTask;
        }
    }
}
