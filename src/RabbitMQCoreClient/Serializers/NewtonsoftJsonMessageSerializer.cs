using Microsoft.Extensions.Logging;
using System;
using System.Text;

namespace RabbitMQCoreClient.Serializers
{
    public class NewtonsoftJsonMessageSerializer : IMessageSerializer
    {
        public Newtonsoft.Json.JsonSerializerSettings Options { get; }

        static readonly Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver JsonResolver =
            new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver
            {
                NamingStrategy = new Newtonsoft.Json.Serialization.CamelCaseNamingStrategy
                {
                    ProcessDictionaryKeys = true
                }
            };

        public NewtonsoftJsonMessageSerializer(Action<Newtonsoft.Json.JsonSerializerSettings>? setupAction = null)
        {
            if (setupAction is null)
            {
                Options = new Newtonsoft.Json.JsonSerializerSettings() { ContractResolver = JsonResolver };
            }
            else
            {
                Options = new Newtonsoft.Json.JsonSerializerSettings();
                setupAction(Options);
            }
        }

        /// <inheritdoc />
        public ReadOnlyMemory<byte> Serialize<TValue>(TValue value)
        {
            var serializedValue = Newtonsoft.Json.JsonConvert.SerializeObject(value, Options);
            return Encoding.UTF8.GetBytes(serializedValue);
        }

        /// <inheritdoc />
        public TResult? Deserialize<TResult>(ReadOnlyMemory<byte> value)
        {
            var message = Encoding.UTF8.GetString(value.ToArray()) ?? string.Empty;
            return Newtonsoft.Json.JsonConvert.DeserializeObject<TResult>(message, Options);
        }
    }
}
