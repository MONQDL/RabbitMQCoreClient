# RabbitMQ Client library for .net core applications with Dependency Injection support

The library allows you to quickly connect and get started with the RabbitMQ message broker.
The library serializes and deserializes messages to JSON using _System.Text.Json_ as default or can use custom serializers.
The library allows you to work with multiple queues, connected to various exchanges. It allows you to work with subscriptions.
The library implements a custom errored messages mechanism, using the TTL and the dead message queue.

## Installation

```powershell
Install-Package RabbitMQCoreClient
```

## MIGRATION from v6 to v7

See [v7 migration guide](v7-MIGRATION.md)

## Using the library

The library allows you to both send and receive messages. It makes possible to subscribe to named queues,
as well as creating short-lived queues to implement the Publish/Subscribe pattern.

### Sending messages

The library allows you to configure parameters both from the configuration file or through the Fluent interface.

##### An example of using a configuration file

*appsettings.json*
```json
{
  "HostName": "rabbit-1",
  "UserName": "user",
  "Password": "password",
  "Exchanges": [
    {
      "Name": "direct_exchange",
      "IsDefault": true
    }
  ]
}
```

*Program.cs - simple console application*

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQCoreClient;
using RabbitMQCoreClient.DependencyInjection;

Console.WriteLine("Simple console message publishing only example");

var config = new ConfigurationBuilder()
            .AddJsonFile($"appsettings.json", optional: false)
            .AddJsonFile($"appsettings.Development.json", optional: true)
            .Build();

var services = new ServiceCollection();
services.AddLogging();
services.AddSingleton(LoggerFactory.Create(x =>
{
    x.SetMinimumLevel(LogLevel.Trace);
    x.AddConsole();
}));

// Just for sending messages.
services.AddRabbitMQCoreClient(config.GetSection("RabbitMQ"));

var provider = services.BuildServiceProvider();

var publisher = provider.GetRequiredService<IQueueService>();

await publisher.ConnectAsync();

await publisher.SendAsync("""{ "foo": "bar" }""", "test_key");

```

*Program.cs - ASP.NET Core application*

```
using RabbitMQCoreClient;
using RabbitMQCoreClient.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Just for sending messages.
builder.Services
    .AddRabbitMQCoreClient(builder.Configuration.GetSection("RabbitMQ"));

var app = builder.Build();

// Configure the HTTP request pipeline.

app.MapPost("/send", async (IQueueService publisher) =>
{
    await publisher.SendAsync("""{ "foo": "bar" }""", "test_key");
    return Results.Ok();
});

app.Run();
```

More examples can be found at `samples` folder in this repository.

The `RabbitMQCoreClient.IQueueService` interface is responsible for sending messages.

In order to send a message, it is enough to get the interface `RabbitMQCoreClient.IQueueService` from DI
and use one of the methods of `SendAsync` or `SendBatchAsync`.

In this case, if you do not specify `exchange`, then the default exchange will be used (from the configuration),
if configured, otherwise you need to explicitly specify the `exchange` parameter.

#### TTL

Each time a message is sent to the message queue, a TTL is set. For new messages the default TTL is 5. 
Each time a message is re-sending to the queue, for example, due to an exception, the TTL is decreasing by 1.
The message will be sent to the dead message queue if the TTL drops to 0.

If you set the parameter `decreaseTtl = false` in the `SendAsync` methods, then the TTL will not be reduced accordingly, 
which can lead to an endless message processing cycle. `decreaseTtl` only can be used in methods with `BasicProperties` argument.

The default TTL setting can be defined in the configuration (see the Configuration section).

#### An example of using a client to send messages:

```csharp
// A sending mesages service that implements the IQueueService interface.
var queueService = serviceProvider.GetRequiredService<IQueueService>();

// Send one message to the queue.
var body = new SimpleObj { Name = "test sending" };
await queueService.SendAsync(body, "test_routing_key");

// Send the list of messages to the queue in batch.
var bodyList = Enumerable.Range(1, 10).Select(x => new SimpleObj { Name = $"test sending {x}" });
await queueService.SendBatchAsync(bodyList, "test_routing_key");
```

#### Buffer messages in memory and send them batch by timer or count

You can use this feature when you have to send many parallel small messages to the queue (for example from the ASP.NET requests).
The feature allows you to buffer that messages at the in-memory list and flush them at once using the `SendBatchAsync` method.

To use this feature register it at DI:

```csharp
using RabbitMQCoreClient.DependencyInjection;

...
services.AddRabbitMQCoreClient(config.GetSection("RabbitMQ"))
    .AddBatchQueueSender();
```

Instead of injecting the interface `RabbitMQCoreClient.IQueueService` inject `RabbitMQCoreClient.BatchQueueSender.IQueueBufferService`.
Then use methods `void AddEvent*()` to queue your messages. The methods are thread safe.

You can configure the flush options by Action or IConfiguration. Example of the configuration JSON:
```json
{
  "QueueFlushSettings": {
    "EventsFlushPeriodSec": 2,
    "EventsFlushCount": 500
  }
}
```

```csharp
using RabbitMQCoreClient.DependencyInjection;

...
services.AddRabbitMQCoreClient(config.GetSection("RabbitMQ"))
    .AddBatchQueueSender(configuration.GetSection("QueueFlushSettings"));
```

#### Extended buffer configuration

If you need you own implementation of sending events from buffer then implement the interface `IEventsWriter` and add it to DI after `.AddBatchQueueSender()`:

```csharp
builder.Services.AddTransient<IEventsWriter, CustomEventsWriter>();
```

Implementation example:

```csharp
namespace RabbitMQCoreClient.BatchQueueSender;

/// <summary>
/// Implementation of the service for sending events to the data bus.
/// </summary>
internal sealed class EventsWriter : IEventsWriter
{
    readonly IQueueService _queueService;

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
        if (!events.Any())
            return;

        await _queueService.SendBatchAsync(events.Select(x => x.Message), routingKey);
    }
}
```

You also can implement `IEventsHandler` to run you methods `OnAfterWriteEvents` or `OnWriteErrors`.
You must add you custom implementation to DI:

```csharp
builder.Services.AddTransient<IEventsHandler, CustomEventsHandler>();
```

##### Custom EventItem model

If you need to add custom properties to EventItem model and handle these properties in `IEventsWriter.WriteBatch`
you can inherit from `EventItem` and use `IEventsBufferEngine.Add`. There is a ready to use class `EventItemWithSourceObject`.
This class contains the source object on the basis of which an array of bytes will be calculated to be sent.
At `IEventsWriter.WriteBatch` cast to the class `EventItemWithSourceObject`.
Keep in mind that such a mechanism adds additional pressure to the GC.

```csharp
public async Task WriteBatch(IEnumerable<EventItem> events, string routingKey)
{
    ...
    foreach (EventItem item in events)
    {
        if (item is EventItemWithSourceObject eventWithSource)
        {
            // Working с item.Source
        }
    }
    ...
}
```

### Receiving and processing messages

By default `builder.Services.AddRabbitMQCoreClient(config.GetSection("RabbitMQ"))` adds support of publisher. 
If you need to consume messages from queues, you must configure consumer.

#### Configuring consumer

You can configure consummer by 2 methods - fluent and from configuration.

Fluent example:

```
builder.Services
    .AddRabbitMQCoreClient(opt => opt.Host = "localhost")
    .AddExchange("default")
    .AddConsumer()
    .AddHandler<Handler>("test_routing_key")
    .AddQueue("my-test-queue")
    .AddSubscription();
```

IConfiguration example:

```
builder.Services
    .AddRabbitMQCoreClientConsumer(builder.Configuration.GetSection("RabbitMQ"))
    .AddHandler<Handler>(["test_routing_key"], new ConsumerHandlerOptions
    {
        RetryKey = "test_routing_key_retry"
    })
    .AddHandler<Handler>(["test_routing_key_subscription"], new ConsumerHandlerOptions
    {
        RetryKey = "test_routing_key_retry"
    });
```

#### Processing messages with `IMessageHandler`
In the basic version, messages are received using the implementation of the interface `RabbitMQCoreClient.IMessageHandler`.

The interface requires the implementation of a message handling method `Task HandleMessage(string message, RabbitMessageEventArgs args)`,
and also the error message router: `ErrorMessageRouting`.

In order to specify which routing keys need to be processed by the handler, you need to configure the handler in RabbitMQCoreClient.

Example:

```csharp
public class RawHandler : IMessageHandler
{
    public ErrorMessageRouting ErrorMessageRouter => new ErrorMessageRouting();
    public ConsumerHandlerOptions? Options { get; set; }

    public Task HandleMessage(ReadOnlyMemory<byte> message, RabbitMessageEventArgs args)
    {
        Console.WriteLine(message);

        return Task.CompletedTask;
    }
}
```

Program.cs partial example

```
services
    .AddRabbitMQCoreClientConsumer(config.GetSection("RabbitMQ"))
    .AddBatchQueueSender() // If you want to send messages with inmemory buffering.
    .AddHandler<SimpleObjectHandler>("test_key");
```

**Note**: A message can only be processed by one handler. Although one handler can handle many messages with different routing keys.
This limitation is due to the routing of erroneous messages in the handler.

More examples can be found at `samples` folder in this repository.

#### `MessageHandlerJson<TModel>`

If your messages is JSON serialized, you can use an abstract class `RabbitMQCoreClient.MessageHandlerJson<TModel>`,
which itself deserializes the Json into the class model of the desired type.

**Note**: If you want to use this class, you must provide source generated JsonSerializerContext for you class.
If you want to use your own deserializer, then use your custom `IMessageHandler` implementation.

Usage example:

```csharp
internal sealed class SimpleObjectHandler : MessageHandlerJson<SimpleObj>
{
    readonly ILogger<SimpleObjectHandler> _logger;

    public SimpleObjectHandler(ILogger<SimpleObjectHandler> logger)
    {
        _logger = logger;
    }

    protected override JsonTypeInfo<SimpleObj> GetSerializerContext() => SimpleObjContext.Default.SimpleObj;

    protected override Task HandleMessage(SimpleObj message, RabbitMessageEventArgs args)
    {
        _logger.LogInformation("Incoming simple object name: {Name}", message.Name);

        return Task.CompletedTask;
    }

    protected override ValueTask OnParseError(string json, Exception e, RabbitMessageEventArgs args)
    {
        _logger.LogError(e, "Incoming message can't be deserialized. Error: {ErrorMessage}", e.Message);
        return base.OnParseError(json, e, args);
    }
}

public class SimpleObj
{
    public required string Name { get; set; }
}

[JsonSerializable(typeof(SimpleObj))]
public partial class SimpleObjContext : JsonSerializerContext
{
}
```

The `RabbitMQCoreClient.MessageHandlerJson <TModel>` class allows you to define behavior on serialization error
by overriding the `ValueTask OnParseError (string json, JsonException e, RabbitMessageEventArgs args)` method.

#### Routing messages

By default, if the handler throws any exception, then the message will be sent back to the queue with reduced TTL.
When processing messages, you often need to specify different behavior for different exceptions.
The `ErrorMessageRouting` message router is used to determine the client's behavior when throwing an exception.

There are 2 options for behavior:

- send the message back to the queue;
- send a message to the dead letter queue.

Note, the message will process by error message router if the message handler throws an Exception.
If the method succeeds normally, the message will be considered delivered.

Usage example:

```csharp
internal class Handler : MessageHandlerJson<SimpleObj>
{
    protected override JsonTypeInfo<SimpleObj> GetSerializerContext() => SimpleObjContext.Default.SimpleObj;

    protected override Task HandleMessage(SimpleObj message, RabbitMessageEventArgs args)
    {
        try
        {
            ProcessMessage(message);
        }
        catch (ArgumentException e) when (e.Message == "parser failed")
        {
            ErrorMessageRouter.MoveToDeadLetter();
            throw;
        }
        catch (Exception)
        {
            ErrorMessageRouter.MoveBackToQueue();
            throw;
        }

        return Task.CompletedTask;
    }

    void ProcessMessage(SimpleObj obj)
    {
        if (obj.Name != "my test name")
            throw new ArgumentException("parser failed");

        Console.WriteLine("It's all ok.");
    }
}
```

### Json Serializers

You can choose what serializer to use to *publish* messages with serialization. 
The library by default supports `System.Text.Json` serializer.

#### Custom publish serializer
You can make make your own custom serializer. To do that you must implement the `RabbitMQCoreClient.Serializers.IMessageSerializer` interface.

Example:

*CustomSerializer.cs*
```csharp
public class CustomMessageSerializer : IMessageSerializer
{
    public Newtonsoft.Json.JsonSerializerSettings Options { get; }

    static readonly Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver JsonResolver =
        new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver
        {
            NamingStrategy = new Newtonsoft.Json.Serialization.CamelCaseNamingStrategy
            {
                ProcessDictionaryKeys = true
            }
        };

    public CustomMessageSerializer(Action<Newtonsoft.Json.JsonSerializerSettings>? setupAction = null)
    {
        if (setupAction is null)
        {
            Options = new Newtonsoft.Json.JsonSerializerSettings() { ContractResolver = JsonResolver };
        }
        else
        {
            Options = new Newtonsoft.Json.JsonSerializerSettings();
            setupAction(Options);
        }
    }

    /// <inheritdoc />
    public string Serialize<TValue>(TValue value)
    {
        return Newtonsoft.Json.JsonConvert.SerializeObject(value, Options);
    }

    /// <inheritdoc />
    public TResult? Deserialize<TResult>(string value)
    {
        return Newtonsoft.Json.JsonConvert.DeserializeObject<TResult>(value, Options);
    }
}
```

Create extension method

```csharp
public static class CustomSerializerBuilderExtentions
{
    /// <summary>
    /// Use Custom serializer as default serializer for the RabbitMQ messages.
    /// </summary>
    public static IRabbitMQCoreClientBuilder AddCustomSerializer(this IRabbitMQCoreClientBuilder builder, Action<JsonSerializerSettings>? setupAction = null)
    {
        builder.Serializer = new NewtonsoftJsonMessageSerializer(setupAction);
        return builder;
    }

    /// <summary>
    /// Use Custom serializer as default serializer for the RabbitMQ messages.
    /// </summary>
    public static IRabbitMQCoreClientConsumerBuilder AddCustomSerializer(this IRabbitMQCoreClientConsumerBuilder builder, Action<JsonSerializerSettings>? setupAction = null)
    {
        builder.Builder.AddNewtonsoftJson(setupAction);
        return builder;
    }
}
```

Use the extension method at the configuration stage.

*Program.cs - console application*
```
services
    .AddRabbitMQCoreClient(config)
    .AddCustomSerializer();
```

#### Quorum queues at cluster environment

You can set option `"UseQuorumQueues": true` at root configuration level 
and `"UseQuorum": true` at queue configuration level. This option adds argument `"x-queue-type": "quorum"` on queue declaration
and can be used at the configured cluster environment.

### Configuration with file

Configuration can be done either through options or through configuration from `appsettings.json`.

The old legacy queue auto-registration format is still supported. But with limitations:

- Only one queue can be automatically registered. The queue is registered at the exchange point "Exchange".

#### SSL support

You can set options to configure SSL secured connection to the server. 
To enable the SSL connection you must set `"SslEnabled": true` option at root configuration level.

You can use ssl options to setup the SSL connection:

- __SslAcceptablePolicyErrors__ [optional] - set of TLS policy (peer verification) 
errors that are deemed acceptable. Default is `"None"`. 
You can supply multiple arguments separated by comma. For example: `"SslAcceptablePolicyErrors": "RemoteCertificateNotAvailable,RemoteCertificateNameMismatch"`.
Acceptable values:
  - None - no SSL policy errors.
  - RemoteCertificateNotAvailable - certificate not available.
  - RemoteCertificateNameMismatch - certificate name mismatch.
  - RemoteCertificateChainErrors - System.Security.Cryptography.X509Certificates.X509Chain.ChainStatus has returned a non empty array.
- __SslVersion__ [optional] - the TLS protocol version. 
The client will let the OS pick a suitable version by using value `"None"`.
If this option is unavailable on some environments or effectively disabled, 
e.g.see via app context, the client will attempt to fall back to TLSv1.2. The default is `"None"`. 
You can supply multiple arguments separated by comma. For example: `"SslVersion": "Ssl3,Tls13"`.

Acceptable values:
  - None - allows the operating system to choose the best protocol to use, and to block
protocols that are not secure. Unless your app has a specific reason not to,
you should use this field.
  - Ssl2 - specifies the SSL 2.0 protocol. SSL 2.0 has been superseded by the TLS protocol
and is provided for backward compatibility only.
  - Ssl3 - specifies the SSL 3.0 protocol. SSL 3.0 has been superseded by the TLS protocol
and is provided for backward compatibility only.
  - Tls - specifies the TLS 1.0 security protocol. The TLS protocol is defined in IETF
RFC 2246.
  - Tls11 - specifies the TLS 1.1 security protocol. The TLS protocol is defined in IETF
RFC 4346.
  - Tls12 - specifies the TLS 1.2 security protocol. The TLS protocol is defined in IETF
RFC 5246.
  - Tls13 - specifies the TLS 1.3 security protocol. The TLS protocol is defined in IETF
RFC 8446.
- __SslServerName__ [optional] - server's expected name.
This MUST match the Subject Alternative Name (SAN) or CN on the peer's (server's) leaf certificate,
otherwise the TLS connection will fail. Default is `""`.
- __SslCheckCertificateRevocation__ [optional] - attempts to check certificate revocation status. Default is `false`.
Set to true to check peer certificate for revocation.
- __SslCertPassphrase__ [optional] - the client certificate passphrase. Default is `""`.
- __SslCertPath__ [optional] - the path to client certificate. Default is `""`.

##### Configuration format

###### Full configuration example

```json
{
  "HostName": "rabbit-1",
  "UserName": "user",
  "Password": "password",
  "DefaultTtl": 5,
  "PrefetchCount": 1,
  "UseQuorumQueues": false, // Introduced in v5.1.0
  "SslEnabled": true, // Introduced in v5.3.0
  "SslAcceptablePolicyErrors": "None", // Introduced in v5.3.0
  "SslVersion": "None", // Introduced in v5.3.0
  "SslServerName": "serverName", // Introduced in v5.3.0
  "SslCheckCertificateRevocation": false, // Introduced in v5.3.0
  "SslCertPassphrase": "pass", // Introduced in v5.3.0
  "SslCertPath": "/certs", // Introduced in v5.3.0
  "MaxBodySize": 16777216, // Introduced in v6.1.1
  "Queues": [
    {
      "Name": "my_queue1",
      "RoutingKeys": [
        "event1",
        "my-messaeg"
      ],
      "Durable": true,
      "Exclusive": false,
      "AutoDelete": false,
      "DeadLetterExchange": "test_dead_letter",
      "UseQuorum": false, // Introduced in v5.1.0
      "Exchanges": [
        "direct_exchange"
      ],
      "Arguments": [
        {
          "param": "value"
        }
      ]
    }
  ],
  "Subscriptions": [
    {
      "RoutingKeys": [
        "event1",
        "my-messaeg"
      ],
      "DeadLetterExchange": "test_dead_letter",
      "UseQuorum": false, // Introduced in v5.1.0
      "Exchanges": [
        "direct_exchange"
      ],
      "Arguments": [
        {
          "param": "value"
        }
      ]
    }
  ],
  "Exchanges": [
    {
      "Name": "direct_exchange",
      "IsDefault": true,
      "Type": "direct",
      "Durable": true,
      "AutoDelete": false,
      "Arguments": [
        {
          "param": "value"
        }
      ]
    }
  ]
}
```

###### Reduced configuration example that is used on a daily basis

If `Exchanges` is not specified in the `Queues` section, then the queue will use the default exchange.
You can skip Queues or Subscriptions or both, if you do not have consumers of this types.

```json
{
  "HostName": "rabbit-1",
  "UserName": "user",
  "Password": "password",
  "Queues": [
    {
      "Name": "my_queue1",
      "RoutingKeys": ["primary-event", "hpsm-incident", "hpsm-maintenance"],
      "DeadLetterExchange": "test_dead_letter"
    }
  ],
  "Subscriptions": [
    {
      "RoutingKeys": [
        "event1",
        "my-messaeg"
      ],
      "DeadLetterExchange": "test_dead_letter"
    }
  ],
  "Exchanges": [
    {
      "Name": "test_smon_direct",
      "IsDefault": true
    }
  ]
}
```

###### An old configuration format that is still supported. The "Queue" will be bound to "Exchange".

As *default exchange* `Exchange` will be used.

```json
{
  "HostName": "rabbit-1",
  "UserName": "user",
  "Password": "password",
  "UseQuorum": false, // Introduced in v5.1.0
  "Queue": {
    "QueueName": "my_queue1",
    "RoutingKeys": ["event1", "my-messaeg"],
    "DeadLetterExchange": "test_dead_letter"
  },
  "Exchange": {
    "Name": "direct_exchange"
  },
  "Subscription": {
    "RoutingKeys": [
      "event1",
      "my-messaeg"
    ],
    "DeadLetterExchange": "test_dead_letter"
  }
}
```
