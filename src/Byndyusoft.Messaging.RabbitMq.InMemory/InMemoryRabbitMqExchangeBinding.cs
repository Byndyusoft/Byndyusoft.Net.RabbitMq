using System;

namespace Byndyusoft.Messaging.RabbitMq.InMemory
{
    public class InMemoryRabbitMqExchangeBinding
    {
        public string RoutingKey { get; init; } = default!;

        public string QueueName { get; init; } = default!;

        public void Deconstruct(out string routingKey, out string queueName)
        {
            routingKey = RoutingKey;
            queueName = QueueName;
        }

        public override string ToString()
        {
            return $"{QueueName}::{RoutingKey}";
        }

        protected bool Equals(InMemoryRabbitMqExchangeBinding other)
        {
            return string.Equals(RoutingKey, other.RoutingKey) && string.Equals(QueueName, other.QueueName);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((InMemoryRabbitMqExchangeBinding) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(RoutingKey, QueueName);
        }
    }
}