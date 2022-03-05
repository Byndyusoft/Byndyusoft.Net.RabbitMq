using System;

namespace Byndyusoft.Messaging.RabbitMq
{
    public class RabbitMqMessageProperties
    {
        public string? ContentType { get; set; }
        public string? ContentEncoding { get; set; }
        public byte Priority { get; set; }
        public string? CorrelationId { get; set; }
        public string? ReplyTo { get; set; }
        public TimeSpan? Expiration { get; set; }
        public string? MessageId { get; set; }
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        public string? Type { get; set; }
        public string? UserId { get; set; }
        public string? AppId { get; set; }
        public string? ClusterId { get; set; }
        public bool Persistent { get; set; }
    }
}