using Byndyusoft.Messaging.RabbitMq.Abstractions.Utils;
using Byndyusoft.Messaging.RabbitMq.Core;
using Microsoft.Extensions.Options;

namespace Byndyusoft.Messaging.RabbitMq
{
    public class RabbitMqClient : RabbitMqClientCore
    {
        public RabbitMqClient(RabbitMqClientHandler handler, RabbitMqClientHandlerOptions options, bool disposeHandler = false)
            : base(handler, options, disposeHandler)
        {
        }

        public RabbitMqClient(RabbitMqClientHandler handler, IOptions<RabbitMqClientHandlerOptions> options)
            : this(handler, Preconditions.CheckNotNull(options, nameof(options)).Value)
        {
        }
    }
}