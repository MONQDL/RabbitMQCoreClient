using Microsoft.Extensions.DependencyInjection;
using RabbitMQCoreClient.Serializers;
using System.Text.Json;

namespace RabbitMQCoreClient.DependencyInjection;

/// <summary>
/// RabbitMQClient builder extensions for System.Text.Json serializer support.
/// </summary>
public static class SystemTextJsonBuilderExtensions
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
