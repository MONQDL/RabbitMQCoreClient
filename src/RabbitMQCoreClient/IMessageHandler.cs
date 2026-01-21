using RabbitMQCoreClient.Models;

namespace RabbitMQCoreClient;

/// <summary>
/// The interface for the handler received from the message queue.
/// </summary>
public interface IMessageHandler
{
    /// <summary>
    /// Process the message asynchronously.
    /// </summary>
    /// <param name="message">Input byte array from consumed queue.</param>
    /// <param name="args">The <see cref="RabbitMessageEventArgs"/> instance containing the message data.</param>
    /// <param name="context">Handler configuration context.</param>
    Task HandleMessage(ReadOnlyMemory<byte> message,
        RabbitMessageEventArgs args,
        MessageHandlerContext context);
}
