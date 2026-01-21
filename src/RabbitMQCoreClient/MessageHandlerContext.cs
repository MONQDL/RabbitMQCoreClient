using RabbitMQCoreClient.DependencyInjection;

namespace RabbitMQCoreClient;

/// <summary>
/// Represents the context for processing a RabbitMQ message, including error routing
/// instructions and handler options.
/// </summary>
public class MessageHandlerContext
{
    /// <summary>
    /// Instructions to the router in case of an exception while processing a message.
    /// </summary>
    public ErrorMessageRouting ErrorMessageRouter { get; }

    /// <summary>
    /// Consumer handler options, that was used during configuration.
    /// </summary>
    public ConsumerHandlerOptions? Options { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageHandlerContext"/> class.
    /// </summary>
    /// <param name="errorMessageRouter">The error message routing instructions.</param>
    /// <param name="options">The consumer handler options.</param>
    public MessageHandlerContext(ErrorMessageRouting errorMessageRouter, ConsumerHandlerOptions? options)
    {
        ErrorMessageRouter = errorMessageRouter;
        Options = options;
    }
}
