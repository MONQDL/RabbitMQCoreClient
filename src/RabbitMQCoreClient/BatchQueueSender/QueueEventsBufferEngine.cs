using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQCoreClient.BatchQueueSender.Exceptions;

namespace RabbitMQCoreClient.BatchQueueSender;

/// <summary>
/// Implementation of the stream data event store buffer.
/// </summary>
public sealed class QueueEventsBufferEngine : IQueueEventsBufferEngine, IDisposable
{
    readonly Timer _flushTimer;
    readonly List<QueueEventItem> _events = [];
    readonly IQueueEventsWriter _eventsWriter;
    readonly QueueBatchSenderOptions _engineOptions;
    readonly ILogger<QueueEventsBufferEngine> _logger;

    static readonly SemaphoreSlim _semaphore = new(1, 1);
    bool _disposedValue;

    /// <summary>
    /// Event storage buffer implementation constructor.
    /// Creates a new instance of the <see cref="QueueEventsBufferEngine"/> class.
    /// </summary>
    public QueueEventsBufferEngine(
        IOptions<QueueBatchSenderOptions> engineOptions,
        IQueueEventsWriter eventsWriter,
        ILogger<QueueEventsBufferEngine> logger)
    {
        if (engineOptions?.Value == null)
            throw new ArgumentNullException(nameof(engineOptions), $"{nameof(engineOptions)} is null.");

        _engineOptions = engineOptions.Value;
        _eventsWriter = eventsWriter;
        _logger = logger;

        _flushTimer = new Timer(async obj => await FlushTimerDelegate(), null,
            _engineOptions.EventsFlushPeriodSec * 1000,
            _engineOptions.EventsFlushPeriodSec * 1000);
    }

    /// <inheritdoc />
    public async Task AddEvent<T>(T @event, string routingKey)
    {
        if (@event is null)
            return;

        await _semaphore.WaitAsync();

        try
        {
            _events.Add(new QueueEventItem(@event, routingKey));
            if (_events.Count < _engineOptions.EventsFlushCount)
                return;

            await Flush();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task AddEvents<T>(IEnumerable<T> events, string routingKey)
    {
        await _semaphore.WaitAsync();

        try
        {
            _events.AddRange(events
                .Where(@event => @event is not null)
                .Select(@event => new QueueEventItem(@event!, routingKey))
                );

            if (_events.Count < _engineOptions.EventsFlushCount)
                return;

            await Flush();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    async Task FlushTimerDelegate()
    {
        await _semaphore.WaitAsync();
        try
        {
            await Flush();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    Task Flush()
    {
        if (_events.Count == 0)
            return Task.CompletedTask;

        var eventsCache = _events.ToArray();
        _events.Clear();

        return HandleEvents(eventsCache);
    }

    async Task HandleEvents(IEnumerable<QueueEventItem> streamDataEvents)
    {
        var routingGroups = streamDataEvents.GroupBy(x => x.RoutingKey);

        var tasks = new List<Task>();

        foreach (var routingGroup in routingGroups)
        {
            var itemsToSend = routingGroup.Select(val => val.Event).ToArray();
            tasks.Add(_eventsWriter.Write(itemsToSend, routingGroup.Key));
        }
        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while trying to write batch of data to Storage.");

            var exceptions = tasks
                .Where(t => t.Exception != null)
                .Select(t => t.Exception)
                .ToList();
            foreach (var aggregateException in exceptions)
            {
                if (aggregateException?.InnerExceptions?[0] is PersistingException persistException)
                {
                    var extendedError = $"Routing key: {persistException.RoutingKey}. Source: " +
                            string.Join(Environment.NewLine,
                            System.Text.Json.JsonSerializer.Serialize(persistException.Items));
                    _logger.LogDebug(extendedError);
                }
                // Unrecorded events are not sent anywhere. For the current implementation, this is not fatal.
            }
        }
    }

    void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _flushTimer?.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Place the cleanup code in the "Dispose(bool disposing)" method.
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
