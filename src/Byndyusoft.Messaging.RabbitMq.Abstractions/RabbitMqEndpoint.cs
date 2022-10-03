namespace Byndyusoft.Messaging.RabbitMq
{
    public class RabbitMqEndpoint
    {
        public string Host { get; init; } = default!;

        public string? Port { get; init; }
    }
}