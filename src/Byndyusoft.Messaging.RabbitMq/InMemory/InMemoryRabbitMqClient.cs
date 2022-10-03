using Byndyusoft.Messaging.RabbitMq.Utils;
using Microsoft.Extensions.Options;

namespace Byndyusoft.Messaging.RabbitMq.InMemory
{
    public class InMemoryRabbitMqClient : RabbitMqClientCore
    {
        public InMemoryRabbitMqClient(IOptions<RabbitMqClientCoreOptions> options)
            : this(new InMemoryRabbitMqClientHandler(), options)
        {
        }

        public InMemoryRabbitMqClient(InMemoryRabbitMqClientHandler handler,
            IOptions<RabbitMqClientCoreOptions> options)
            : base(handler, Preconditions.CheckNotNull(options, nameof(options)).Value)
        {
        }
    }
}