using System;

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
        public string Serialize<TValue>(TValue value)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(value, Options);
        }

        /// <inheritdoc />
        public TResult? Deserialize<TResult>(string value)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<TResult>(value, Options);
        }
    }
}
