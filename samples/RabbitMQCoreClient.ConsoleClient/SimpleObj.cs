using System.Text.Json.Serialization;

namespace RabbitMQCoreClient.ConsoleClient;


public class SimpleObj
{
    public string Name { get; set; }
}

[JsonSerializable(typeof(SimpleObj))]
public partial class SimpleObjContext : JsonSerializerContext
{
}