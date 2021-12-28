using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQCoreClient.Configuration.DependencyInjection.Options;
using RabbitMQCoreClient.Serializers;
using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQCoreClient.ConsoleClient
{
    class Program
    {
        static readonly AutoResetEvent _closing = new AutoResetEvent(false);

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.CancelKeyPress += Exit;

            var config = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.Development.json", optional: false)
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(LoggerFactory.Create(x =>
            {
                x.SetMinimumLevel(LogLevel.Trace);
                x.AddConsole();
            }));
            // Just for sending messages.
            services
                .AddRabbitMQCoreClient(config.GetSection("RabbitMQ"))
                .AddSystemTextJson(x =>
                {
                    x.PropertyNamingPolicy = null;
                });

            // For sending and consuming messages config with subscriptions.
            services
                .AddRabbitMQCoreClientConsumer(config.GetSection("RabbitMQ"))
                .AddHandler<Handler>(new[] { "test_routing_key" }, new ConsumerHandlerOptions
                {
                    RetryKey = "test_routing_key_retry",
                    CustomSerializer = new SystemTextJsonMessageSerializer(x =>
                    {
                        x.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                        x.PropertyNameCaseInsensitive = false;
                    })
                })
                .AddHandler<Handler>(new[] { "test_routing_key_subscription" }, new ConsumerHandlerOptions
                {
                    RetryKey = "test_routing_key_retry",
                    CustomSerializer = new SystemTextJsonMessageSerializer(x =>
                    {
                        x.PropertyNamingPolicy = null;
                        x.PropertyNameCaseInsensitive = false;
                    })
                });

            // For sending and consuming messages full configuration.
            //services
            //    .AddRabbitMQCoreClient(config)
            //    .AddExchange("sdsad")
            //    .AddConsumer()
            //    .AddHandler<Handler>(new[] { "test_routing_key" })
            //    .AddQueue("asdasdad")
            //    .AddSubscription();

            var serviceProvider = services.BuildServiceProvider();

            var queueService = serviceProvider.GetRequiredService<IQueueService>();
            var consumer = serviceProvider.GetRequiredService<IQueueConsumer>();
            consumer.Start();

            //var body = new SimpleObj { Name = "test sending" };
            //await queueService.SendAsync(body, "test_routing_key");
            //await queueService.SendAsync(body, "test_routing_key");

            // Send a batch of messages parallel.
            //await Task.WhenAll(
            //    Enumerable.Range(0, 1000)
            //    .Select(i =>
            //    {
            //        try
            //        {
            //            Task.Delay(3000);
            //            var bodyList = Enumerable.Range(1, 1).Select(x => new SimpleObj { Name = $"test sending {x}" });
            //            return queueService.SendBatchAsync(bodyList, "test_routing_key", jsonSerializerSettings: new Newtonsoft.Json.JsonSerializerSettings()).AsTask();
            //        }
            //        catch (Exception e)
            //        {
            //            Console.Error.WriteLine("Ошибка отправки сообщения " + e.Message);
            //            return Task.CompletedTask;
            //        }
            //    }));
            CancellationTokenSource source = new CancellationTokenSource();
            var longRunningTask = CreateSender(queueService, source.Token);

            //var bodyList = Enumerable.Range(1, 1).Select(x => new SimpleObj { Name = $"test sending {x}" });
            //await queueService.SendBatchAsync(bodyList, "test_routing_key", jsonSerializerSettings: new Newtonsoft.Json.JsonSerializerSettings()).AsTask();

            Console.WriteLine("Waiting");
            _closing.WaitOne();
            //await longRunningTask;
            Environment.Exit(0);
        }

        static async Task CreateSender(IQueueService queueService, CancellationToken token)
        {
            while (true)
            {
                if (token.IsCancellationRequested)
                    return;
                try
                {
                    await Task.Delay(3000, token);
                    var bodyList = Enumerable.Range(1, 1).Select(x => new SimpleObj { Name = $"test sending {x}" });
                    await queueService.SendBatchAsync(bodyList, "test_routing_key_subscription").AsTask();
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Error during message send " + e.Message);
                }
            }
        }

        protected static void Exit(object sender, ConsoleCancelEventArgs args)
        {
            args.Cancel = true;
            Console.WriteLine("Exit");
            _closing.Set();
        }
    }
}
