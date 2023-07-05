using System;
using System.Text.Json.Serialization;

namespace RabbitMQCoreClient.Serializers
{
    public class SystemTextJsonMessageSerializer : IMessageSerializer
    {
        public System.Text.Json.JsonSerializerOptions Options { get; }

        public SystemTextJsonMessageSerializer(Action<System.Text.Json.JsonSerializerOptions>? setupAction = null)
        {
            if (setupAction is null)
            {
                Options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                };
                Options.Converters.Add(new JsonStringEnumConverter());
            }
            else
            {
                Options = new System.Text.Json.JsonSerializerOptions();
                setupAction(Options);
            }
        }

        /// <inheritdoc />
        public ReadOnlyMemory<byte> Serialize<TValue>(TValue value) =>
            System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(value, Options);

        /// <inheritdoc />
        public TResult? Deserialize<TResult>(ReadOnlyMemory<byte> value) => 
            System.Text.Json.JsonSerializer.Deserialize<TResult>(value.Span, Options);
    }
}
