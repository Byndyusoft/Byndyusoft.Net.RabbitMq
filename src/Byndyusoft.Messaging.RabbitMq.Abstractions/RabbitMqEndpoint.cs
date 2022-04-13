namespace Byndyusoft.Messaging.RabbitMq.Abstractions
{
    public class RabbitMqEndpoint
    {
        public string Host { get; init; } = default!;

        public string? Port { get; init; }
    }
}