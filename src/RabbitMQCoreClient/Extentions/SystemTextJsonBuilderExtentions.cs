using RabbitMQCoreClient.Serializers;
using System;
using System.Text.Json;

namespace Microsoft.Extensions.DependencyInjection;

public static class SystemTextJsonBuilderExtentions
{
    /// <summary>
    /// Use System.Text.Json serializer as default serializer for the RabbitMQ messages.
    /// </summary>
    public static IRabbitMQCoreClientBuilder AddSystemTextJson(this IRabbitMQCoreClientBuilder builder, Action<JsonSerializerOptions>? setupAction = null)
    {
        builder.Serializer = new SystemTextJsonMessageSerializer(setupAction);
        return builder;
    }

    /// <summary>
    /// Use System.Text.Json serializer as default serializer for the RabbitMQ messages.
    /// </summary>
    public static IRabbitMQCoreClientConsumerBuilder AddSystemTextJson(this IRabbitMQCoreClientConsumerBuilder builder, Action<JsonSerializerOptions>? setupAction = null)
    {
        builder.Builder.AddSystemTextJson(setupAction);
        return builder;
    }
}
