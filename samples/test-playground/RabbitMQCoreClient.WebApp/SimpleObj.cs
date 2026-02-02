using System.Text.Json.Serialization;

namespace RabbitMQCoreClient.WebApp;

public class SimpleObj
{
    public required string Name { get; set; }
}

[JsonSerializable(typeof(SimpleObj))]
public partial class SimpleObjContext : JsonSerializerContext
{
}
