# RabbitMQ Client library for .net core applications with Dependency Injection support

Library Version: v5

The library allows you to quickly connect and get started with the RabbitMQ message broker.
The library serializes and deserializes messages to JSON using _Newtonsoft.Json_ as default or _System.Text.Json_. 
The library allows you to work with multiple queues, connected to various exchanges. It allows you to work with subscriptions.
The library implements a custom errored messages mechanism, using the TTL and the dead message queue.

## Installation

```powershell
Install-Package RabbitMQCoreClient
```

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

*Program.cs - console application*
```
class Program
{
    static async Task Main(string[] args)
    {
        var config = new ConfigurationBuilder()
                    .AddJsonFile($"appsettings.json", optional: false)
                    .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(LoggerFactory.Create(x =>
        {
            x.SetMinimumLevel(LogLevel.Trace);
            x.AddConsole();
        }));

        // Just for sending messages.
        services
            .AddRabbitMQCoreClient(config);
    }
}
```

*Startup.cs - ASP.NET Core application*

```
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        ...

        // Just for sending messages.
        services
            .AddRabbitMQCoreClient(config);
    }
}
```

The `RabbitMQCoreClient.IQueueService` interface is responsible for sending messages.

In order to send a message, it is enough to get the interface `RabbitMQCoreClient.IQueueService` from DI
and use one of the following methods

```csharp
ValueTask SendAsync<T>(T obj, string routingKey, string exchange = default, bool decreaseTtl = true, string correlationId = default);
ValueTask SendJsonAsync(string json, string routingKey, string exchange = default, bool decreaseTtl = true, string correlationId = default);
ValueTask SendAsync(byte[] obj, IBasicProperties props, string routingKey, string exchange, bool decreaseTtl = true, string correlationId = default);

// Batch sending
ValueTask SendBatchAsync<T>(IEnumerable<T> objs, string routingKey, string exchange = default, bool decreaseTtl = true, string correlationId = default);
ValueTask SendJsonBatchAsync(IEnumerable<string> serializedJsonList, string routingKey, string exchange = default, bool decreaseTtl = true, string correlationId = default);
ValueTask SendBatchAsync(IEnumerable<(byte[] Body, IBasicProperties Props)> objs, string routingKey, string exchange, bool decreaseTtl = true, string correlationId = default);
```

In this case, if you do not specify `exchange`, then the default exchange will be used (from the configuration),
if configured, otherwise you need to explicitly specify the `exchange` parameter.

#### TTL

Each time a message is sent to the message queue, a TTL is set. For new messages the default TTL is 5. 
Each time a message is re-sending to the queue, for example, due to an exception, the TTL is decreasing by 1.
The message will be sent to the dead message queue if the TTL drops to 0.

If you set the parameter `decreaseTtl = false` in the `SendAsync` methods, then the TTL will not be reduced accordingly, 
which can lead to an endless message processing cycle.

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

#### Buffer messages in memory and send them at separate thread
From the version v5.1.0 there was introduced a new mechanic of the sending messages using separate thread. 
You can use this feature when you have to send many parallel small messages to the queue (for example from the ASP.NET requests).
The feature allows you to buffer that messages at the inmemory list and flush them at once using the `SendBatchAsync` method.

To use this feature register it at DI:

```csharp
using RabbitMQCoreClient.BatchQueueSender.DependencyInjection;

...
services.AddBatchQueueSender();
```

Instead of injecting the interface `RabbitMQCoreClient.IQueueService` inject `RabbitMQCoreClient.BatchQueueSender.IQueueEventsBufferEngine`.
Then use methods to queue your messages. The methods are thread safe.
```csharp
Task AddEvent<T>(T @event, string routingKey);
Task AddEvent<T>(IEnumerable<T> events, string routingKey);
```

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
using RabbitMQCoreClient.BatchQueueSender.DependencyInjection;

...
services.AddBatchQueueSender(configuration.GetSection("QueueFlushSettings"));
```

### Receiving and processing messages

##### Console application

```
class Program
{
    static readonly AutoResetEvent _closing = new AutoResetEvent(false);

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(LoggerFactory.Create(x =>
        {
            x.SetMinimumLevel(LogLevel.Trace);
            x.AddConsole();
        }));

        // For sending and consuming messages full.
        services
            .AddRabbitMQCoreClient(opt => opt.Host = "localhost")
            .AddExchange("default")
            .AddConsumer()
            .AddHandler<Handler>("test_routing_key")
            .AddQueue("my-test-queue")
            .AddSubscription();

        var serviceProvider = services.BuildServiceProvider();
        var consumer = serviceProvider.GetRequiredService<IQueueConsumer>();
        consumer.Start();

        var body = new SimpleObj { Name = "test sending" };
        await queueService.SendAsync(body, "test_routing_key");

        _closing.WaitOne();
        Environment.Exit(0);
    }
}
```

`_closing.WaitOne ();` is used to prevent the program from terminating immediately after starting.

The `.Start ();` method does not block the main thread.

##### ASP.NET Core 3.1+
```
public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        services
            .AddRabbitMQCoreClient(opt => opt.Host = "localhost")
            .AddExchange("default")
            .AddConsumer()
            .AddHandler<Handler>("test_routing_key")
            .AddQueue("my-test-queue");
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime)
    {
        app.StartRabbitMqCore(lifetime);

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
```

#### `IMessageHandler`
In the basic version, messages are received using the implementation of the interface `RabbitMQCoreClient.IMessageHandler`.

The interface requires the implementation of a message handling method `Task HandleMessage(string message, RabbitMessageEventArgs args)`,
and also the error message router: `ErrorMessageRouting`.

In order to specify which routing keys need to be processed by the handler, you need to configure the handler in RabbitMQCoreClient.

Example:

```csharp
public class RawHandler : IMessageHandler
{
    public ErrorMessageRouting ErrorMessageRouter => new ErrorMessageRouting();
    public ConsumerHandlerOptions Options { get; set; }
    public IMessageSerializer Serializer { get; set; }

    public Task HandleMessage(string message, RabbitMessageEventArgs args)
    {
        Console.WriteLine(message);

        return Task.CompletedTask;
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        services
            .AddRabbitMQCoreClientConsumer(config)
            .AddHandler<RawHandler>("test_routing_key");

        var serviceProvider = services.BuildServiceProvider();
        var consumer = serviceProvider.GetRequiredService<IQueueConsumer>();
        consumer.Start();
    }
}
```

**Note**: A message can only be processed by one handler. Although one handler can handle many messages with different routing keys.
This limitation is due to the routing of erroneous messages in the handler.

#### `MessageHandlerJson<TModel>`

Since messages in this client are serialized in Json, the interface implementation has been added as an abstract class `RabbitMQCoreClient.MessageHandlerJson<TModel>`,
which itself deserializes the Json into the model of the desired type. Usage example:

```csharp
public class Handler : MessageHandlerJson<SimpleObj>
{
    protected override Task HandleMessage(SimpleObj message, RabbitMessageEventArgs args)
    {
        Console.WriteLine(JsonConvert.SerializeObject(message));
        return Task.CompletedTask;
    }

    protected override ValueTask OnParseError(string json, Exception e, RabbitMessageEventArgs args)
    {
        Console.WriteLine(e.Message);
        return base.OnParseError(json, e, args);
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        services
            .AddRabbitMQCoreClientConsumer(config)
            .AddHandler<RawHandler>("test_routing_key");

        var serviceProvider = services.BuildServiceProvider();
        var consumer = serviceProvider.GetRequiredService<IQueueConsumer>();
        consumer.Start();
    }
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
public class Handler : MessageHandlerJson<SimpleObj>
{
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

You can choose what serializer to use. The library supports `System.Text.Json` or `Newtonsoft.Json` serializers.
To configure the serializer for the sender and consumer you can call `AddNewtonsoftJson()` or `AddSystemTextJson()` method at the configuration stage.

Example
*Program.cs - console application*
```
class Program
{
    static async Task Main(string[] args)
    {
        var config = new ConfigurationBuilder()
                    .AddJsonFile($"appsettings.json", optional: false)
                    .Build();

        var services = new ServiceCollection();

        services
            .AddRabbitMQCoreClient(config)
            .AddSystemTextJson();
    }
}
```

The default serializer is set to `Newtonsoft.Json` due to heavy code migrations in the existing code base that uses the library.

If you want to use different serializers for different message handlers that you can set CustomSerializer at the Handler configuration stage.

Example

Example
*Program.cs - console application*
```
class Program
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        services
            .AddRabbitMQCoreClientConsumer(config)
            .AddSystemTextJson()
            .AddHandler<RawHandler>("test_routing_key", new ConsumerHandlerOptions
                {
                    CustomSerializer = new NewtonsoftJsonMessageSerializer()
                }));

        var serviceProvider = services.BuildServiceProvider();
        var consumer = serviceProvider.GetRequiredService<IQueueConsumer>();
        consumer.Start();
    }
}
```

#### Custom serializer
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
class Program
{
    static async Task Main(string[] args)
    {
        var config = new ConfigurationBuilder()
                    .AddJsonFile($"appsettings.json", optional: false)
                    .Build();

        var services = new ServiceCollection();

        services
            .AddRabbitMQCoreClient(config)
            .AddCustomSerializer();
    }
}
```

#### Quorum queues at cluster environment
Started from v5.1.0 you can set option `"UseQuorumQueues": true` at root configuration level 
and `"UseQuorum": true` at queue configuration level. This option adds argument `"x-queue-type": "quorum"` on queue declaration
and can be used at the configured cluster environment.

### Configuration with file

Configuration can be done either through options or through configuration from appsettings.json.

In version 4.0 of the library, the old (<= v3) queue auto-registration format is still supported. But with limitations:

- Only one queue can be automatically registered. The queue is registered at the exchange point "Exchange".

#### SSL support
Started from v5.2.0 you can set options to configure SSL secured connection to the server. 
To enable the SSL connection you must set `"SslEnabled": true` option at root configuration level.

You can use ssl options to setup the SSL connection:

- __SslAcceptablePolicyErrors__ [optional] - set of TLS policy (peer verification) 
errors that are deemed acceptable. Default is `"None"`. Acceptable values:
  - None - no SSL policy errors.
  - RemoteCertificateNotAvailable - certificate not available.
  - RemoteCertificateNameMismatch - certificate name mismatch.
  - RemoteCertificateChainErrors - System.Security.Cryptography.X509Certificates.X509Chain.ChainStatus has returned a non empty array.
- __SslVersion__ [optional] - the TLS protocol version. 
The client will let the OS pick a suitable version by using value `"None"`.
If this option is unavailable on somne environments or effectively disabled, 
e.g.see via app context, the client will attempt to fall backto TLSv1.2. The default is `"None"`. Acceptable values:
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

###### Full configuration:

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

###### Reduced configuration that is used on a daily basis

If `Exchanges` is not specified in the `Queues` section, then the queue will use the default exchange.

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
