using Newtonsoft.Json;
using RabbitMQCoreClient.Configuration;
using RabbitMQCoreClient.Configuration.DependencyInjection.Options;
using RabbitMQCoreClient.Models;
using System;
using System.Threading.Tasks;

namespace RabbitMQCoreClient
{
    /// <summary>
    /// Обработчик сообщения, принятого из очереди.
    /// </summary>
    /// <typeparam name="TModel">Тип модели, в котобую будет произведена десериализация.</typeparam>
    /// <seealso cref="RabbitMQCoreClient.IMessageHandler" />
    public abstract class MessageHandlerJson<TModel> : IMessageHandler
    {
        /// <summary>
        /// Методы маршрутизации входящего сообщения.
        /// </summary>
        public ErrorMessageRouting ErrorMessageRouter { get; } = new ErrorMessageRouting();

        /// <summary>
        /// Метод будет вызван при ошибке парсинга Json в модель.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <param name="e">The e.</param>
        /// <param name="args">The <see cref="RabbitMessageEventArgs"/> instance containing the event data.</param>
        /// <returns></returns>
        protected virtual ValueTask OnParseError(string json, JsonException e, RabbitMessageEventArgs args) => default;

        /// <summary>
        /// Обработать json сообщение.
        /// </summary>
        /// <param name="message">Сообщение, десериализованное в объект.</param>
        /// <param name="args">The <see cref="RabbitMessageEventArgs" /> instance containing the event data.</param>
        /// <returns></returns>
        protected abstract Task HandleMessage(TModel message, RabbitMessageEventArgs args);

        /// <summary>
        /// Сообщение в формате Json.
        /// </summary>
        protected string? RawJson { get; set; }

        /// <summary>
        /// Gets the options.
        /// </summary>
        public ConsumerHandlerOptions? Options { get; set; }

        /// <inheritdoc />
        public async Task HandleMessage(string message, RabbitMessageEventArgs args)
        {
            RawJson = message;
            TModel messageModel;
            try
            {
                messageModel = JsonConvert.DeserializeObject<TModel>(message, Options?.JsonSerializerSettings ?? AppConstants.DefaultSerializerSettings);
            }
            catch (JsonException e)
            {
                await OnParseError(message, e, args);
                // Падаем на верхнеуровневый обработчик.
                throw;
            }

            await HandleMessage(messageModel, args);
        }
    }
}
