using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQCoreClient.DependencyInjection;

/// <summary>
/// Describes the mode in which the service should start.
/// </summary>
public enum RunModes
{
    /// <summary>
    /// Run the service as both a publisher and a consumer.
    /// </summary>
    PublisherAndConsumer = 0,
    /// <summary>
    /// Run the service only as a publisher (no consuming logic) and no pushed channel open.
    /// </summary>
    PublisherOnly = 1
}
