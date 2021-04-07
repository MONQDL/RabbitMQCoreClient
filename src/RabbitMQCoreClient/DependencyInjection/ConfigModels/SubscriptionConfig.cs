using System.Collections.Generic;

namespace RabbitMQCoreClient.DependencyInjection.ConfigModels
{
    /// <summary>
    /// Очередь сообщения для подписки на события.
    /// Очередь имеет автоматическое наименование. При отсоединении клиента от сервера очередь автоматически удаляется.
    /// </summary>
    public class SubscriptionConfig
    {
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
