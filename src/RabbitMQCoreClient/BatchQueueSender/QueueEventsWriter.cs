using Microsoft.Extensions.Logging;
using RabbitMQCoreClient.BatchQueueSender.Exceptions;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RabbitMQCoreClient.BatchQueueSender
{
    /// <summary>
    /// Implementation of the service for sending events to the data bus.
    /// </summary>
    public class QueueEventsWriter : IQueueEventsWriter
    {
        readonly ILogger<QueueEventsWriter> _logger;
        readonly IQueueService _queueService;

        long _writtenCount;
        long _avgWriteTimeMs;

        public QueueEventsWriter(
            IQueueService queueService,
            ILogger<QueueEventsWriter> logger)
        {
            _queueService = queueService;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task Write(object[] items, string routingKey)
        {
            if (items.Length == 0)
                return;

            _logger.LogInformation("Start writing {rowsCount} data rows from the buffer.", items.Length);

            var sw = new Stopwatch();
            sw.Start();

            try
            {
                await _queueService.SendBatchAsync(items, routingKey, new System.Text.Json.JsonSerializerOptions());
            }
            catch (Exception e)
            {
                throw new PersistingException("Error while persisting data", items, routingKey, e);
            }

            sw.Stop();

            _logger.LogInformation("Buffer has sent {rowsCount} rows to the queue bus at {elapsedMilliseconds} ms.",
                items.Length, sw.ElapsedMilliseconds);

            _writtenCount += items.Length;

            if (_avgWriteTimeMs == 0)
                _avgWriteTimeMs = sw.ElapsedMilliseconds;
            else
                _avgWriteTimeMs = (sw.ElapsedMilliseconds + _avgWriteTimeMs) / 2;

            _logger.LogInformation("From start of the service {rowsCount} total rows has been sent to the queue bus. " +
                "The average sending time per row is {avgWriteTime} ms.",
                _writtenCount, _avgWriteTimeMs);
        }
    }
}
