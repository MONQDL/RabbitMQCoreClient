using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using RabbitMQCoreClient;
using RabbitMQCoreClient.DependencyInjection;
using RabbitMQCoreClient.Exceptions;

namespace Microsoft.Extensions.DependencyInjection;

public static class ApplicationBuilderExtentions
{
    /// <summary>Starts the rabbit mq core client consuming queues.</summary>
    /// <param name="app">The application.</param>
    /// <param name="lifetime">The lifetime.</param>
    /// <param name="runMode">Describes the mode in which the service should start.</param>
    public static IApplicationBuilder StartRabbitMqCore(this IApplicationBuilder app, 
        IHostApplicationLifetime lifetime, 
        RunModes runMode = RunModes.PublisherAndConsumer)
    {
        if (runMode is RunModes.PublisherAndConsumer)
        {
            var publisher = app.ApplicationServices.GetService<IQueueService>()
                ?? throw new ClientConfigurationException("Rabbit MQ Core Client Service is not configured. " +
                    "Add services.AddRabbitMQCoreClient(...); to the DI.");
            var consumer = app.ApplicationServices.GetService<IQueueConsumer>()
                ?? throw new ClientConfigurationException("Rabbit MQ Core Client Consumer is not configured. " +
                    "Add services.AddRabbitMQCoreClient(...).AddConsumer(); to the DI.");
            lifetime.ApplicationStarted.Register(async () => await consumer.Start());
            lifetime.ApplicationStopping.Register(async () =>
            {
                await consumer.Shutdown();
                await publisher.Shutdown();
            });
        }
        else
        {
            var publisher = app.ApplicationServices.GetService<IQueueService>()
                ?? throw new ClientConfigurationException("Rabbit MQ Core Client Service is not configured. " +
                    "Add services.AddRabbitMQCoreClient(...); to the DI.");
            lifetime.ApplicationStarted.Register(() => publisher.Connect());
            lifetime.ApplicationStopping.Register(() => publisher.Shutdown());
        }


        return app;
    }
}
