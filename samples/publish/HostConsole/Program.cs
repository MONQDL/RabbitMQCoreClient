using HostConsole;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQCoreClient.DependencyInjection;

Console.WriteLine("Host console message publishing only example");

using IHost host = new HostBuilder()
    .ConfigureHostConfiguration(configHost =>
    {
        configHost.AddCommandLine(args);

        configHost
            .AddJsonFile($"appsettings.json", optional: false)
            .AddJsonFile($"appsettings.Development.json", optional: false);
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
            .AddRabbitMQCoreClient(builder.Configuration.GetSection("RabbitMQ"));

        services.AddHostedService<PublisherBackgroundService>();
    })
    .UseConsoleLifetime()
    .Build();

await host.RunAsync();
