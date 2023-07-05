﻿using RabbitMQCoreClient.Configuration.DependencyInjection.Options;
using RabbitMQCoreClient.Models;
using RabbitMQCoreClient.Serializers;
using System;
using System.Threading.Tasks;

namespace RabbitMQCoreClient
{
    /// <summary>
    /// The interface for the handler received from the message queue.
    /// </summary>
    public interface IMessageHandler
    {
        /// <summary>
        /// Process the message asynchronously.
        /// </summary>
        /// <param name="message">Input byte array from condumed by RabbitMQ queue.</param>
        /// <param name="args">The <see cref="RabbitMessageEventArgs"/> instance containing the message data.</param>
        Task HandleMessage(ReadOnlyMemory<byte> message, RabbitMessageEventArgs args);

        /// <summary>
        /// Instructions to the router in case of an exception while processing a message.
        /// </summary>
        ErrorMessageRouting ErrorMessageRouter { get; }

        /// <summary>
        /// Consumer handler options, that was used during configuration.
        /// </summary>
        ConsumerHandlerOptions? Options { get; set; }

        /// <summary>
        /// The default json serializer.
        /// </summary>
        IMessageSerializer Serializer { get; set; }
    }
}