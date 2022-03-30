using System;

namespace Byndyusoft.Messaging.RabbitMq.InMemory
{
    public class InMemoryRabbitExchangeBinding
    {
        public string RoutingKey { get; init; } = default!;

        public string QueueName { get; init; } = default!;

        public void Deconstruct(out string routingKey, out string queueName)
        {
            routingKey = RoutingKey;
            queueName = QueueName;
        }

        public override string ToString() => $"{QueueName}::{RoutingKey}";

        protected bool Equals(InMemoryRabbitExchangeBinding other)
        {
            return string.Equals(RoutingKey, other.RoutingKey) && string.Equals(QueueName, other.QueueName);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((InMemoryRabbitExchangeBinding) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(RoutingKey, QueueName);
        }
    }
}