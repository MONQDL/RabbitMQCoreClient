using RabbitMQCoreClient.Serializers.JsonConverters;
using System;
using System.Text.Json.Serialization;

namespace RabbitMQCoreClient.Serializers
{
    public class SystemTextJsonMessageSerializer : IMessageSerializer
    {
        static readonly System.Text.Json.JsonSerializerOptions _defaultOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        };

        public static System.Text.Json.JsonSerializerOptions DefaultOptions => _defaultOptions;

        static SystemTextJsonMessageSerializer()
        {
            _defaultOptions.Converters.Add(new JsonStringEnumConverter());
            _defaultOptions.Converters.Add(new NewtonsoftJObjectConverter());
            _defaultOptions.Converters.Add(new NewtonsoftJArrayConverter());
            _defaultOptions.Converters.Add(new NewtonsoftJTokenConverter());
        }

        public System.Text.Json.JsonSerializerOptions Options { get; }

        public SystemTextJsonMessageSerializer(Action<System.Text.Json.JsonSerializerOptions>? setupAction = null)
        {
            if (setupAction is null)
            {
                Options = _defaultOptions;
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
