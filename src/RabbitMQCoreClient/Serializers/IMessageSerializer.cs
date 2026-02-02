using System.Diagnostics.CodeAnalysis;

namespace RabbitMQCoreClient.Serializers;

/// <summary>
/// The serialization factory that uses be the RabbitMQCoreClient to serialize/deserialize messages.
/// </summary>
public interface IMessageSerializer
{
    /// <summary>
    /// Serialize the value <paramref name="value"/> of type <typeparamref name="TValue"/> to string.
    /// </summary>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="value">The object to serialize.</param>
    /// <returns>Serialized string.</returns>
    [RequiresUnreferencedCode("Serialization might require types that cannot be statically analyzed.")]
    ReadOnlyMemory<byte> Serialize<TValue>(TValue value);
}
