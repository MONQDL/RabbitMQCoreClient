using RabbitMQCoreClient.Models;

namespace RabbitMQCoreClient;

/// <summary>
/// Incoming message routing methods.
/// </summary>
public sealed class ErrorMessageRouting
{
    /// <summary>
    /// Selected processing route.
    /// Default: Routes.SourceQueue.
    /// </summary>
    public Routes Route { get; private set; } = Routes.SourceQueue;

    /// <summary>
    /// Actions to change Ttl.
    /// Default: TtlActions.Decrease.
    /// </summary>
    public TtlActions TtlAction { get; set; } = TtlActions.Decrease;

    /// <summary>
    /// Select the route for sending the message.
    /// </summary>
    /// <param name="route">Маршрут.</param>
    public void MoveTo(Routes route) => Route = route;

    /// <summary>
    /// Send the message back to the queue.
    /// </summary>
    /// <param name="decreaseTtl">If <c>true</c> then ttl messages will be minified.</param>
    public void MoveBackToQueue(bool decreaseTtl = true)
    {
        Route = Routes.SourceQueue;
        TtlAction = decreaseTtl ? TtlActions.Decrease : TtlActions.DoNotChange;
    }

    /// <summary>
    /// Send a message to dead letter exchange.
    /// </summary>
    public void MoveToDeadLetter() => Route = Routes.DeadLetter;
}
