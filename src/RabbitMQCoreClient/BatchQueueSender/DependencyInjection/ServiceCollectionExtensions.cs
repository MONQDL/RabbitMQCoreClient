using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace RabbitMQCoreClient.BatchQueueSender.DependencyInjection
{
    /// <summary>
    /// Class containing extension methods for registering the BatchQueueSender services at DI.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        static IServiceCollection AddBatchQueueSenderCore(this IServiceCollection services)
        {
            services.AddTransient<IQueueEventsWriter, QueueEventsWriter>();
            services.AddSingleton<IQueueEventsBufferEngine, QueueEventsBufferEngine>();
            return services;
        }

        /// <summary>
        /// Registers an instance of the <see cref="IQueueEventsBufferEngine"/> interface in the DI.
        /// Injected service <see cref="IQueueEventsBufferEngine"/> can be used to send messages
        /// to the inmemory queue and process them at the separate thread.
        /// </summary>
        /// <param name="services">List of services registered in DI.</param>
        /// <param name="configuration">Configuration section containing fields for configuring the message processing service.</param>
        /// <param name="setupAction">Use this method if you need to override the configuration.</param>
        public static IServiceCollection AddBatchQueueSender(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<QueueBatchSenderOptions>? setupAction = null)
        {
            RegisterOptions(services, configuration, setupAction);

            return services.AddBatchQueueSenderCore();
        }

        /// <summary>
        /// Registers an instance of the <see cref="IQueueEventsBufferEngine"/> interface in the DI.
        /// Injected service <see cref="IQueueEventsBufferEngine"/> can be used to send messages
        /// to the inmemory queue and process them at the separate thread.
        /// </summary>
        /// <param name="services">List of services registered in DI.</param>
        /// <param name="setupAction">Use this method if you need to override the configuration.</param>
        /// <returns></returns>
        public static IServiceCollection AddBatchQueueSender(this IServiceCollection services,
            Action<QueueBatchSenderOptions>? setupAction)
        {
            services.Configure(setupAction);
            return services.AddBatchQueueSenderCore();
        }

        /// <summary>
        /// Registers an instance of the <see cref="IQueueEventsBufferEngine"/> interface in the DI.
        /// Injected service <see cref="IQueueEventsBufferEngine"/> can be used to send messages
        /// to the inmemory queue and process them at the separate thread.
        /// </summary>
        /// <param name="services">List of services registered in DI.</param>
        /// <returns></returns>
        public static IServiceCollection AddBatchQueueSender(this IServiceCollection services)
        {
            services.AddSingleton((x) => Options.Create(new QueueBatchSenderOptions()));
            return services.AddBatchQueueSenderCore();
        }

        static void RegisterOptions(IServiceCollection services,
            IConfiguration configuration,
            Action<QueueBatchSenderOptions>? setupAction)
        {
            var instance = configuration.Get<QueueBatchSenderOptions>();
            setupAction?.Invoke(instance);
            var options = Options.Create(instance);

            services.AddSingleton((x) => options);
        }
    }
}
