using System;

namespace RabbitMQCoreClient.Models
{
    public class RabbitMessageEventArgs : EventArgs
    {
        /// <summary>
        /// The routing key used when the message was originally published.
        /// </summary>
        public string RoutingKey { get; private set; }

        /// <summary>
        /// Корреляционный Id, которые пробрасывается вместе с сообщением и может быть использован при выявлении цепочек логов.
        /// </summary>
        public string? CorrelationId { get; set; }

        public RabbitMessageEventArgs(string routingKey)
        {
            RoutingKey = routingKey;
        }
    }
}
