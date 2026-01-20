using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQCoreClient;
using RabbitMQCoreClient.DependencyInjection;
using SimpleConsole;

Console.WriteLine("Simple console message publishing and consuming messages example");

using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (sender, eventArgs) =>
{
    Console.WriteLine("\nCtrl+C pressed. Exiting...");
    cts.Cancel();
    eventArgs.Cancel = true; // Предотвращаем немедленное завершение процесса
};

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

// You can just call AddRabbitMQCoreClientConsumer(). It configures AddRabbitMQCoreClient() automatically.
services
    .AddRabbitMQCoreClientConsumer(config.GetSection("RabbitMQ"))
    .AddBatchQueueSender() // If you want to send messages with inmemory buffering.
    .AddHandler<SimpleObjectHandler>("test_key");

var provider = services.BuildServiceProvider();

var publisher = provider.GetRequiredService<IQueueService>();

await publisher.ConnectAsync(cts.Token);

var consumer = provider.GetRequiredService<IQueueConsumer>();

await consumer.StartAsync(cts.Token);

await publisher.SendAsync("""{ "Name": "bar" }""", "test_key", cancellationToken: cts.Token);

await WaitForCancellationAsync(cts.Token);

static async Task WaitForCancellationAsync(CancellationToken cancellationToken)
{
    // Creating a TaskCompletionSource that will terminate when the token is canceled.
    var tcs = new TaskCompletionSource<bool>();

    // Registering a callback for token cancellation.
    using (cancellationToken.Register(() => tcs.SetResult(true)))
    {
        await tcs.Task;
    }
}
