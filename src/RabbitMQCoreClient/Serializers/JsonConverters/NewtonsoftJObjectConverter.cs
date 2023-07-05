using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RabbitMQCoreClient.Serializers.JsonConverters
{
    public class NewtonsoftJObjectConverter : JsonConverter<Newtonsoft.Json.Linq.JObject>
    {
        public override Newtonsoft.Json.Linq.JObject? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var jsonDoc = JsonDocument.ParseValue(ref reader);
            var objStr = jsonDoc.RootElement.GetRawText();
            return Newtonsoft.Json.Linq.JObject.Parse(objStr);
        }

        public override void Write(Utf8JsonWriter writer, Newtonsoft.Json.Linq.JObject value, JsonSerializerOptions options)
        {
            writer.WriteRawValue(value.ToString());
        }
    }
}
