using Byndyusoft.Messaging.RabbitMq.Abstractions;
using Byndyusoft.Messaging.RabbitMq.Core;

namespace Byndyusoft.Messaging.RabbitMq.InMemory
{
    public class InMemoryRabbitMqClient : RabbitMqClient
    {
        public InMemoryRabbitMqClient(RabbitMqClientOptions options)
            : this(new InMemoryRabbitMqClientHandler(), options)
        {
        }

        public InMemoryRabbitMqClient(InMemoryRabbitMqClientHandler handler, RabbitMqClientOptions options) 
            : base(handler, options)
        {
        }
    }
}