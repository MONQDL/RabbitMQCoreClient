using RabbitMQCoreClient.Configuration.DependencyInjection.Options;
using RabbitMQCoreClient.Models;
using System.Threading.Tasks;

namespace RabbitMQCoreClient
{
    /// <summary>
    /// Интерфейс обработчика, принятого из очереди сообщения.
    /// </summary>
    public interface IMessageHandler
    {
        /// <summary>
        /// Обработать сообщение асинхронно.
        /// </summary>
        /// <param name="message">Входящая строка json с объектом и его типом.</param>
        /// <param name="args">The <see cref="RabbitMessageEventArgs"/> instance containing the message data.</param>
        Task HandleMessage(string message, RabbitMessageEventArgs args);

        /// <summary>
        /// Указания маршрутизатору в случае исключения при обработке сообщения.
        /// </summary>
        ErrorMessageRouting ErrorMessageRouter { get; }

        /// <summary>
        /// Consumer handler options, that was used during configuration.
        /// </summary>
        ConsumerHandlerOptions? Options { get; set; }
    }
}