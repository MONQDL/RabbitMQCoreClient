using RabbitMQCoreClient.Diagnostics;
using System.Diagnostics;

namespace RabbitMQCoreClient.BatchQueueSender;

/// <summary>
/// Implementation of the service for sending events to the data bus.
/// </summary>
internal sealed class EventsWriter : IEventsWriter
{
    readonly IQueueService _queueService;
    const int PublishBatchSize = 128;
    const string BatchSendActivity = "BufferBatchSend";

    /// <summary>
    /// Create new object of <see cref="EventsWriter"/>.
    /// </summary>
    /// <param name="queueService">Queue publisher.</param>
    public EventsWriter(IQueueService queueService)
    {
        _queueService = queueService;
    }

    /// <inheritdoc />
    public async Task WriteBatch(IEnumerable<EventItem> events, string routingKey)
    {
        var items = events as EventItem[] ?? events.ToArray();
        if (items.Length == 0)
            return;

        var publishes = new List<(ValueTask Task, Activity? Activity)>();

        try
        {
            foreach (var item in items)
            {
                Activity? activity;
                if (item.ParentContext != default)
                {
                    activity = RabbitMqActivitySource.Source.StartActivity(BatchSendActivity, 
                        kind: ActivityKind.Producer, 
                        parentContext: item.ParentContext);
                }
                else
                    activity = RabbitMqActivitySource.Source.StartActivity(BatchSendActivity,
                        kind: ActivityKind.Producer);

                activity?.Start();

                var task = _queueService.SendAsync(item.Message, routingKey);
                publishes.Add((task, activity));

                if (publishes.Count >= PublishBatchSize)
                    await AwaitPublishes(publishes);
            }

            await AwaitPublishes(publishes);
        }
        finally
        {
            foreach (var (_, activity) in publishes)
                activity?.Dispose();
        }
    }

    static async Task AwaitPublishes(List<(ValueTask Task, Activity? Activity)> publishes)
    {
        Exception? firstException = null;
        for (int i = 0; i < publishes.Count; i++)
        {
            try
            {
                await publishes[i].Task;
            }
            catch (Exception ex)
            {
                firstException ??= ex;
            }
            finally
            {
                publishes[i].Activity?.Stop();
            }
        }
        publishes.Clear();

        if (firstException != null)
            throw firstException;
    }
}
