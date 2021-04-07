namespace RabbitMQCoreClient.Models
{
    /// <summary>
    /// Действия, производимые с ttl.
    /// </summary>
    public enum TtlActions
    {
        /// <summary>
        /// Уменьшить ttl сообщения.
        /// </summary>
        Decrease,
        /// <summary>
        /// Не изменять ttl сообщения.
        /// </summary>
        DoNotChange
    }
}