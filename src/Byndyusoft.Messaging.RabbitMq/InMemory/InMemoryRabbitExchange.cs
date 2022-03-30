using System.Collections.Generic;
using Byndyusoft.Messaging.RabbitMq.Topology;

namespace Byndyusoft.Messaging.RabbitMq.InMemory
{
    public class InMemoryRabbitExchange
    {
        private readonly HashSet<InMemoryRabbitExchangeBinding> _bindings = new();

        public InMemoryRabbitExchange(string name, ExchangeOptions options)
        {
            Name = name;
            Options = options;
        }

        public string Name { get; }

        public ExchangeOptions Options { get; }

        public IEnumerable<InMemoryRabbitExchangeBinding> Bindings => _bindings;

        public void Bind(string routingKey, string queueName)
        {
            _bindings.Add(new InMemoryRabbitExchangeBinding {QueueName = queueName, RoutingKey = routingKey});
        }

        public override string ToString() => Name;
    }
}