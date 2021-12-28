using System;
using System.Collections.Generic;

namespace RabbitMQCoreClient.DependencyInjection.ConfigModels
{
    /// <summary>
    /// Simple custom message queue.
    /// </summary>
    public class QueueConfig
    {
        string? _name;

        /// <summary>
        /// The name of the message queue.
        /// </summary>
        [Obsolete("It is worth switching to using Name.")]
        public string QueueName { get; set; } = default!;

        /// <summary>
        /// The name of the message queue.
        /// </summary>
        public string Name
        {
            get => string.IsNullOrEmpty(_name) ? QueueName : _name;
            set => _name = value;
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
        /// The name of the exchange point that will receive messages for which a reject or nack was received.
        /// </summary>
        public string? DeadLetterExchange { get; set; }

        /// <summary>
        /// List of additional parameters that will be used when initializing the queue.
        /// </summary>
        public IDictionary<string, object> Arguments { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// List of routing keys for the queue.
        /// </summary>
        public HashSet<string> RoutingKeys { get; set; } = new HashSet<string>();

        /// <summary>
        /// The list of exchange points to which the queue is bound.
        /// </summary>
        public HashSet<string> Exchanges { get; set; } = new HashSet<string>();
    }
}
