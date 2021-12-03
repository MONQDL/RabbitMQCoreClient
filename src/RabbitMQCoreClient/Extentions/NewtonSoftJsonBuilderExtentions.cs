using Newtonsoft.Json;
using RabbitMQCoreClient.Serializers;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class NewtonSoftJsonBuilderExtentions
    {
        /// <summary>
        /// Use NewtonsoftJson serializer as default serializer for the RabbitMQ messages.
        /// </summary>
        public static IRabbitMQCoreClientBuilder AddNewtonsoftJson(this IRabbitMQCoreClientBuilder builder, Action<JsonSerializerSettings>? setupAction = null)
        {
            builder.Serializer = new NewtonsoftJsonMessageSerializer(setupAction);
            return builder;
        }

        /// <summary>
        /// Use NewtonsoftJson serializer as default serializer for the RabbitMQ messages.
        /// </summary>
        public static IRabbitMQCoreClientConsumerBuilder AddNewtonsoftJson(this IRabbitMQCoreClientConsumerBuilder builder, Action<JsonSerializerSettings>? setupAction = null)
        {
            builder.Builder.AddNewtonsoftJson(setupAction);
            return builder;
        }
    }
}
