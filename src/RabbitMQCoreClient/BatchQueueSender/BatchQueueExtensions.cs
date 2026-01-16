using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace RabbitMQCoreClient.BatchQueueSender;

/// <summary>
/// BatchQueue extension methods.
/// </summary>
public static class BatchQueueExtensions
{
    /// <summary>
    /// Add an object to be send as event to the data bus.
    /// </summary>
    /// <param name="service">The <see cref="IQueueEventsBufferEngine"/> object.</param>
    /// <param name="obj">The object to send to the data bus.</param>
    /// <param name="routingKey">The name of the route key with which you want to send events to the data bus.</param>
    public static void AddEvent<T>(this IQueueEventsBufferEngine service, [NotNull] T obj, string routingKey)
        where T : class =>
        service.Add(new EventItem(service.Serializer.Serialize(obj), routingKey));

    /// <summary>
    /// Add a byte array object to be send as event to the data bus.
    /// </summary>
    /// <param name="service">The <see cref="IQueueEventsBufferEngine"/> object.</param>
    /// <param name="obj">The byte array object to send to the data bus.</param>
    /// <param name="routingKey">The name of the route key with which you want to send events to the data bus.</param>
    public static void AddEvent(this IQueueEventsBufferEngine service, ReadOnlyMemory<byte> obj, string routingKey) =>
        service.Add(new EventItem(obj, routingKey));

    /// <summary>
    /// Add a byte array object to send as event to the data bus.
    /// </summary>
    /// <param name="service">The <see cref="IQueueEventsBufferEngine"/> object.</param>
    /// <param name="obj">The byte array object to send to the data bus.</param>
    /// <param name="routingKey">The name of the route key with which you want to send events to the data bus.</param>
    public static void AddEvent(this IQueueEventsBufferEngine service, byte[] obj, string routingKey) =>
        service.Add(new EventItem(obj, routingKey));

    /// <summary>
    /// Add a string object to send as event to the data bus.
    /// </summary>
    /// <param name="service">The <see cref="IQueueEventsBufferEngine"/> object.</param>
    /// <param name="obj">The string object to send to the data bus.</param>
    /// <param name="routingKey">The name of the route key with which you want to send events to the data bus.</param>
    public static void AddEvent(this IQueueEventsBufferEngine service, string obj, string routingKey) =>
        service.Add(new EventItem(Encoding.UTF8.GetBytes(obj).AsMemory(), routingKey));

    /// <summary>
    /// Add objects collection to send as events to the data bus.
    /// </summary>
    /// <typeparam name="T">The type of list item of the <paramref name="objs"/> property.</typeparam>
    /// <param name="service">The <see cref="IQueueEventsBufferEngine"/> object.</param>
    /// <param name="objs">The list of objects to send to the data bus.</param>
    /// <param name="routingKey">The name of the route key with which you want to send events to the data bus.</param>
    /// <returns></returns>
    public static void AddEvents<T>(this IQueueEventsBufferEngine service, IEnumerable<T> objs, string routingKey)
        where T : class
    {
        foreach (var obj in objs)
            service.Add(new EventItem(service.Serializer.Serialize(obj), routingKey));
    }

    /// <summary>
    /// Add a byte array objects collection to send as event to the data bus.
    /// </summary>
    /// <param name="service">The <see cref="IQueueEventsBufferEngine"/> object.</param>
    /// <param name="objs">The list of byte array objects to send to the data bus.</param>
    /// <param name="routingKey">The name of the route key with which you want to send events to the data bus.</param>
    /// <returns></returns>
    public static void AddEvents(this IQueueEventsBufferEngine service, IEnumerable<ReadOnlyMemory<byte>> objs, string routingKey)
    {
        foreach (var obj in objs)
            service.Add(new EventItem(obj, routingKey));
    }

    /// <summary>
    /// Add a byte array objects collection to send as event to the data bus.
    /// </summary>
    /// <param name="service">The <see cref="IQueueEventsBufferEngine"/> object.</param>
    /// <param name="objs">The list of byte array objects to send to the data bus.</param>
    /// <param name="routingKey">The name of the route key with which you want to send events to the data bus.</param>
    /// <returns></returns>
    public static void AddEvents(this IQueueEventsBufferEngine service, IEnumerable<byte[]> objs, string routingKey)
    {
        foreach (var obj in objs)
            service.Add(new EventItem(obj, routingKey));
    }

    /// <summary>
    /// Add a byte array objects collection to send as event to the data bus.
    /// </summary>
    /// <param name="service">The <see cref="IQueueEventsBufferEngine"/> object.</param>
    /// <param name="objs">The list of byte array objects to send to the data bus.</param>
    /// <param name="routingKey">The name of the route key with which you want to send events to the data bus.</param>
    /// <returns></returns>
    public static void AddEvents(this IQueueEventsBufferEngine service, IEnumerable<string> objs, string routingKey)
    {
        foreach (var obj in objs)
            service.Add(new EventItem(Encoding.UTF8.GetBytes(obj).AsMemory(), routingKey));
    }
}
