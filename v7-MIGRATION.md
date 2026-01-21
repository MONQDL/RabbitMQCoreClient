# Migrating to RabbitMQCoreClient 7.x

This document outlines the major API changes in version 7 of this library.

## Namespaces

The namespace structure has changed. All service registration logic has been moved to `using RabbitMQCoreClient.DependencyInjection;`.

## Start Methods

A new startup model for publishers and consumers has been implemented.
Services are now registered and started as `Microsoft.Extensions.Hosting.IHostedService`.
You should remove the calls to `app.StartRabbitMqCore(lifetime)` in _Program.cs_.

## `async` / `await`

Methods that perform queue, exchange configuration, and server connection have been renamed to follow the Async naming pattern.

## `Send` Methods

The API for `Send*` methods has been reworked.
Optional parameters `correlationId` and `decreaseTtl` have been removed from some `Send*` methods. `CancellationToken` arguments have been added.
- `correlationId` was used incorrectly in this context. It was intended to be some kind of trace identifier, but this field is used for message correlation in the AMQP protocol.
- `decreaseTtl` is meaningless without passing `BasicProperties`, because to decrement the TTL, it must first be retrieved from the headers in BasicProperties. For new messages, the TTL is always the maximum.

The `SendJson*` methods have been removed. Overloads accepting strings have been added in their place. Please use those.

## `IMessageHandler`

The properties `ErrorMessageRouting ErrorMessageRouter { get; }` and `ConsumerHandlerOptions? Options { get; set; }`
have been moved to the new `MessageHandlerContext` class and are now passed in the `context` field of the `IMessageHandler.HandleMessage` method.

The signature of the `IMessageHandler.HandleMessage` method has changed:

```
    Task HandleMessage(ReadOnlyMemory<byte> message,
        RabbitMessageEventArgs args,
        MessageHandlerContext context);
```

**Migration Guide:**

- Add a new argument to the `HandleMessage` method: `MessageHandlerContext context`.
- Remove the following properties from your `IMessageHandler` implementation:
```
    public ErrorMessageRouting ErrorMessageRouter => new ErrorMessageRouting();
    public ConsumerHandlerOptions? Options { get; set; }
```
- Use `context.ErrorMessageRouter` instead of `this.ErrorMessageRouter`.

## `MessageHandlerJson<TModel>`

Due to the changes in `IMessageHandler`, the signatures of the `OnParseError` and `HandleMessage` methods have also changed.

**Migration Guide:**

- Add a new argument to both `OnParseError` and `HandleMessage` methods: `MessageHandlerContext context`.
- Use `context.ErrorMessageRouter` instead of `this.ErrorMessageRouter`.

## `AddBatchQueueSender`

The method for setting up `BatchQueueSender` has changed. It is now configured via the RabbitMQCoreClient builder like this:

```csharp
services
    .AddRabbitMQCoreClient(config.GetSection("RabbitMQ"))
    .AddBatchQueueSender();
```

Previously, `AddBatchQueueSender` could be called directly on `IServiceCollection`, potentially forgetting to register `AddRabbitMQCoreClient`.

`IQueueEventsBufferEngine` has been renamed to `IQueueBufferService`.

## IMessageSerializer

The ability to define deserialization logic within the interface has been removed. To use a custom deserializer, implement your own override of the `IMessageHandler` interface.

`MessageHandlerJson<TModel>` has changed. It now works with JSON serialization context and requires a context to be provided. Example:

```csharp
public class SimpleObj
{
    public required string Name { get; set; }
}

[JsonSerializable(typeof(SimpleObj))]
public partial class SimpleObjContext : JsonSerializerContext
{
}

public class Handler : MessageHandlerJson<SimpleObj>
{
    protected override JsonTypeInfo<SimpleObj> GetSerializerContext() => SimpleObjContext.Default.SimpleObj;

    protected override Task HandleMessage(SimpleObj message, RabbitMessageEventArgs args)
    {
        return Task.CompletedTask;
    }
}
```