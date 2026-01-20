using System.Text.Json.Serialization;

namespace HostConsole;

public class SimpleObj
{
    public required string Name { get; set; }
}

[JsonSerializable(typeof(SimpleObj))]
public partial class SimpleObjContext : JsonSerializerContext
{
}
