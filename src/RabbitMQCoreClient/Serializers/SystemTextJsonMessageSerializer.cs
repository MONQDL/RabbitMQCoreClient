using System;

namespace RabbitMQCoreClient.Serializers
{
    public class SystemTextJsonMessageSerializer: IMessageSerializer
    {
        public System.Text.Json.JsonSerializerOptions Options { get; }

        public SystemTextJsonMessageSerializer(Action<System.Text.Json.JsonSerializerOptions>? setupAction = null)
        {
            if (setupAction is null)
                Options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                    DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                };
            else
            {
                Options = new System.Text.Json.JsonSerializerOptions();
                setupAction(Options);
            }
        }

        /// <inheritdoc />
        public string Serialize<TValue>(TValue value)
        {
            return System.Text.Json.JsonSerializer.Serialize(value, Options);
        }

        /// <inheritdoc />
        public TResult? Deserialize<TResult>(string value)
        {
            return System.Text.Json.JsonSerializer.Deserialize<TResult>(value, Options);
        }
    }
}
