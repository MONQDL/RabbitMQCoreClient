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

    /// <summary>
    /// Deserialize the value <paramref name="value"/> from string to <typeparamref name="TResult"/> type.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="value">The byte array of the message from the provider as ReadOnlyMemory &lt;byte&gt;.</param>
    /// <returns>The object of type <typeparamref name="TResult"/> or null.</returns>
    [RequiresUnreferencedCode("Serialization might require types that cannot be statically analyzed.")]
    TResult? Deserialize<TResult>(ReadOnlyMemory<byte> value);
}
