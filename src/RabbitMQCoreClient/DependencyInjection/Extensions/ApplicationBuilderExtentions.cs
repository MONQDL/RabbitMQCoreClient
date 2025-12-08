using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using RabbitMQCoreClient.Exceptions;

namespace Microsoft.Extensions.DependencyInjection;

public static class ApplicationBuilderExtentions
{
    /// <summary>Starts the rabbit mq core client consuming queues.</summary>
    /// <param name="app">The application.</param>
    /// <param name="lifetime">The lifetime.</param>
    public static IApplicationBuilder StartRabbitMqCore(this IApplicationBuilder app, IHostApplicationLifetime lifetime)
    {
        var consumer = app.ApplicationServices.GetService<IQueueConsumer>()
            ?? throw new ClientConfigurationException("Rabbit MQ Core Client Consumer is not configured. " +
                "Add services.AddRabbitMQCoreClient(...).AddConsumer(); to the DI.");
        lifetime.ApplicationStarted.Register(() => consumer.Start());
        lifetime.ApplicationStopping.Register(() => consumer.Shutdown());

        return app;
    }
}
