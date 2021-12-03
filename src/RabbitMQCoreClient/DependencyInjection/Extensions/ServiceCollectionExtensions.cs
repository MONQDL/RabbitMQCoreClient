using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RabbitMQCoreClient.Configuration.DependencyInjection;
using RabbitMQCoreClient.Configuration.DependencyInjection.Options;
using RabbitMQCoreClient.DependencyInjection.ConfigFormats;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Класс, содержащий методы-расширения для создания интерфейса настройки обработчика сообщений RabbitMQ
    /// <see cref="IRabbitMQCoreClientConsumerBuilder"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        static IRabbitMQCoreClientBuilder AddRabbitMQCoreClient(this IServiceCollection services)
        {
            var builder = new RabbitMQCoreClientBuilder(services);

            builder.AddRequiredPlatformServices();

            builder.AddDefaultSerializer();

            services.TryAddSingleton<IRabbitMQCoreClientBuilder>(builder);

            return builder;
        }

        /// <summary>
        /// Создать экземпляр класса настройки обработчика сообщений RabbitMQ.
        /// </summary>
        /// <param name="services">Список сервисов, зарегистрированных в DI.</param>
        /// <param name="configuration">Секция конфигурации, содержащая поля для настройки сервиса обработки сообщений.</param>
        /// <param name="setupAction">Используйте данный метод, если требуется переопределить конфигурацию.</param>
        public static IRabbitMQCoreClientBuilder AddRabbitMQCoreClient(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<RabbitMQCoreClientOptions>? setupAction = null)
        {
            RegisterOptions(services, configuration, setupAction);

            // Производим автонастройку очередей из настроек IConfiguration.
            var builder = services.AddRabbitMQCoreClient();

            // Поддержка старого формата регистрации очереди.
            builder.RegisterV1Configuration(configuration);
            builder.RegisterV2Configuration(configuration);

            return builder;
        }

        /// <summary>
        /// Создать экземпляр класса настройки обработчика сообщений RabbitMQ.
        /// </summary>
        public static IRabbitMQCoreClientBuilder AddRabbitMQCoreClient(this IServiceCollection services,
            Action<RabbitMQCoreClientOptions>? setupAction)
        {
            services.Configure(setupAction);
            return services.AddRabbitMQCoreClient();
        }

        /// <summary>
        /// Создать экземпляр класса настройки обработчика сообщений RabbitMQ.
        /// </summary>
        /// <param name="services">Список сервисов, зарегистрированных в DI.</param>
        /// <param name="configuration">Секция конфигурации, содержащая поля для настройки сервиса обработки сообщений.</param>
        /// <param name="setupAction">Используйте данный метод, если требуется переопределить конфигурацию.</param>
        public static IRabbitMQCoreClientConsumerBuilder AddRabbitMQCoreClientConsumer(this IServiceCollection services,
            IConfiguration configuration, Action<RabbitMQCoreClientOptions>? setupAction = null)
        {
            // Производим автонастройку очередей из настроек IConfiguration.
            var builder = services.AddRabbitMQCoreClient(configuration, setupAction);

            var consumerBuilder = builder.AddConsumer();

            // Поддержка старого формата регистрации очереди.
            consumerBuilder.RegisterV1Configuration(configuration);
            consumerBuilder.RegisterV2Configuration(configuration);

            return consumerBuilder;
        }

        static void RegisterOptions(IServiceCollection services, IConfiguration configuration, Action<RabbitMQCoreClientOptions>? setupAction)
        {
            var instance = configuration.Get<RabbitMQCoreClientOptions>();
            setupAction?.Invoke(instance);
            var options = Options.Options.Create(instance);

            services.AddSingleton((x) => options);
        }
    }
}
