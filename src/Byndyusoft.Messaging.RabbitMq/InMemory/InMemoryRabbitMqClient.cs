using Byndyusoft.Messaging.RabbitMq.Abstractions;
using Byndyusoft.Messaging.RabbitMq.Core;
using Microsoft.Extensions.Options;

namespace Byndyusoft.Messaging.RabbitMq.InMemory
{
    public class InMemoryRabbitMqClient : RabbitMqClient
    {
        public InMemoryRabbitMqClient(IOptions<RabbitMqClientOptions> options)
            : this(new InMemoryRabbitMqClientHandler(), options)
        {
        }

        public InMemoryRabbitMqClient(InMemoryRabbitMqClientHandler handler, IOptions<RabbitMqClientOptions> options) 
            : base(handler, options)
        {
        }
    }
}