using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQCoreClient.Exceptions;
using System;
using System.Collections.Generic;

namespace RabbitMQCoreClient.Configuration.DependencyInjection.Options
{
    /// <summary>
    /// Options to be applied to the message queue.
    /// </summary>
    public abstract class QueueBase
    {
        protected QueueBase(string? name, bool durable, bool exclusive, bool autoDelete, bool useQuorum)
        {
            Name = name;
            Durable = durable;
            Exclusive = exclusive;
            AutoDelete = autoDelete;
            UseQuorum = useQuorum;
        }

        /// <summary>
        /// The queue Name. If null, then the name will be automaticly choosen.
        /// </summary>
        public virtual string? Name { get; protected set; }

        /// <summary>
        /// If true, the queue will be saved on disc.
        /// </summary>
        public virtual bool Durable { get; protected set; }

        /// <summary>
        /// If true, then the queue will be used by single service and will be deleted after client will disconnect.
        /// </summary>
        public virtual bool Exclusive { get; protected set; }

        /// <summary>
        /// If true, the queue will be automaticly deleted on client disconnect.
        /// </summary>
        public virtual bool AutoDelete { get; protected set; }

        /// <summary>
        /// The name of the exchange point that will receive messages for which a reject or nack was received.
        /// </summary>
        public virtual string? DeadLetterExchange { get; set; }

        /// <summary>
        /// While creating the queue use parameter "x-queue-type": "quorum".
        /// </summary>
        public virtual bool UseQuorum { get; set; } = false;

        /// <summary>
        /// List of additional parameters that will be used when initializing the queue.
        /// </summary>
        public virtual IDictionary<string, object> Arguments { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// ist of routing keys for the queue.
        /// </summary>
        public virtual HashSet<string> RoutingKeys { get; set; } = new HashSet<string>();

        /// <summary>
        /// The list of exchange points to which the queue is bound.
        /// </summary>
        public virtual HashSet<string> Exchanges { get; set; } = new HashSet<string>();

        /// <summary>
        /// Declare the queue on <see cref="Exchanges"/> and start consuming messages.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="consumer"></param>
        public virtual void StartQueue(IModel channel, AsyncEventingBasicConsumer consumer)
        {
            if (!string.IsNullOrWhiteSpace(DeadLetterExchange)
                && !Arguments.ContainsKey(AppConstants.RabbitMQHeaders.DeadLetterExchangeHeader))
                Arguments.Add(AppConstants.RabbitMQHeaders.DeadLetterExchangeHeader, DeadLetterExchange);

            if (UseQuorum && !Arguments.ContainsKey(AppConstants.RabbitMQHeaders.QueueTypeHeader))
                Arguments.Add(AppConstants.RabbitMQHeaders.QueueTypeHeader, "quorum");

            var declaredQueue = channel.QueueDeclare(queue: Name ?? string.Empty,
                    durable: Durable,
                    exclusive: Exclusive,
                    autoDelete: AutoDelete,
                    arguments: Arguments);

            if (declaredQueue is null)
                throw new QueueBindException("Queue is not properly binded.");

            if (RoutingKeys.Count > 0)
                foreach (var exchangeName in Exchanges)
                {
                    BindToExchange(channel, declaredQueue, exchangeName);
                }

            channel.BasicConsume(queue: declaredQueue.QueueName,
                autoAck: false,
                consumer: consumer,
                consumerTag: $"amq.{declaredQueue.QueueName}.{Guid.NewGuid()}"
                );
        }

        void BindToExchange(IModel channel, QueueDeclareOk declaredQueue, string exchangeName)
        {
            foreach (var route in RoutingKeys)
                channel.QueueBind(
                    queue: declaredQueue.QueueName,
                    exchange: exchangeName,
                    routingKey: route
                );
        }
    }
}
