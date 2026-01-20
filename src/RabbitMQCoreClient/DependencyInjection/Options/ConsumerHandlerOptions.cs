using RabbitMQCoreClient.Serializers;

namespace RabbitMQCoreClient.DependencyInjection;

/// <summary>
/// Consumer Handler Options.
/// </summary>
public class ConsumerHandlerOptions
{
    /// <summary>
    /// The routing key that will mark the message on the exception handling stage.
    /// If configured, the message will return to the exchange with this key instead of original Key.
    /// </summary>
    public string? RetryKey { get; set; } = default;
}
