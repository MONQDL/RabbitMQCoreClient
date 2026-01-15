using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQCoreClient;
using RabbitMQCoreClient.BatchQueueSender.DependencyInjection;

Console.WriteLine("Simple console message publishing only example");

var config = new ConfigurationBuilder()
            .AddJsonFile($"appsettings.json", optional: false)
            .AddJsonFile($"appsettings.Development.json", optional: true)
            .Build();

var services = new ServiceCollection();
services.AddLogging();
services.AddSingleton(LoggerFactory.Create(x =>
{
    x.SetMinimumLevel(LogLevel.Trace);
    x.AddConsole();
}));

// Just for sending messages.
services.AddRabbitMQCoreClient(config.GetSection("RabbitMQ"))
    .AddBatchQueueSender();

var provider = services.BuildServiceProvider();

var publisher = provider.GetRequiredService<IQueueService>();

await publisher.ConnectAsync();

await publisher.SendAsync("""{ "foo": "bar" }""", "test_key");
