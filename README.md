# RabbitMQ Client library for .net core applications

Library Version: v4

## Change Log

See CHANGELOG.md

*English*

The library allows you to quickly connect and get started with the RabbitMQ message broker.
The library serializes and deserializes messages to JSON using Newtonsoft.Json. The library allows you to work with multiple queues,
connected to various exchanges. It allows you to work with subscriptions.
The library implements a custom errored messages mechanism, using the TTL and the dead message queue.

## Installation

```powershell
Install-Package RabbitMQCoreClient
```

## Using the library

The library allows you to both send and receive messages. The library makes it possible to subscribe to named queues,
as well as creating short-lived queues to implement the Publish / Subscribe pattern.

### Sending messages

The library allows you to configure parameters both from the configuration file, there and through the Fluent interface.

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

Each time a message is sent to the message queue, a TTL is set. For new messages the default TTL is 5. Each time a message is re-sending to the queue, for example, due to an exception, the TTL is decreasing by 1.
The message will be sent to the dead message queue if the TTL drops to 0.

If you set the parameter `decreaseTtl = false` in the `SendAsync` methods, then the TTL will not be reduced accordingly, which can lead to an endless message processing cycle.

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

The interface requires the implementation of a message handling method `Task HandleMessage (string message, RabbitMessageEventArgs args)`,
and also the error message router: `ErrorMessageRouting`.

In order to specify which routing keys need to be processed by the handler, you need to configure the handler in RabbitMQCoreClient.

Example:

```csharp
public class RawHandler : IMessageHandler
{
    public ErrorMessageRouting ErrorMessageRouter => new ErrorMessageRouting();

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

Since messages in this client are serialized in Json, the interface implementation has been added as an abstract class `RabbitMQCoreClient.MessageHandlerJson <TModel>`,
which itself deserializes the Json into the model of the desired type. Usage example:

```csharp
public class Handler : MessageHandlerJson<SimpleObj>
{
    protected override Task HandleMessage(SimpleObj message, RabbitMessageEventArgs args)
    {
        Console.WriteLine(JsonConvert.SerializeObject(message));
        return Task.CompletedTask;
    }

    protected override ValueTask OnParseError(string json, JsonException e, RabbitMessageEventArgs args)
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

### Configuration with file

Configuration can be done either through options or through configuration from appsettings.json.

In version 4.0 of the library, the old (<= v3) queue auto-registration format is still supported. But with limitations:

- Only one queue can be automatically registered. The queue is registered at the exchange point "Exchange".

##### Configuration format

###### Full configuration:

```json
{
  "HostName": "rabbit-1",
  "UserName": "user",
  "Password": "password",
  "DefaultTtl": 5,
  "PrefetchCount": 1,
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

---

*Russian*

Библиотека дает возможность быстро подключиться и начать работу с брокером сообщений RabbitMQ.
Библиотека сериализует и десериализует сообщения в JSON с применением Newtonsoft.Json. Библиотека позволяет работать с множеством очередей, 
подключенных к различным точкам обмена (exchange). Позволяет работать с подписками.
В библиотеке реалдизован кастомный механизм сообщений, которые в процессе обработки вызывают исключение с помощью механизма TTL и очереди мертвых сообщений.

## Установка

```powershell
Install-Package RabbitMQCoreClient
```

## Использование

Библиотека позволяет как отправлять, так и получать сообщения. Библиотека дает возможность подписываться на именованные очереди, 
так же как и создавать короткоживующе очереди для реализации паттерна Publish/Subscribe.

### Отправка сообщений

Библиотека позволяет конфигурировать параметры как из конфигурационного файла, там и через Fluent интерфейс.

##### Пример использования конфигурационного файла

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

*Program.cs - консольная программа*
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

*Startup.cs - ASP.NET Core*

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

За отправку сообщений отвечает интерфейс `RabbitMQCoreClient.IQueueService`.

Для того, чтобы отправить сообщение достаточно получить из DI интерфейс `RabbitMQCoreClient.IQueueService`
и воспользоваться одним из методов

```csharp
ValueTask SendAsync<T>(T obj, string routingKey, string exchange = default, bool decreaseTtl = true, string correlationId = default);
ValueTask SendJsonAsync(string json, string routingKey, string exchange = default, bool decreaseTtl = true, string correlationId = default);
ValueTask SendAsync(byte[] obj, IBasicProperties props, string routingKey, string exchange, bool decreaseTtl = true, string correlationId = default);
# Пакетная отправка.
ValueTask SendBatchAsync<T>(IEnumerable<T> objs, string routingKey, string exchange = default, bool decreaseTtl = true, string correlationId = default);
ValueTask SendJsonBatchAsync(IEnumerable<string> serializedJsonList, string routingKey, string exchange = default, bool decreaseTtl = true, string correlationId = default);
ValueTask SendBatchAsync(IEnumerable<(byte[] Body, IBasicProperties Props)> objs, string routingKey, string exchange, bool decreaseTtl = true, string correlationId = default);
```

При этом если не указывать `exchange`, то будет использована точка обмена по умолчанию из конфигурации, если сконфигурирована, иначе требуется явным образом указать параметр `exchange`.

#### TTL

При каждой отправке сообщения в очередь для сообщений выставляется TTL. По умолчанию для новых сообщений TTL=5. Каждый раз при повторной отправке сообщения в очередь, например, из-за исключения, TTL уменьшается на 1.
Сообщение будет отправлено в очередь мертвых сообщений если TTL опустится до 0.

Если задать параметр `decreaseTtl = false` в методах SendAsync, то TTL соответственно не будет уменьшен, что может привести к бесконечному циклу обработки сообщений.

Настройку TTL, по умолчанию можно определить в конфигурации (см. раздел Конфигурация).

#### Пример использования клиента для отправки сообщений:

```csharp
// Сервис отправки, реализующий интерфейс IQueueService.
var queueService = serviceProvider.GetRequiredService<IQueueService>();

// Отправки одно сообщение в очередь.
var body = new SimpleObj { Name = "test sending" };
await queueService.SendAsync(body, "test_routing_key");

// Отправить список сообщений в очередь пакетно.
var bodyList = Enumerable.Range(1, 10).Select(x => new SimpleObj { Name = $"test sending {x}" });
await queueService.SendBatchAsync(bodyList, "test_routing_key");
```

### Прием и обработка сообщений

#### Подключение

##### Консоль

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

`_closing.WaitOne();` используется для того, чтобы программа не завершилась сразу же после старта.

Метод `.Start();` не блокирует основной поток.

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
В базовом варианте прием сообщений осуществляется с помощью реализации интерфейса `RabbitMQCoreClient.IMessageHandler`.

Интерфейс требует определения метода обработки сообщения `Task HandleMessage(string message, RabbitMessageEventArgs args)`,
А также маршрутизатора ошибочных сообщений: `ErrorMessageRouting`.

Для того, чтобы указать какие ключи маршрутизации требуется обрабатывать обработчиком, требуется сконфигурировать обработчик в RabbitMQCoreClient.

Пример:

```csharp
public class RawHandler : IMessageHandler
{
    public ErrorMessageRouting ErrorMessageRouter => new ErrorMessageRouting();

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

**Примечание**: Сообщение может быть обработано только одним обработчиком. Хотя один обработчик может обработать много сообщений с разными ключами маршрутизации.
Такое ограничение связано с маршрутизацией ошибочных сообщения в обработчике.

#### `MessageHandlerJson<TModel>`

Так как сообщения в данном клиенте сериализуются в Json, то добавлена реализация интерфейса в виде абстрактного класса `RabbitMQCoreClient.MessageHandlerJson<TModel>`,
который сам выполняет десериализацию Json в модель нужного типа. Пример использования:

```csharp
public class Handler : MessageHandlerJson<SimpleObj>
{
    protected override Task HandleMessage(SimpleObj message, RabbitMessageEventArgs args)
    {
        Console.WriteLine(JsonConvert.SerializeObject(message));
        return Task.CompletedTask;
    }

    protected override ValueTask OnParseError(string json, JsonException e, RabbitMessageEventArgs args)
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

Класс `RabbitMQCoreClient.MessageHandlerJson<TModel>` дает возможность определить поведение при ошибке сериализации
путем переопределения метода `ValueTask OnParseError(string json, JsonException e, RabbitMessageEventArgs args)`.

#### Маршрутизация сообщений

По умолчанию, если обработчик выбрасывает любое исключение, то сообщение будет отправлено обратно в очередь с уменьшенным TTL.
При обработке сообщений часто требуется задать разное поведение при различных исключениях.
Для определения поведения клиента при вызове исключения используется маршрутизатор сообщений `ErrorMessageRouting`.

Возможны 2 варианта поведения:

- отправить сообщение обратно в очередь;
- отправить сообщение в очередь мёртвых сообщений.

Следует учитывать, чтобы сообщение прошло маршрутизацию ошибочных сообщений требуется, чтобы обработчик упал с ошибкой.
Если метод выполнится нормально, то сообщение будет считаться доставленным.

Пример использования:

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

### Конфигурация

Конфигурация может быть выполнена как с помощью опций, так и с помощью конфигурирования из appsettings.json.

В версии 4.0 библиотеки всё еще поддерживается старый (<=v3) формат авторегистрации очередей. Но с ограничениями:

- Автоматически зарегистрировать можно только одну очередь. Регистрация очереди происходит на точку обмена `"Exchange"`.

##### Формат конфигурации

###### Полная конфигурация:

```json
{
  "HostName": "rabbit-1",
  "UserName": "user",
  "Password": "password",
  "DefaultTtl": 5,
  "PrefetchCount": 1,
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

###### Конфигурация сокращенная, которая используется повседневно

Если `Exchanges` в секции `Queues` не указано, то очередь будет пристыкована к точке обмена по умолчанию.

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

###### Старый формат конфигурации, который все еще поддерживается. Очередь будет привязана к Exchange.

Как *default exchange* будет использоваться `Exchange`.

```json
{
  "HostName": "rabbit-1",
  "UserName": "user",
  "Password": "password",
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