using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQCoreClient.Exceptions;
using System;
using System.Collections.Generic;

namespace RabbitMQCoreClient.Configuration.DependencyInjection.Options
{
    /// <summary>
    /// Параметры, которые будут применены к очереди сообщений.
    /// </summary>
    public abstract class QueueBase
    {
        protected QueueBase(string? name, bool durable, bool exclusive, bool autoDelete)
        {
            Name = name;
            Durable = durable;
            Exclusive = exclusive;
            AutoDelete = autoDelete;
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
        /// Название точки обмена, в которую будут попадать сообщения, для которых был получен reject или nack.
        /// </summary>
        public virtual string? DeadLetterExchange { get; set; }

        /// <summary>
        /// Список дополнительных параметров, которые будут использоваться при инициализации очереди.
        /// </summary>
        public virtual IDictionary<string, object> Arguments { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Список ключей маршрутизации для очереди.
        /// </summary>
        public virtual HashSet<string> RoutingKeys { get; set; } = new HashSet<string>();

        /// <summary>
        /// Список точек обмена, к которым привязывается очередь.
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

            var declaredQueue = channel.QueueDeclare(queue: Name ?? string.Empty,
                    durable: Durable,
                    exclusive: Exclusive,
                    autoDelete: AutoDelete,
                    arguments: Arguments);

            if (declaredQueue is null)
                throw new QueueBindException("Queue is not properly binded.");

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
            if (RoutingKeys.Count > 0)
                foreach (var route in RoutingKeys)
                    channel.QueueBind(
                        queue: declaredQueue.QueueName,
                        exchange: exchangeName,
                        routingKey: route
                    );
            else
                channel.QueueBind(
                    queue: declaredQueue.QueueName,
                    exchange: exchangeName,
                    routingKey: declaredQueue.QueueName
                );
        }
    }
}
