using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RabbitMQCoreClient.Serializers.JsonConverters
{
    public class NewtonsoftJArrayConverter : JsonConverter<Newtonsoft.Json.Linq.JArray>
    {
        public override Newtonsoft.Json.Linq.JArray? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var jsonDoc = JsonDocument.ParseValue(ref reader);
            var objStr = jsonDoc.RootElement.GetRawText();
            return Newtonsoft.Json.Linq.JArray.Parse(objStr);
        }

        public override void Write(Utf8JsonWriter writer, Newtonsoft.Json.Linq.JArray value, JsonSerializerOptions options)
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
