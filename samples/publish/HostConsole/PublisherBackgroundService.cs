using Microsoft.Extensions.Hosting;
using RabbitMQCoreClient;

namespace HostConsole;

class PublisherBackgroundService : BackgroundService
{
    readonly IQueueService _publisher;

    public PublisherBackgroundService(IQueueService publisher)
    {
        _publisher = publisher;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _publisher.SendAsync("""{ "foo": "bar" }""", "test_key", cancellationToken: stoppingToken);
    }
}
