using System;
using System.Collections.Generic;
using Byndyusoft.Messaging.RabbitMq.Topology;

namespace Byndyusoft.Messaging.RabbitMq.InMemory
{
    public class InMemoryRabbitMqExchange
    {
        private readonly HashSet<InMemoryRabbitMqExchangeBinding> _bindings = new();

        public InMemoryRabbitMqExchange(string name, ExchangeOptions options)
        {
            Name = name;
            Options = options;
        }

        public string Name { get; }

        public ExchangeOptions Options { get; }

        public IEnumerable<InMemoryRabbitMqExchangeBinding> Bindings => _bindings;

        public void Bind(string routingKey, string queueName)
        {
            _bindings.Add(new InMemoryRabbitMqExchangeBinding {QueueName = queueName, RoutingKey = routingKey});
        }

        public override string ToString()
        {
            return Name;
        }

        public void Unbind(string routingKey, string queueName)
        {
            _bindings.RemoveWhere(binding =>
                string.Equals(binding.QueueName, queueName, StringComparison.InvariantCultureIgnoreCase) &&
                string.Equals(binding.RoutingKey, routingKey, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}