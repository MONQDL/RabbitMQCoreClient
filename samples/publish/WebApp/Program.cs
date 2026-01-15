using RabbitMQCoreClient;

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
