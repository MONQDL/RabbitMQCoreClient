using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using RabbitMQCoreClient.Serializers;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace RabbitMQCoreClient.BatchQueueSender;

/// <summary>
/// Implementation of the stream data event store buffer.
/// </summary>
internal sealed class QueueBufferService : IQueueBufferService, IDisposable
{
    readonly Queue<EventItem> _buffer = new Queue<EventItem>();
    readonly object _syncRoot = new object();
    readonly Timer _timer;
    readonly int _sizeLimit;
    readonly TimeSpan _timeLimit;
    readonly IEventsWriter _writer;
    readonly IEventsHandler? _eventsHandler;
    readonly ILogger<QueueBufferService>? _log;
    int _count;
    Task _currentFlushTask = Task.CompletedTask;

    /// <inheritdoc />
    public IMessageSerializer Serializer { get; }

    const string ErrorWhileWritingEvents = "There was an error while writing events. Details: {ErrorMessage}";
    const string ErrorOnAfterWriteEvents = "There was an error execution OnAfterWriteEvents method. Details: {ErrorMessage}";
    const string ErrorOnWriteErrors = "There was an error execution OnWriteErrors method. Details: {ErrorMessage}";

    /// <summary>
    /// The implementation constructor of the event storage buffer.
    /// Creates a new instance of the class <see cref="QueueBufferService"/>.
    /// </summary>
    public QueueBufferService(IEventsWriter writer,
        int sizeLimit,
        TimeSpan timeLimit,
        IEventsHandler? eventsHandler,
        IRabbitMQCoreClientBuilder builder,
        ILogger<QueueBufferService>? log)
    {
        _log = log;
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _eventsHandler = eventsHandler;
        _sizeLimit = sizeLimit;
        _timeLimit = timeLimit;
        _timer = new Timer(_ => _ = FlushByTimerAsync(), null, Timeout.Infinite, Timeout.Infinite);
        Serializer = builder.Serializer ?? new SystemTextJsonMessageSerializer();
    }

    /// <inheritdoc />
    public void Add([NotNull] EventItem item)
    {
        lock (_syncRoot)
        {
            _buffer.Enqueue(item);
            _count++;

            if (_count == 1)
                _timer.Change(_timeLimit, Timeout.InfiniteTimeSpan);

            if (_count >= _sizeLimit)
                _currentFlushTask = FlushAsync();
        }
    }

    async Task FlushByTimerAsync()
    {
        EventItem[]? array = null;
        int count = 0;

        lock (_syncRoot)
        {
            if (_count == 0)
                return;
            (array, count) = ExtractItems();
        }

        await ProcessItemsAsync(array, count).ConfigureAwait(false);
    }

    async Task FlushAsync()
    {
        EventItem[]? array = null;
        int count = 0;

        lock (_syncRoot)
        {
            (array, count) = ExtractItems();
        }

        await ProcessItemsAsync(array, count).ConfigureAwait(false);
    }

    (EventItem[] Array, int Count) ExtractItems()
    {
        _timer.Change(Timeout.Infinite, Timeout.Infinite);
        int count = _buffer.Count;
        var array = ArrayPool<EventItem>.Shared.Rent(count);

        _buffer.CopyTo(array, 0);
        _buffer.Clear();
        _count = 0;

        return (array, count);
    }

    async Task ProcessItemsAsync(EventItem[] array, int count)
    {
        List<EventItem>? errorEvents = null;
        List<EventItem>? completedEvents = null;
        var sw = new Stopwatch();
        sw.Start();

        try
        {
            var batch = new ArraySegment<EventItem>(array, 0, count);
            var routeKeyGroups = GroupByKey(batch);
            var tasksWithData = new List<(Task Task, IEnumerable<EventItem> Events)>();
            var tasks = new List<Task>();
            foreach (var routeKeyGroup in routeKeyGroups)
            {
                var task = _writer.WriteBatch(routeKeyGroup.Items, routeKeyGroup.Key);
                tasks.Add(task);
                tasksWithData.Add((Task: task, Events: routeKeyGroup.Items.AsEnumerable()));
            }

            try
            {
                if (tasks.Count > 0)
                    await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch
            {
                // Ignore the single exception, as we'll handle everything below.
            }

            foreach (var tuple in tasksWithData)
            {
                var task = tuple.Task;
                var eventsGroup = tuple.Events;

                if (task.IsFaulted && task.Exception != null)
                {
                    errorEvents ??= new List<EventItem>();
                    foreach (var innerEx in task.Exception.InnerExceptions)
                    {
                        _log?.LogError(innerEx, ErrorWhileWritingEvents, innerEx.Message);
                        // Log exception
                        errorEvents.AddRange(eventsGroup);
                    }
                }
                else if (!task.IsFaulted && _eventsHandler != null)
                {
                    completedEvents ??= new List<EventItem>();
                    completedEvents.AddRange(eventsGroup);
                }
            }
        }
        finally
        {
            ArrayPool<EventItem>.Shared.Return(array);

            _log?.LogInformation("Buffer has written '{RecordsCount}' records to the database at '{ElapsedMilliseconds}' ms.",
                count, sw.ElapsedMilliseconds);
        }

        try
        {
            if (_eventsHandler != null)
                await _eventsHandler.OnAfterWriteEvents(completedEvents ?? Enumerable.Empty<EventItem>()).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _log?.LogError(e, ErrorOnAfterWriteEvents, e.Message);
        }

        try
        {
            if (_eventsHandler != null && errorEvents != null && errorEvents.Count > 0)
                await _eventsHandler.OnWriteErrors(errorEvents).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _log?.LogError(e, ErrorOnWriteErrors, e.Message);
        }
    }

    static IEnumerable<GroupData> GroupByKey(ArraySegment<EventItem> batch)
    {
        if (batch.Array is null)
            yield break;

        var groups = DictionaryPool<string, List<EventItem>>.Rent();
        try
        {
            for (int i = 0; i < batch.Count; i++)
            {
                var item = batch.Array[batch.Offset + i];
                if (!groups.TryGetValue(item.RoutingKey, out var list))
                {
                    list = ListPool<EventItem>.Rent();
                    groups[item.RoutingKey] = list;
                }
                list.Add(item);
            }

            foreach (var pair in groups)
            {
                yield return new GroupData(pair.Key, pair.Value.ToArray());
            }
        }
        finally
        {
            foreach (var list in groups.Values)
            {
                ListPool<EventItem>.Return(list);
            }
            DictionaryPool<string, List<EventItem>>.Return(groups);
        }
    }

    /// <inheritdoc />
    public async Task CompleteAsync()
    {
        Task flushTask;
        lock (_syncRoot)
        {
            flushTask = _currentFlushTask;
        }

        await flushTask.ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _timer?.Dispose();
        GC.SuppressFinalize(this);
    }

    readonly struct GroupData
    {
        public readonly string Key;
        public readonly EventItem[] Items;

        public GroupData(string key, EventItem[] items)
        {
            Key = key;
            Items = items;
        }
    }

    static class ListPool<T>
    {
        static readonly ObjectPool<List<T>> _pool =
            new DefaultObjectPool<List<T>>(new ListPolicy<T>());

        public static List<T> Rent() => _pool.Get();
        public static void Return(List<T> list) => _pool.Return(list);

        class ListPolicy<R> : PooledObjectPolicy<List<R>>
        {
            public override List<R> Create() => new List<R>();
            public override bool Return(List<R> list)
            {
                list.Clear();
                return true;
            }
        }
    }

    static class DictionaryPool<TKey, TValue>
        where TKey : notnull
    {
        static readonly ConcurrentBag<Dictionary<TKey, TValue>> _pool = new();

        public static Dictionary<TKey, TValue> Rent()
        {
            if (_pool.TryTake(out var dict))
            {
                return dict;
            }
            return [];
        }

        public static void Return(Dictionary<TKey, TValue> dict)
        {
            dict.Clear();
            _pool.Add(dict);
        }
    }
}
