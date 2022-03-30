namespace Byndyusoft.Messaging.Abstractions
{
    public class QueueServiceEndpoint
    {
        public string Transport { get; set; } = default!;

        public string Host { get; set; } = default!;

        public string? Port { get; set; }
    }
}