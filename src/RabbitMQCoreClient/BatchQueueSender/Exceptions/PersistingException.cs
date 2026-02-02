namespace RabbitMQCoreClient.BatchQueueSender.Exceptions;

/// <summary>
/// Create new object of <see cref="PersistingException"/>.
/// </summary>
public sealed class PersistingException : Exception
{
    /// <summary>
    /// The data items.
    /// </summary>
    public IEnumerable<EventItem> Items { get; }

    /// <summary>
    /// The routing key of the queue bus.
    /// </summary>
    public string RoutingKey { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PersistingException" /> 
    /// class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, 
    /// or a null reference (<see langword="Nothing" /> in Visual Basic) if no inner exception is specified.
    /// <param name="items">The data items.</param>
    /// <param name="routingKey">The routing key of the queue bus.</param>
    /// </param>
    public PersistingException(string message,
        IEnumerable<EventItem> items,
        string routingKey,
        Exception innerException) : base(message, innerException)
    {
        Items = items;
        RoutingKey = routingKey;
    }
}
