using Microsoft.Extensions.DependencyInjection;

namespace RabbitMQCoreClient.DependencyInjection;

sealed class RabbitMQHostedService : Microsoft.Extensions.Hosting.IHostedService
{
    readonly IQueueService _publisher;
    readonly IQueueConsumer? _consumer;

    public RabbitMQHostedService(IServiceProvider serviceProvider)
    {
        _publisher = serviceProvider.GetRequiredService<IQueueService>();
        _consumer = serviceProvider.GetService<IQueueConsumer>();
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // If the AddConsumer() method was not called,
        // then we do not start the event listening channel.
        if (_consumer != null)
            await _consumer.Start(cancellationToken); // Consumer starts publisher first.
        else
            await _publisher.ConnectAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_publisher != null)
            await _publisher.ShutdownAsync(); // Publisher shuts down consumer too as it is Connection God.
    }
}
