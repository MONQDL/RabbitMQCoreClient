using RabbitMQ.Client;
using RabbitMQCoreClient.Configuration.DependencyInjection.Options;

namespace RabbitMQCoreClient.Configuration.DependencyInjection;

/// <summary>
/// The RabbitMQ Exchange
/// </summary>
public class Exchange
{
    /// <summary>
    /// Exchange point name.
    /// </summary>
    public string Name => Options.Name;

    /// <summary>
    /// Exchange point configuration settings.
    /// </summary>
    public ExchangeOptions Options { get; } = new ExchangeOptions();

    /// <summary>
    /// Initializes a new instance of the <see cref="Exchange" /> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">options</exception>
    /// <exception cref="ArgumentException">exchangeName
    /// or
    /// services</exception>
    public Exchange(ExchangeOptions options)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options), $"{nameof(options)} is null.");

        if (string.IsNullOrEmpty(options.Name))
            throw new ArgumentException($"Name in {nameof(options)} is null or empty.", nameof(options));
    }

    /// <summary>
    /// Starts the exchange.
    /// </summary>
    /// <param name="_channel">The channel.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task StartExchangeAsync(IChannel _channel, CancellationToken cancellationToken = default) => 
        _channel.ExchangeDeclareAsync(
            exchange: Name,
            type: Options.Type,
            durable: Options.Durable,
            autoDelete: Options.AutoDelete,
            arguments: Options.Arguments,
            cancellationToken: cancellationToken
            );
}
