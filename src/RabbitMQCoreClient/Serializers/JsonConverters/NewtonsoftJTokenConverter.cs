using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RabbitMQCoreClient.Serializers.JsonConverters
{
    public class NewtonsoftJTokenConverter : JsonConverter<Newtonsoft.Json.Linq.JToken>
    {
        public override Newtonsoft.Json.Linq.JToken? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var jsonDoc = JsonDocument.ParseValue(ref reader);
            var objStr = jsonDoc.RootElement.GetRawText();
            return Newtonsoft.Json.Linq.JToken.Parse(objStr);
        }

        public override void Write(Utf8JsonWriter writer, Newtonsoft.Json.Linq.JToken value, JsonSerializerOptions options)
        {
#if NET5_0
            using var doc = JsonDocument.Parse(value.ToString());
            doc.WriteTo(writer);
#else
            writer.WriteRawValue(value.ToString());
#endif
        }
    }
}
