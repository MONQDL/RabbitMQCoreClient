using RabbitMQ.Client.Events;
using System.Net.Security;
using System.Security.Authentication;
using System.Text.Json.Serialization;

namespace RabbitMQCoreClient.DependencyInjection;

public class RabbitMQCoreClientOptions
{
    /// <summary>
    /// RabbitMQ server address.
    /// </summary>
    public string HostName { get; set; } = "127.0.0.1";

    /// <summary>
    /// Password of the user who has rights to connect to the server <see cref="HostName"/>.
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// Service access port.
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Timeout setting for connection attempts (in milliseconds).
    /// Default: 30000.
    /// </summary>
    public int RequestedConnectionTimeout { get; set; } = 30000;

    public ushort RequestedHeartbeat { get; set; } = 60;

    /// <summary>
    /// Timeout setting between reconnection attempts (in milliseconds).
    /// Default: 3000.
    /// </summary>
    public int ReconnectionTimeout { get; set; } = 3000;

    /// <summary>
    /// Reconnection attempts count.
    /// Default: null.
    /// So it will try to establish a connection every 3 seconds an unlimited number of times.
    /// In other case ConnectionException will occur when the limit is reached.
    /// </summary>
    public int? ReconnectionAttemptsCount { get; set; }

    /// <summary>
    /// User who has rights to connect to the server <see cref="HostName"/>.
    /// </summary>
    public string UserName { get; set; } = "guest";

    /// <summary>
    /// Gets or sets the virtual host.
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// The number of times the message was attempted to be processed during which an exception was thrown.
    /// </summary>
    public int DefaultTtl { get; set; } = 5;

    /// <summary>
    /// Number of messages to be pre-loaded into the handler.
    /// </summary>
    public ushort PrefetchCount { get; set; } = 1;

    /// <summary>
    /// Internal library call exception handler event.
    /// </summary>
    public AsyncEventHandler<CallbackExceptionEventArgs>? ConnectionCallbackExceptionHandler { get; set; }

    /// <summary>
    /// While creating queues use parameter "x-queue-type": "quorum" on the whole client.
    /// </summary>
    public bool UseQuorumQueues { get; set; } = false;

    /// <summary>
    /// Controls if TLS should indeed be used. Set to false to disable TLS
    /// on the connection.
    /// </summary>
    public bool SslEnabled { get; set; } = false;

    /// <summary>
    /// Retrieve or set the set of TLS policy (peer verification) errors that are deemed acceptable.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SslPolicyErrors SslAcceptablePolicyErrors { get; set; } = SslPolicyErrors.None;

    /// <summary>
    /// Retrieve or set the TLS protocol version.
    /// The client will let the OS pick a suitable version by using <see cref="SslProtocols.None" />.
    /// If this option is disabled, e.g.see via app context, the client will attempt to fall back
    /// to TLSv1.2.
    /// </summary>
    /// <seealso cref="SslProtocols" />
    /// <seealso href="https://www.rabbitmq.com/ssl.html#dotnet-client" />
    /// <seealso href="https://docs.microsoft.com/en-us/dotnet/framework/network-programming/tls?view=netframework-4.6.2" />
    /// <seealso href="https://docs.microsoft.com/en-us/dotnet/api/system.security.authentication.sslprotocols?view=netframework-4.8" />
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SslProtocols SslVersion { get; set; } = SslProtocols.None;

    /// <summary>
    /// Retrieve or set server's expected name.
    /// This MUST match the Subject Alternative Name (SAN) or CN on the peer's (server's) leaf certificate,
    /// otherwise the TLS connection will fail.
    /// </summary>
    public string? SslServerName { get; set; }

    /// <summary>
    /// Attempts to check certificate revocation status. Default is false.
    /// Set to true to check peer certificate for revocation.
    /// </summary>
    /// <remarks>
    /// Uses the built-in .NET TLS implementation machinery for checking a certificate against
    /// certificate revocation lists.
    /// </remarks>
    public bool SslCheckCertificateRevocation { get; set; } = false;

    /// <summary>
    /// Retrieve or set the client certificate passphrase.
    /// </summary>
    public string? SslCertPassphrase { get; set; }

    /// <summary>
    /// Retrieve or set the path to client certificate.
    /// </summary>
    public string? SslCertPath { get; set; }

    /// <summary>
    /// The maximum message body size limit. Default is 16MBi.
    /// </summary>
    public int MaxBodySize { get; set; } = 16 * 1024 * 1024; // 16 МБи
}
