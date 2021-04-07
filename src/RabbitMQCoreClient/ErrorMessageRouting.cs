using RabbitMQCoreClient.Models;

namespace RabbitMQCoreClient
{
    /// <summary>
    /// Методы маршрутизации входящего сообщения.
    /// </summary>
    public sealed class ErrorMessageRouting
    {
        /// <summary>
        /// Выбранный маршрут обработки.
        /// По умолчанию: Routes.SourceQueue.
        /// </summary>
        public Routes Route { get; private set; } = Routes.SourceQueue;

        /// <summary>
        /// Действия по изменению Ttl.
        /// По умолчанию: TtlActions.Decrease.
        /// </summary>
        public TtlActions TtlAction { get; set; } = TtlActions.Decrease;

        /// <summary>
        /// Выбрать машрут отправки сообщения.
        /// </summary>
        /// <param name="route">Маршрут.</param>
        public void MoveTo(Routes route) => Route = route;

        /// <summary>
        /// Отправить сообщение обратно в очередь.
        /// </summary>
        /// <param name="decreaseTtl">Если <c>true</c> то будет уменьшено ttl сообщения.</param>
        public void MoveBackToQueue(bool decreaseTtl = true)
        {
            Route = Routes.SourceQueue;
            TtlAction = decreaseTtl ? TtlActions.Decrease : TtlActions.DoNotChange;
        }

        /// <summary>
        /// Отправить сообщение в dead letter exchange.
        /// </summary>
        public void MoveToDeadLetter() => Route = Routes.DeadLetter;
    }
}
