using RabbitMQCoreClient.Configuration.DependencyInjection.Options;
using RabbitMQCoreClient.Models;
using RabbitMQCoreClient.Serializers;
using System;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQCoreClient
{
    /// <summary>
    /// Handler for the message received from the queue.
    /// </summary>
    /// <typeparam name="TModel">The type of model that will be deserialized into.</typeparam>
    /// <seealso cref="RabbitMQCoreClient.IMessageHandler" />
    public abstract class MessageHandlerJson<TModel> : IMessageHandler
    {
        /// <summary>
        /// Incoming message routing methods.
        /// </summary>
        public ErrorMessageRouting ErrorMessageRouter { get; } = new ErrorMessageRouting();

        /// <summary>
        /// The method will be called when there is an error parsing Json into the model.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <param name="e">The exception.</param>
        /// <param name="args">The <see cref="RabbitMessageEventArgs"/> instance containing the event data.</param>
        /// <returns></returns>
        protected virtual ValueTask OnParseError(string json, Exception e, RabbitMessageEventArgs args) => default;

        /// <summary>
        /// Process json message.
        /// </summary>
        /// <param name="message">The message deserialized into an object.</param>
        /// <param name="args">The <see cref="RabbitMessageEventArgs" /> instance containing the event data.</param>
        /// <returns></returns>
        protected abstract Task HandleMessage(TModel message, RabbitMessageEventArgs args);

        /// <summary>
        /// Raw Json formatted message.
        /// </summary>
        protected string? RawJson { get; set; }

        /// <summary>
        /// Gets the options.
        /// </summary>
        public ConsumerHandlerOptions? Options { get; set; }

        /// <summary>
        /// The default json serializer.
        /// </summary>
        public IMessageSerializer Serializer { get; set; } = new SystemTextJsonMessageSerializer();

        /// <inheritdoc />
        public async Task HandleMessage(ReadOnlyMemory<byte> message, RabbitMessageEventArgs args)
        {
            RawJson = Encoding.UTF8.GetString(message.ToArray());
            TModel messageModel;
            try
            {
                var obj = Serializer.Deserialize<TModel>(message);
                if (obj is null)
                    throw new InvalidOperationException("The json parser returns null.");
                messageModel = obj;
            }
            catch (Exception e)
            {
                await OnParseError(RawJson, e, args);
                // Fall to the top-level exception handler.
                throw;
            }

            await HandleMessage(messageModel, args);
        }
    }
}
