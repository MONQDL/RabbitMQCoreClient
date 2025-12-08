using RabbitMQCoreClient;
using RabbitMQCoreClient.Configuration.DependencyInjection.Options;
using RabbitMQCoreClient.WebApp;

var builder = WebApplication.CreateBuilder(args);

// Just for sending messages.
builder.Services
    .AddRabbitMQCoreClient(builder.Configuration.GetSection("RabbitMQ"));

// For sending and consuming messages config with subscriptions.
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
var app = builder.Build();

// Configure the HTTP request pipeline.

app.MapPost("/send", async (IQueueService queueService) =>
{
    await queueService.SendAsync(new SimpleObj { Name = "test" }, "test_routing_key");
    return Results.Ok();
});

app.StartRabbitMqCore(app.Lifetime);

app.Run();
