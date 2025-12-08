using RabbitMQ.Client.Events;
using System;

namespace RabbitMQCoreClient.Configuration.DependencyInjection.Options;

/// <summary>
/// Parameters that allow you to organize your own client exception handling mechanisms.
/// </summary>
public class ErrorHandlingOptions
{
    /// <summary>
    /// Internal library call exception handler event. <c>null</c> to use default handlers.
    /// </summary>
    public EventHandler<CallbackExceptionEventArgs>? CallbackExceptionHandler { get; set; } = null;

    /// <summary>
    /// An exception handler event when the connection cannot be reestablished. <c>null</c> to use default handlers.
    /// </summary>
    public EventHandler<ConnectionRecoveryErrorEventArgs>? ConnectionRecoveryErrorHandler { get; set; } = null;
}
