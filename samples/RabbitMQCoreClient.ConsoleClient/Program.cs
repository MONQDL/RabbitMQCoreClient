using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQCoreClient;
using RabbitMQCoreClient.BatchQueueSender;
using RabbitMQCoreClient.ConsoleClient;
using RabbitMQCoreClient.DependencyInjection;
using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

Console.OutputEncoding = Encoding.UTF8;

using IHost host = new HostBuilder()
    .ConfigureHostConfiguration(configHost =>
    {
        configHost.AddCommandLine(args);
        configHost.AddJsonFile($"appsettings.Development.json", optional: false);
    })
    .ConfigureServices((builder, services) =>
    {
        services.AddLogging();
        services.AddSingleton(LoggerFactory.Create(x =>
        {
            x.SetMinimumLevel(LogLevel.Trace);
            x.AddConsole();
        }));

        // Just for sending messages.
        services
            .AddRabbitMQCoreClient(builder.Configuration.GetSection("RabbitMQ"))
            .AddBatchQueueSender()
            .AddSystemTextJson(x => x.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);

        // For sending and consuming messages config with subscriptions.
        services
            .AddRabbitMQCoreClientConsumer(builder.Configuration.GetSection("RabbitMQ"))
            .AddHandler<Handler>(new[] { "test_routing_key" }, new ConsumerHandlerOptions
            {
                RetryKey = "test_routing_key_retry"
            })
            .AddHandler<Handler>(new[] { "test_routing_key_subscription" }, new ConsumerHandlerOptions
            {
                RetryKey = "test_routing_key_retry"
            });

        // For sending and consuming messages full configuration.
        //services
        //    .AddRabbitMQCoreClient(config)
        //    .AddExchange("sdsad")
        //    .AddConsumer()
        //    .AddHandler<Handler>(new[] { "test_routing_key" })
        //    .AddQueue("asdasdad")
        //    .AddSubscription();
    })
    .UseConsoleLifetime()
    .Build();

var serviceProvider = host.Services;

var queueService = serviceProvider.GetRequiredService<IQueueService>();
var consumer = serviceProvider.GetRequiredService<IQueueConsumer>();
var batchSender = serviceProvider.GetRequiredService<IQueueEventsBufferEngine>();
await consumer.Start();

//var body = new SimpleObj { Name = "test sending" };
//await queueService.SendAsync(body, "test_routing_key");
//await queueService.SendAsync(body, "test_routing_key");

// Send a batch of messages parallel.
//await Task.WhenAll(
//    Enumerable.Range(0, 100)
//    .Select(i =>
//    {
//        try
//        {
//            var bodyList = Enumerable.Range(1, 1).Select(x => new SimpleObj { Name = $"test sending {x}" });
//            return queueService.SendBatchAsync(bodyList, "test_routing_key", jsonSerializerSettings: new Newtonsoft.Json.JsonSerializerSettings()).AsTask();
//        }
//        catch (Exception e)
//        {
//            Console.Error.WriteLine("Ошибка отправки сообщения " + e.Message);
//            return Task.CompletedTask;
//        }
//    }));
using CancellationTokenSource source = new CancellationTokenSource();
//await CreateSender(queueService, source.Token);

//var bodyList = Enumerable.Range(1, 1).Select(x => new SimpleObj { Name = $"test sending {x}" });
//await queueService.SendBatchAsync(bodyList, "test_routing_key", jsonSerializerSettings: new Newtonsoft.Json.JsonSerializerSettings()).AsTask();
await CreateBatchSender(batchSender, source.Token);

await host.RunAsync();


static async Task CreateSender(IQueueService queueService, CancellationToken token)
{
    while (!token.IsCancellationRequested)
    {
        try
        {
            await Task.Delay(1000, token);
            var bodyList = Enumerable.Range(1, 2).Select(x => new SimpleObj { Name = $"test sending {x}" });
            await queueService.SendBatchAsync(bodyList, "test_routing_key", SimpleObjContext.Default.SimpleObj);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("Error during message send " + e.Message);
        }
    }
}

static async Task CreateBatchSender(IQueueEventsBufferEngine batchSender, CancellationToken token)
{
    while (!token.IsCancellationRequested)
    {
        try
        {
            await Task.Delay(500, token);
            var bodyList = Enumerable.Range(1, 2).Select(x => new SimpleObj { Name = $"test sending {x}" });
            batchSender.AddEvents(bodyList, "test_routing_key");
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("Error during message send " + e.Message);
        }
    }
}
