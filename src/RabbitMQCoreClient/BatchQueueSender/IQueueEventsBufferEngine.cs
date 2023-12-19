using System.Collections.Generic;
using System.Threading.Tasks;

namespace RabbitMQCoreClient.BatchQueueSender
{
    /// <summary>
    /// Event buffer interface.
    /// </summary>
    public interface IQueueEventsBufferEngine
    {
        /// <summary>
        /// Add an event to send to the data bus.
        /// </summary>
        /// <param name="event">The object to send to the data bus.</param>
        /// <param name="routingKey">The name of the route key with which you want to send events to the data bus.</param>
        /// <returns><see cref="Task"/> showing the completion of the operation.</returns>
        Task AddEvent<T>(T @event, string routingKey);

        /// <summary>
        /// Add events to send to the data bus.
        /// </summary>
        /// <typeparam name="T">The type of list item of the <paramref name="events"/> property.</typeparam>
        /// <param name="events">The list of objects to send to the data bus.</param>
        /// <param name="routingKey">The name of the route key with which you want to send events to the data bus.</param>
        /// <returns></returns>
        Task AddEvents<T>(IEnumerable<T> events, string routingKey);
    }
}
