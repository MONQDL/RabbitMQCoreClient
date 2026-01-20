# Migrating to RabbitMQCoreClient 7.x

This document makes note of major changes in the API of this library for
version 7.

## Namespaces

Изменилась структура пространств имен. Теперь вся логика регистрации сервиса переехала в `using RabbitMQCoreClient.DependencyInjection;`.

## Start methods

Имплементирована новая модель запуска publisher и consumer. 
Теперь сервисы регистрируются и запускаются как `Microsoft.Extensions.Hosting.IHostedService`.
Нужно удалить вызовы `app.StartRabbitMqCore(lifetime)`.

## `async` / `await`

Методы, которые выполняют конфигурацию очередей, exchange, соединение с сервером переименованы для использования Async паттерна в именовании.

## `Send` methods

Переработана API для методов `Send*`.
Из некоторых `Send*` методов удалены необязательные параметры `correlationId` и `decreaseTtl`. Добавлены `CancellationToken` атрибуты.
- `correlationId` в данном контексте неправильно использовался, предполагалось, что это должен быть некоторого рода trace идентификатор, но это поле используется при передаче сообщений в протоколе.
- `decreaseTtl` без передачи `BasicProperties` не имеет смысла, т.к. чтобы выполнить понижение ttl, этот ttl нужно получить из заголовков. Для новых сообщений ttl всегда максимальный.

Удалены методы `SendJson*`, взамен добавлены перегрузки для приема строк. Используйте их.

## `AddBatchQueueSender`

Изменился метод подключения `BatchQueueSender`. Теперь это выполняется с помощью билдера RabbitMQCoreClient так:

```csharp
services
    .AddRabbitMQCoreClient(config.GetSection("RabbitMQ"))
    .AddBatchQueueSender();
```

Раньше `AddBatchQueueSender` можно было выполнить непосредственно на IServiceCollection и забыть при этом зарегистрировать AddRabbitMQCoreClient.

`IQueueEventsBufferEngine` переименован в `IQueueBufferService`.

## IMessageSerializer

Удалена возможность определения десериализации в интерфейсе. Теперь для того, чтобы использовать кастомный десериализатор используйте собственное переопределение интерфейса IMessageHandler.

Изменился MessageHandlerJson<TModel>. Теперь он работает с контекстом сериализации JSON и требует получения контекста. Пример:

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