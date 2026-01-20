using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace RabbitMQCoreClient.Serializers;

/// <summary>
/// System.Text.Json message serializer.
/// </summary>
public class SystemTextJsonMessageSerializer : IMessageSerializer
{
    /// <summary>
    /// Default serialization options.
    /// </summary>
    public static System.Text.Json.JsonSerializerOptions DefaultOptions { get; } =
        new System.Text.Json.JsonSerializerOptions
        {
            DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };

    /// <summary>
    /// Current serialization options.
    /// </summary>
    public System.Text.Json.JsonSerializerOptions Options { get; }

    /// <summary>
    /// Creates new object of <see cref="SystemTextJsonMessageSerializer"/>.
    /// </summary>
    /// <param name="setupAction">Setup parameters.</param>
    public SystemTextJsonMessageSerializer(Action<System.Text.Json.JsonSerializerOptions>? setupAction = null)
    {
        if (setupAction is null)
        {
            Options = DefaultOptions;
        }
        else
        {
            Options = new System.Text.Json.JsonSerializerOptions();
            setupAction(Options);
        }
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("Method uses System.Text.Json.JsonSerializer.SerializeToUtf8Bytes witch is incompatible with trimming.")]
    public ReadOnlyMemory<byte> Serialize<TValue>(TValue value) =>
        System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(value, Options);
}
