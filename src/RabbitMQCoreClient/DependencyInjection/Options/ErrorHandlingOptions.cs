using RabbitMQ.Client.Events;
using System;

namespace RabbitMQCoreClient.Configuration.DependencyInjection.Options
{
    /// <summary>
    /// Параметры, позволяющие организовать собственные механизмы обработки исключений клиента.
    /// </summary>
    public class ErrorHandlingOptions
    {
        /// <summary>
        /// Событие обработчика исключения внутренних вызовов библиотеки. <c>null</c>
        /// для использования обработчиков по умолчанию.
        /// </summary>
        public EventHandler<CallbackExceptionEventArgs>? CallbackExceptionHandler { get; set; } = null;

        /// <summary>
        /// Событие обработчика исключения при невозможности восстановить соединение. <c>null</c>
        /// для использования обработчиков по умолчанию.
        /// </summary>
        public EventHandler<ConnectionRecoveryErrorEventArgs>? ConnectionRecoveryErrorHandler { get; set; } = null;
    }
}
