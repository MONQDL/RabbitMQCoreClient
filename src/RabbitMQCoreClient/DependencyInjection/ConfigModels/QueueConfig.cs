using System;
using System.Collections.Generic;

namespace RabbitMQCoreClient.DependencyInjection.ConfigModels
{
    /// <summary>
    /// Простая настраиваемая очередь сообщений.
    /// </summary>
    public class QueueConfig
    {
        string? _name;

        /// <summary>
        /// Название очереди сообщений.
        /// </summary>
        [Obsolete("Стоит перейти на использование Name")]
        public string QueueName { get; set; } = default!;

        /// <summary>
        /// Название очереди сообщений.
        /// </summary>
        public string Name
        {
            get => string.IsNullOrEmpty(_name) ? QueueName : _name;
            protected set => _name = value;
        }

        /// <summary>
        /// If true, the queue will be saved on disc.
        /// </summary>
        public bool Durable { get; set; } = true;

        /// <summary>
        /// If true, then the queue will be used by single service and will be deleted after client will disconnect.
        /// </summary>
        public bool Exclusive { get; set; } = false;

        /// <summary>
        /// If true, the queue will be automaticly deleted on client disconnect.
        /// </summary>
        public bool AutoDelete { get; set; } = false;

        /// <summary>
        /// Название точки обмена, в которую будут попадать сообщения, для которых был получен reject или nack.
        /// </summary>
        public string? DeadLetterExchange { get; set; }

        /// <summary>
        /// Список дополнительных параметров, которые будут использоваться при инициализации очереди.
        /// </summary>
        public IDictionary<string, object> Arguments { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Список ключей маршрутизации для очереди.
        /// </summary>
        public HashSet<string> RoutingKeys { get; set; } = new HashSet<string>();

        /// <summary>
        /// Список точек обмена, к которым привязывается очередь.
        /// </summary>
        public HashSet<string> Exchanges { get; set; } = new HashSet<string>();
    }
}
