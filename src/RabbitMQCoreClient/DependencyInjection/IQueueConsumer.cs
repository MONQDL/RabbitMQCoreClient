using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// The Consumer interface uses for starting and stopping manipulations.
    /// </summary>
    public interface IQueueConsumer : IDisposable
    {
        /// <summary>
        /// Connect to all queues and start receiving messages.
        /// </summary>
        /// <returns></returns>
        void Start();

        /// <summary>
        /// Stops listening Queues.
        /// </summary>
        void Shutdown();

        /// <summary>
        /// The channel that consume messages from the RabbitMQ Instance. 
        /// Can be Null, if <see cref="Start"/> method was not called.
        /// </summary>
        IModel? ConsumeChannel { get; }

        /// <summary>
        /// The Async consumer, with default consume method configurated.
        /// </summary>
        AsyncEventingBasicConsumer? Consumer { get; }
    }
}
