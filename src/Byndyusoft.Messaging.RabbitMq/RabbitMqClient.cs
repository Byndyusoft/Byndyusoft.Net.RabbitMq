using Byndyusoft.Messaging.RabbitMq.Utils;
using Microsoft.Extensions.Options;

namespace Byndyusoft.Messaging.RabbitMq
{
    public class RabbitMqClient : RabbitMqClientCore
    {
        public RabbitMqClient(RabbitMqClientHandler handler, RabbitMqClientOptions options, bool disposeHandler = false)
            : base(handler, options, disposeHandler)
        {
        }

        public RabbitMqClient(RabbitMqClientHandler handler, IOptions<RabbitMqClientOptions> options)
            : this(handler, Preconditions.CheckNotNull(options, nameof(options)).Value)
        {
        }
    }
}